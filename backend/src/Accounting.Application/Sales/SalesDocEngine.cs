using Accounting.Application.OperationTemplates;
using Accounting.Domain.Sales;

namespace Accounting.Application.Sales;

/// <param name="Level">err ⇒ ثبت/قطعی‌سازی ممکن نیست · warn ⇒ هشدار · ok/info ⇒ اطلاع</param>
public sealed record SalesCheck(string Level, string Message);

public sealed record SalesJournalLine(string RoleKey, Guid AccountId, string? AccountCode, string? AccountTitle, long Debit, long Credit, string Description, IReadOnlyList<VoucherDraftDetail> Details);

public sealed record SalesJournal(IReadOnlyList<SalesJournalLine> Lines, IReadOnlyList<string> Errors)
{
    public long Debit => Lines.Sum(l => l.Debit);
    public long Credit => Lines.Sum(l => l.Credit);
}

/// <summary>تنظیمات + نگاشت + نرخ‌ها برای یک واحد — یک بار در هر درخواست خوانده می‌شود.</summary>
public sealed record SalesContext(SalesSetting Setting, IReadOnlyDictionary<string, SalesAccountMap> Maps, IReadOnlyList<SalesVatRate> Rates);

/// <summary>
/// قواعد سند فروش (طرح: docs/sales-module.md): مقدار باقی‌مانده برای تبدیل، کنترل‌های ثبت/قطعی‌سازی و سند حسابداری.
/// هیچ چیز ذخیره نمی‌کند.
/// </summary>
public sealed class SalesDocEngine
{
    private readonly ISalesRepository _repo;
    private readonly ISalesLedgerReader _ledger;
    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IDetailReader _details;
    private readonly AssistantUnitPolicy _policy;

    public SalesDocEngine(ISalesRepository repo, ISalesLedgerReader ledger, ISubsidiaryAccountReader accounts, IDetailReader details, AssistantUnitPolicy policy)
    { _repo = repo; _ledger = ledger; _accounts = accounts; _details = details; _policy = policy; }

    public async Task<SalesContext> ContextAsync(string vahedCode, CancellationToken ct)
    {
        var setting = await _repo.GetSettingAsync(vahedCode, false, ct) ?? new SalesSetting { VahedCode = vahedCode };
        var maps = (await _repo.GetAccountMapsAsync(vahedCode, false, ct)).ToDictionary(m => m.RoleKey);
        return new SalesContext(setting, maps, await _repo.ListVatRatesAsync(false, ct));
    }

    // ─────────── تبدیل سند ───────────

    /// <summary>تبدیل‌های مجاز از هر نوع.</summary>
    public static IReadOnlyList<SalesDocType> TargetsOf(SalesDocType t) => t switch
    {
        SalesDocType.ProForma => [SalesDocType.Order],
        SalesDocType.Order => [SalesDocType.Delivery, SalesDocType.Invoice],
        SalesDocType.Delivery => [SalesDocType.Invoice],
        SalesDocType.Invoice => [SalesDocType.Return],
        _ => [],
    };

    /// <summary>پیش‌فاکتور و سفارش باید تأییدشده، و حواله/فاکتور قطعی باشند تا تبدیل شوند.</summary>
    public static bool IsConvertible(SalesDoc d) =>
        d.Status == (d.DocType is SalesDocType.ProForma or SalesDocType.Order ? SalesDocStatus.Approved : SalesDocStatus.Posted);

    /// <summary>
    /// مقدار باقی‌مانده هر کالای <paramref name="source"/> برای سند مقصد. سندهای زندهٔ صادرشده (هر وضعیتی جز باطل،
    /// از جمله پیش‌نویس) مصرف‌شده‌اند تا دو پیش‌نویس هم‌زمان بیش از سفارش ثبت نکنند؛ <paramref name="excludeDocId"/>
    /// سندی است که در حال ویرایش است.
    /// </summary>
    public async Task<Dictionary<Guid, decimal>> RemainingAsync(
        SalesDoc source, SalesDocType target, Guid? excludeDocId, bool billBeforeDelivery, IReadOnlyDictionary<Guid, SalesItem> items, CancellationToken ct)
    {
        var kids = (await _repo.GetChildrenAsync([source.Id], ct)).Where(d => d.Id != excludeDocId).ToList();
        decimal Used(IEnumerable<SalesDoc> docs, Guid item) => docs.SelectMany(d => d.Lines).Where(l => l.ItemId == item).Sum(l => l.Qty);

        // فاکتورشدهٔ یک سفارش = فاکتورهای مستقیم سفارش + فاکتورهای حواله‌های آن.
        async Task<Func<Guid, decimal>> InvoicedForOrderAsync(SalesDoc order, IReadOnlyList<SalesDoc> orderKids)
        {
            var deliveries = orderKids.Where(k => k.DocType == SalesDocType.Delivery).Select(k => k.Id).ToList();
            var viaDn = deliveries.Count == 0 ? [] : (await _repo.GetChildrenAsync(deliveries, ct)).Where(d => d.Id != excludeDocId && d.DocType == SalesDocType.Invoice).ToList();
            var direct = orderKids.Where(k => k.DocType == SalesDocType.Invoice).ToList();
            return item => Used(direct, item) + Used(viaDn, item);
        }

        var result = new Dictionary<Guid, decimal>();
        Func<Guid, decimal>? soInvoiced = null;
        SalesDoc? order = null;
        if (source.DocType == SalesDocType.Order && target == SalesDocType.Invoice)
            soInvoiced = await InvoicedForOrderAsync(source, kids);
        if (source.DocType == SalesDocType.Delivery && source.SourceId is { } soId && await _repo.GetDocAsync(soId, false, ct) is { } so)
        {
            order = so;
            soInvoiced = await InvoicedForOrderAsync(so, (await _repo.GetChildrenAsync([so.Id], ct)).Where(d => d.Id != excludeDocId).ToList());
        }

        foreach (var g in source.Lines.GroupBy(l => l.ItemId))
        {
            var qty = g.Sum(l => l.Qty);
            var isGoods = items.GetValueOrDefault(g.Key)?.Kind != SalesItemKind.Service;
            decimal rem = (source.DocType, target) switch
            {
                (SalesDocType.ProForma, SalesDocType.Order) => qty - Used(kids.Where(k => k.DocType == SalesDocType.Order), g.Key),
                (SalesDocType.Order, SalesDocType.Delivery) => isGoods ? qty - Used(kids.Where(k => k.DocType == SalesDocType.Delivery), g.Key) : 0,
                (SalesDocType.Order, SalesDocType.Invoice) => !isGoods || billBeforeDelivery ? qty - soInvoiced!(g.Key) : 0,
                (SalesDocType.Delivery, SalesDocType.Invoice) => qty - Used(kids.Where(k => k.DocType == SalesDocType.Invoice), g.Key),
                (SalesDocType.Invoice, SalesDocType.Return) => qty - Used(kids.Where(k => k.DocType == SalesDocType.Return), g.Key),
                _ => 0,
            };
            if (order is not null && soInvoiced is not null)
                rem = Math.Min(rem, order.Lines.Where(l => l.ItemId == g.Key).Sum(l => l.Qty) - soInvoiced(g.Key));
            if (rem > 0) result[g.Key] = rem;
        }
        return result;
    }

    // ─────────── کنترل‌ها ───────────

    /// <summary>
    /// همهٔ کنترل‌های سند. <paramref name="reasons"/> دلیل‌های ارجاع به تأیید (تخفیف، سقف اعتبار با سیاست «ارجاع»).
    /// <paramref name="forPosting"/>: کنترل قفل دوره و موجودی فقط برای قطعی‌سازی سخت است.
    /// </summary>
    public async Task<(List<SalesCheck> Checks, List<string> Reasons)> CheckAsync(
        SalesDoc doc, SalesContext ctx, IReadOnlyDictionary<Guid, SalesItem> items, SalesCustomerRow? customer, bool forPosting, string? fiscalYear, CancellationToken ct)
    {
        var checks = new List<SalesCheck>();
        var reasons = new List<string>();
        void Err(string m) => checks.Add(new("err", m));
        void Warn(string m) => checks.Add(new("warn", m));
        void Info(string m) => checks.Add(new("info", m));

        var c = customer?.Customer;
        if (c is null || c.IsDeleted) Err("مشتری انتخاب نشده یا حذف شده است.");
        else if (c.IsBlocked) Err($"حساب مشتری «{customer!.TafsiliName}» مسدود است.");

        var dateOk = SalesDates.IsValid(doc.DocDate);
        if (!dateOk) Err("تاریخ سند معتبر نیست.");
        else if (FiscalYearGuard.Check(SalesDates.ToDate(doc.DocDate), fiscalYear) is { } fy) Err(fy.Message);
        if (doc.ValidUntil is not null && !SalesDates.IsValid(doc.ValidUntil)) Err("تاریخ اعتبار پیش‌فاکتور معتبر نیست.");

        if (doc.Lines.Count == 0) Err("حداقل یک کالا یا خدمت لازم است.");
        if (doc.Lines.Any(l => l.Qty <= 0)) Err("تعداد هر ردیف باید بیشتر از صفر باشد.");
        if (doc.Lines.GroupBy(l => l.ItemId).Any(g => g.Count() > 1)) Err("یک کالا در چند ردیف تکرار شده است؛ ردیف‌ها را ادغام کنید.");
        foreach (var l in doc.Lines)
        {
            var it = items.GetValueOrDefault(l.ItemId);
            if (it is null || it.IsDeleted || it.VahedCode != doc.VahedCode) { Err("کالای ردیف " + l.Radif + " پیدا نشد."); continue; }
            if (!it.IsActive && doc.SourceId is null) Err($"کالای «{it.Name}» غیرفعال است.");
            if (doc.DocType == SalesDocType.Delivery && it.Kind == SalesItemKind.Service) Err($"خدمت «{it.Name}» در حواله انبار نمی‌آید.");
            if (l.Price < 0 || l.DiscountPercent is < 0 or > 100) Err($"قیمت یا تخفیف «{it.Name}» نامعتبر است.");
            if (l.Price == 0 && doc.DocType != SalesDocType.Delivery) Warn($"قیمت «{it.Name}» صفر است.");
        }

        // مبدأ و باقی‌مانده
        if (doc.DocType == SalesDocType.Return && doc.SourceId is null) Err("برگشت از فروش باید به فاکتور اصلی ارجاع داشته باشد.");
        if (doc.DocType == SalesDocType.Delivery && doc.SourceId is null) Err("حواله تحویل از سفارش فروش صادر می‌شود.");
        if (doc.SourceId is { } srcId)
        {
            var src = await _repo.GetDocAsync(srcId, false, ct);
            if (src is null || src.VahedCode != doc.VahedCode || !TargetsOf(src.DocType).Contains(doc.DocType) || !IsConvertible(src))
                Err("سند مبدأ برای این تبدیل معتبر نیست.");
            else
            {
                if (src.CustomerId != doc.CustomerId) Err("مشتری باید همان مشتری سند مبدأ باشد.");
                var rem = await RemainingAsync(src, doc.DocType, doc.Id, ctx.Setting.BillBeforeDelivery, items, ct);
                foreach (var l in doc.Lines)
                {
                    var name = items.GetValueOrDefault(l.ItemId)?.Name ?? "کالا";
                    if (!rem.TryGetValue(l.ItemId, out var r)) Err($"«{name}» در سند مبدأ باقی‌مانده‌ای ندارد.");
                    else if (l.Qty > r) Err($"مقدار «{name}» بیش از باقی‌ماندهٔ سند مبدأ ({r:0.###}) است.");
                }
            }
        }

        // موجودی: حواله و فاکتور مستقیم کالا را خارج می‌کنند
        var takesStock = doc.DocType == SalesDocType.Delivery || (doc.DocType == SalesDocType.Invoice && doc.SourceId is null);
        if (takesStock && !ctx.Setting.AllowNegativeStock)
            foreach (var l in doc.Lines)
                if (items.GetValueOrDefault(l.ItemId) is { Kind: SalesItemKind.Goods } it && l.Qty > it.Stock)
                    (forPosting ? (Action<string>)Err : Warn)($"موجودی «{it.Name}» کافی نیست (موجود: {it.Stock:0.###}).");

        // تخفیف — فقط سندی که قیمتش را خودش تعیین می‌کند؛ سند تبدیلی قیمت و تخفیف مبدأ تأییدشده را دارد.
        if (doc.SourceId is null && doc.DocType is SalesDocType.ProForma or SalesDocType.Order or SalesDocType.Invoice)
        {
            var maxLine = doc.Lines.Count == 0 ? 0 : doc.Lines.Max(l => l.DiscountPercent);
            var eff = doc.Gross > 0 ? 100m * doc.Discount / doc.Gross : 0;
            var top = Math.Max(maxLine, eff);
            if (top > ctx.Setting.DiscountApprovalPercent)
            {
                var t = $"تخفیف {Math.Round(top, 1)}٪ بیش از آستانهٔ {ctx.Setting.DiscountApprovalPercent:0.##}٪";
                reasons.Add(t);
                Warn(t + " — نیازمند تأیید مدیر فروش");
            }
        }

        // سقف اعتبار: سفارش و فاکتور مستقیم تعهد تازه می‌سازند
        if (c is not null && (doc.DocType == SalesDocType.Order || (doc.DocType == SalesDocType.Invoice && doc.SourceId is null)))
        {
            var (balance, open) = await ExposureAsync(c, ctx, doc.Id, doc.Year, ct);
            var after = balance + open + doc.Total;
            if (c.CreditLimit > 0 && after > c.CreditLimit)
            {
                var t = $"عبور از سقف اعتبار ({after:N0} از {c.CreditLimit:N0} ریال)";
                switch (ctx.Setting.CreditPolicy)
                {
                    case CreditPolicy.Block: Err(t + " — ثبت مجاز نیست"); break;
                    case CreditPolicy.Approve: reasons.Add(t); Warn(t + " — ارجاع برای تأیید"); break;
                    default: Warn(t); break;
                }
            }
            else if (c.CreditLimit > 0) checks.Add(new("ok", $"اعتبار کافی: {c.CreditLimit - after:N0} ریال پس از این سند"));
            else Info("برای این مشتری سقف اعتبار تعریف نشده است.");
        }

        if (c is not null && doc.DocType is SalesDocType.Invoice or SalesDocType.Return)
        {
            if (SalesIds.CompleteForMoadian(c)) checks.Add(new("ok", "صورتحساب مودیان: نوع اول (اطلاعات خریدار کامل)"));
            else if (c.PersonKind == PersonKind.Legal) Warn("اطلاعات هویتی/کد پستی خریدار حقوقی ناقص است؛ صورتحساب نوع اول ممکن نیست.");
            else Info("صورتحساب مودیان: نوع دوم (اطلاعات خریدار ناقص)");
        }

        if (forPosting && dateOk && await _policy.PeriodLockedAsync(doc.VahedCode, SalesDates.ToDate(doc.DocDate), ct) is { } locked)
            Err(locked.Message);

        return (checks, reasons);
    }

    /// <summary>مانده دریافتنی مشتری از دفتر + مبلغ فاکتورنشدهٔ سفارش‌های تأییدشدهٔ باز.</summary>
    public async Task<(long Balance, long OpenOrders)> ExposureAsync(SalesCustomer c, SalesContext ctx, Guid? exceptDocId, string year, CancellationToken ct)
    {
        var balance = ctx.Maps.TryGetValue(SalesAccountRoles.Receivable, out var ar)
            ? await _ledger.ReceivableBalanceAsync(c.VahedCode, year, ar.AccountId, c.TafsiliId, ct)
            : 0;
        long open = 0;
        foreach (var so in (await _repo.GetApprovedOrdersAsync(c.Id, ct)).Where(o => o.Id != exceptDocId))
        {
            var kids = await _repo.GetChildrenAsync([so.Id], ct);
            var dn = kids.Where(k => k.DocType == SalesDocType.Delivery).Select(k => k.Id).ToList();
            var invoices = kids.Where(k => k.DocType == SalesDocType.Invoice).Concat(dn.Count == 0 ? [] : (await _repo.GetChildrenAsync(dn, ct)).Where(k => k.DocType == SalesDocType.Invoice)).ToList();
            foreach (var l in so.Lines.Where(l => l.Qty > 0))
            {
                var invoiced = invoices.SelectMany(i => i.Lines).Where(x => x.ItemId == l.ItemId).Sum(x => x.Qty);
                open += (long)Math.Round(l.Total * Math.Max(0, l.Qty - invoiced) / l.Qty);
            }
        }
        return (balance, open);
    }

    // ─────────── سند حسابداری ───────────

    public async Task<SalesJournal> JournalAsync(SalesDoc doc, SalesContext ctx, IReadOnlyDictionary<Guid, SalesItem> items, SalesCustomer customer, CancellationToken ct)
    {
        var rows = new List<(string Role, long Dr, long Cr, string Desc)>();
        void Dr(string r, long v, string d) { if (v != 0) rows.Add((r, v, 0, d)); }
        void Cr(string r, long v, string d) { if (v != 0) rows.Add((r, 0, v, d)); }

        long Cost(SalesDocLine l) => items.GetValueOrDefault(l.ItemId) is { Kind: SalesItemKind.Goods } it
            ? (long)Math.Round(l.Qty * (l.UnitCost > 0 ? l.UnitCost : it.StandardCost), MidpointRounding.AwayFromZero)
            : 0;
        bool IsGoods(SalesDocLine l) => items.GetValueOrDefault(l.ItemId)?.Kind != SalesItemKind.Service;
        var goodsCost = doc.Lines.Where(IsGoods).Sum(Cost);
        var title = SalesLabels.Type(doc.DocType) + (doc.DocNo is null ? "" : " " + doc.DocNo);

        switch (doc.DocType)
        {
            case SalesDocType.Invoice:
            {
                var src = doc.SourceId is { } sid ? await _repo.GetDocAsync(sid, false, ct) : null;
                var early = src?.DocType == SalesDocType.Order; // کالای فاکتورشده پیش از تحویل
                long goods = 0, service = 0, disc = 0, contract = 0;
                foreach (var l in doc.Lines)
                {
                    if (IsGoods(l) && early) contract += l.Taxable;
                    else
                    {
                        if (IsGoods(l)) goods += l.Gross; else service += l.Gross;
                        disc += l.LineDiscount + l.HeaderDiscountShare;
                    }
                }
                Dr(SalesAccountRoles.Receivable, doc.Total, title);
                Dr(SalesAccountRoles.Discount, disc, "تخفیف فروش — " + title);
                Cr(SalesAccountRoles.Sales, goods, "فروش کالا — " + title);
                Cr(SalesAccountRoles.Service, service, "درآمد خدمات — " + title);
                Cr(SalesAccountRoles.Contract, contract, "کالای فاکتورشدهٔ تحویل‌نشده — " + title);
                Cr(SalesAccountRoles.Vat, doc.Vat, "مالیات بر ارزش افزوده — " + title);
                if (src is null)
                {
                    Dr(SalesAccountRoles.Cogs, goodsCost, "بهای تمام‌شده — " + title);
                    Cr(SalesAccountRoles.Inventory, goodsCost, "خروج کالا — " + title);
                }
                break;
            }
            case SalesDocType.Delivery:
                Dr(SalesAccountRoles.Cogs, goodsCost, "بهای تمام‌شدهٔ کالای تحویلی — " + title);
                Cr(SalesAccountRoles.Inventory, goodsCost, "خروج کالا — " + title);
                break;
            case SalesDocType.Return:
                Dr(SalesAccountRoles.Return, doc.Taxable, title);
                Dr(SalesAccountRoles.Vat, doc.Vat, "برگشت مالیات — " + title);
                Cr(SalesAccountRoles.Receivable, doc.Total, title);
                Dr(SalesAccountRoles.Inventory, goodsCost, "ورود کالای برگشتی — " + title);
                Cr(SalesAccountRoles.Cogs, goodsCost, "برگشت بهای تمام‌شده — " + title);
                break;
        }

        var errors = new List<string>();
        foreach (var role in rows.Select(r => r.Role).Distinct().Where(r => !ctx.Maps.ContainsKey(r)))
            errors.Add($"حساب نقش «{RoleTitle(role)}» در تنظیمات فروش تعریف نشده است.");
        if (errors.Count > 0) return new SalesJournal([], errors);

        var accounts = await _accounts.GetByIdsAsync(rows.Select(r => ctx.Maps[r.Role].AccountId).Distinct().ToList(), ct);
        var detailIds = ctx.Maps.Values.Where(m => m.TafsiliId is not null).Select(m => m.TafsiliId!.Value).Append(customer.TafsiliId).Distinct().ToList();
        var details = await _details.GetByIdsAsync(detailIds, doc.VahedCode, ct);

        var lines = new List<SalesJournalLine>();
        foreach (var r in rows)
        {
            var map = ctx.Maps[r.Role];
            if (!accounts.TryGetValue(map.AccountId, out var acc) || !acc.IsActive)
            {
                errors.Add($"حساب نقش «{RoleTitle(r.Role)}» فعال نیست.");
                continue;
            }
            var links = new List<VoucherDraftDetail>();
            foreach (var level in acc.DetailLevels.OrderBy(x => x.Level))
            {
                DetailInfo? pick = null;
                if (map.TafsiliId is { } f && details.TryGetValue(f, out var fd) && fd.DetailGroupIds.Overlaps(level.DetailGroupIds)) pick = fd;
                else if (details.TryGetValue(customer.TafsiliId, out var cd) && cd.DetailGroupIds.Overlaps(level.DetailGroupIds) && links.All(x => x.DetailId != cd.Id)) pick = cd;
                if (pick is not null) links.Add(new VoucherDraftDetail(level.Level, level.LevelId, pick.Id, pick.Title));
                else if (level.IsRequired) errors.Add($"حساب «{acc.Title}» تفصیلی سطح {level.Level} لازم دارد؛ در نگاشت حساب فروش تفصیلی ثابت بدهید.");
            }
            lines.Add(new SalesJournalLine(r.Role, acc.Id, acc.Code, acc.Title, r.Dr, r.Cr, r.Desc, links));
        }
        var j = new SalesJournal(lines, errors);
        if (errors.Count == 0 && j.Debit != j.Credit) errors.Add("سند حسابداری تراز نیست.");
        return j;
    }

    public static string RoleTitle(string role) => role switch
    {
        SalesAccountRoles.Receivable => "حساب‌های دریافتنی",
        SalesAccountRoles.Sales => "فروش کالا",
        SalesAccountRoles.Service => "درآمد خدمات",
        SalesAccountRoles.Discount => "تخفیفات فروش",
        SalesAccountRoles.Return => "برگشت از فروش",
        SalesAccountRoles.Vat => "مالیات بر ارزش افزوده",
        SalesAccountRoles.Contract => "بدهی قراردادی",
        SalesAccountRoles.Cogs => "بهای تمام‌شده",
        SalesAccountRoles.Inventory => "موجودی کالا",
        _ => role,
    };
}
