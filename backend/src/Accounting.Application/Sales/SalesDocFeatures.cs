using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Sales;
using MediatR;

namespace Accounting.Application.Sales;

// ═══════════════ ماژول فروش — چرخهٔ سند: ذخیره، ثبت، تأیید/رد، قطعی‌سازی، ابطال، تبدیل ═══════════════

public sealed record SalesDocLineInput(Guid ItemId, decimal Qty, long Price, decimal DiscountPercent);

public sealed record SalesDocInput(
    SalesDocType DocType, string DocDate, Guid CustomerId, Guid? SourceId, int PayTerms, string? ValidUntil, string? DeliveryDate,
    SalesType SalesType, string? RefNo, string? Note, string? PrintNote, long HeaderDiscount, IReadOnlyList<SalesDocLineInput> Lines);

public sealed record SalesDocResult(
    bool Success, Guid? Id, string? DocNo, SalesDocStatus? Status, IReadOnlyList<SalesCheck> Checks, IReadOnlyList<string> Errors,
    Guid? VoucherId = null, string? VoucherNo = null)
{
    public static SalesDocResult Fail(IEnumerable<string> errors, IReadOnlyList<SalesCheck>? checks = null) =>
        new(false, null, null, null, checks ?? [], errors.ToList());
}

public sealed record SalesDocListRow(
    Guid Id, SalesDocType DocType, string? DocNo, string DocDate, Guid CustomerId, string? CustomerName, SalesDocStatus Status,
    long Total, Guid? SourceId, Guid? VoucherId, string CreatedBy);

public sealed record SalesDocPage(IReadOnlyList<SalesDocListRow> Items, int Total, int Page, int PageSize);

public sealed record SalesDocLineDto(
    Guid ItemId, string? ItemCode, string? ItemName, SalesItemKind? Kind, string? UnitName, string? TaxCode, decimal Stock,
    decimal Qty, long Price, decimal DiscountPercent, long Gross, long LineDiscount, long HeaderDiscountShare, long Taxable,
    decimal VatRate, long Vat, long Total);

public sealed record SalesChainNode(Guid Id, SalesDocType DocType, string? DocNo, SalesDocStatus Status, int Depth);

public sealed record SalesConversionOption(SalesDocType Target, bool Available, string? Reason);

public sealed record SalesHistoryDto(string Action, string By, DateTime At);

public sealed record SalesDocPermissions(bool Edit, bool Submit, bool Approve, bool Reject, bool Post, bool Void, bool Convert);

public sealed record SalesDocDto(
    Guid Id, SalesDocType DocType, string? DocNo, string DocDate, SalesDocStatus Status, SalesCustomerDto? Customer,
    Guid? SourceId, int PayTerms, string? DueDate, string? ValidUntil, string? DeliveryDate, SalesType SalesType, string? RefNo,
    string? Note, string? PrintNote, long HeaderDiscount, long Gross, long Discount, long Taxable, long Vat, long Total,
    IReadOnlyList<string> ApprovalReasons, Guid? VoucherId, string CreatedBy, string? ApprovedBy, string? PostedBy, DateTime CreatedAtUtc,
    IReadOnlyList<SalesDocLineDto> Lines, IReadOnlyList<SalesChainNode> Chain, IReadOnlyList<SalesConversionOption> Conversions,
    IReadOnlyList<SalesJournalLine> Journal, IReadOnlyList<string> JournalErrors, IReadOnlyList<SalesCheck> Checks,
    IReadOnlyList<SalesHistoryDto> History, SalesDocPermissions Can);

/// <summary>ساخت و محاسبهٔ سند از ورودی — مشترک بین پیش‌نمایش و ذخیره.</summary>
public sealed class SalesDocBuilder
{
    private readonly ISalesRepository _repo;
    private readonly SalesDocEngine _engine;

    public SalesDocBuilder(ISalesRepository repo, SalesDocEngine engine) { _repo = repo; _engine = engine; }

    /// <returns>خطاهای ساختاری (بدون آن‌ها سند ذخیره‌شدنی نیست)؛ سند و ردیف‌ها روی <paramref name="doc"/> نوشته می‌شوند.</returns>
    public async Task<(List<string> Errors, IReadOnlyDictionary<Guid, SalesItem> Items, SalesCustomerRow? Customer)> ApplyAsync(
        SalesDoc doc, SalesDocInput input, SalesContext ctx, CancellationToken ct)
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(input.DocType)) errors.Add("نوع سند نامعتبر است.");
        if (!SalesDates.IsValid(input.DocDate)) errors.Add("تاریخ سند معتبر نیست (yyyyMMdd شمسی).");
        if (input.Lines is null || input.Lines.Count == 0) errors.Add("حداقل یک ردیف لازم است.");
        if (input.Lines?.Count > 300) errors.Add("حداکثر ۳۰۰ ردیف در هر سند.");
        if (errors.Count > 0) return (errors, new Dictionary<Guid, SalesItem>(), null);

        SalesDoc? src = null;
        if (input.SourceId is { } sid)
        {
            src = await _repo.GetDocAsync(sid, false, ct);
            if (src is null || src.VahedCode != doc.VahedCode || src.IsDeleted) errors.Add("سند مبدأ پیدا نشد.");
        }
        var customerId = src?.CustomerId ?? input.CustomerId;
        var customer = (await _repo.ListCustomersAsync(doc.VahedCode, null, [customerId], ct)).FirstOrDefault();
        if (customer is null) errors.Add("مشتری پیدا نشد (پروفایل فروش مشتری را بسازید).");

        var items = await _repo.GetItemsAsync(input.Lines!.Select(l => l.ItemId).Concat(src?.Lines.Select(l => l.ItemId) ?? []).Distinct().ToList(), false, ct);
        if (input.Lines!.Any(l => !items.TryGetValue(l.ItemId, out var it) || it.VahedCode != doc.VahedCode || it.IsDeleted))
            errors.Add("یکی از کالاها پیدا نشد.");
        if (errors.Count > 0) return (errors, items, customer);

        doc.DocType = input.DocType;
        doc.DocDate = input.DocDate;
        doc.Year = input.DocDate[..4];
        doc.CustomerId = customerId;
        doc.SourceId = input.SourceId;
        doc.PayTerms = Math.Clamp(input.PayTerms, 0, 3650);
        doc.ValidUntil = doc.DocType == SalesDocType.ProForma ? SalesDefaults.Clean(input.ValidUntil, 8) : null;
        doc.DeliveryDate = SalesDefaults.Clean(input.DeliveryDate, 8);
        doc.SalesType = Enum.IsDefined(input.SalesType) ? input.SalesType : SalesType.Domestic;
        doc.RefNo = SalesDefaults.Clean(input.RefNo, 50);
        doc.Note = SalesDefaults.Clean(input.Note, 1000);
        doc.PrintNote = SalesDefaults.Clean(input.PrintNote, 1000);
        // سند تبدیلی: قیمت، تخفیف و تخفیف کلی از مبدأ (قیمت تأییدشده عوض نمی‌شود؛ فقط مقدار کم می‌شود).
        doc.HeaderDiscount = src is null ? Math.Max(0, input.HeaderDiscount) : 0;

        var lines = input.Lines!.Select((l, i) =>
        {
            var sl = src?.Lines.FirstOrDefault(x => x.ItemId == l.ItemId);
            return new SalesDocLine
            {
                Id = Guid.NewGuid(),
                DocId = doc.Id,
                Radif = i + 1,
                ItemId = l.ItemId,
                Qty = Math.Round(l.Qty, 3),
                Price = sl?.Price ?? l.Price,
                DiscountPercent = sl?.DiscountPercent ?? Math.Round(l.DiscountPercent, 2),
            };
        }).ToList();
        if (src is not null && src.HeaderDiscount > 0 && src.Gross > 0)
        {
            // سهم تخفیف کلی مبدأ به نسبت مقدار منتقل‌شده
            var share = lines.Sum(l => src.Lines.Where(x => x.ItemId == l.ItemId).Sum(x => x.Qty == 0 ? 0 : x.HeaderDiscountShare * Math.Min(1, l.Qty / x.Qty)));
            doc.HeaderDiscount = (long)Math.Round(share, MidpointRounding.AwayFromZero);
        }

        var rate = SalesCalculator.VatRateFor(ctx.Rates, doc.DocDate);
        var t = SalesCalculator.Apply(lines, doc.HeaderDiscount, rate, id => items.GetValueOrDefault(id)?.IsExempt ?? false);
        (doc.Gross, doc.Discount, doc.Taxable, doc.Vat, doc.Total) = (t.Gross, t.Discount, t.Taxable, t.Vat, t.Total);
        doc.Lines = lines;
        return (errors, items, customer);
    }
}

// ─────────── فهرست ───────────

public sealed record ListSalesDocsQuery(
    SalesDocType? Type = null, SalesDocStatus? Status = null, Guid? CustomerId = null, string? Search = null,
    string? FromDate = null, string? ToDate = null, int Page = 1, int PageSize = 25) : IRequest<SalesDocPage>, IVahedScopedQuery
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListSalesDocsHandler : IRequestHandler<ListSalesDocsQuery, SalesDocPage>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    public ListSalesDocsHandler(ISalesRepository repo, SalesAccess access) { _repo = repo; _access = access; }

    public async Task<SalesDocPage> Handle(ListSalesDocsQuery q, CancellationToken ct)
    {
        await _access.RequireAnyAsync(q.VahedCode, ct);
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 200);
        var (items, total) = await _repo.ListDocsAsync(new SalesDocFilter(q.VahedCode, q.Type, q.Status, q.CustomerId, q.Search?.Trim(), q.FromDate, q.ToDate, (page - 1) * size, size), ct);
        var customers = (await _repo.ListCustomersAsync(q.VahedCode, null, items.Select(d => d.CustomerId).Distinct().ToList(), ct)).ToDictionary(c => c.Customer.Id);
        return new SalesDocPage(items.Select(d => new SalesDocListRow(d.Id, d.DocType, d.DocNo, d.DocDate, d.CustomerId,
            customers.GetValueOrDefault(d.CustomerId)?.TafsiliName, d.Status, d.Total, d.SourceId, d.VoucherId, d.CreatedBy)).ToList(), total, page, size);
    }
}

// ─────────── جزئیات ───────────

public sealed record GetSalesDocQuery(Guid Id) : IRequest<SalesDocDto>, IVahedScopedQuery
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
    [JsonIgnore] public string? Year { get; set; }
}

public sealed class GetSalesDocHandler : IRequestHandler<GetSalesDocQuery, SalesDocDto>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly SalesDocEngine _engine;

    public GetSalesDocHandler(ISalesRepository repo, SalesAccess access, SalesDocEngine engine) { _repo = repo; _access = access; _engine = engine; }

    public async Task<SalesDocDto> Handle(GetSalesDocQuery q, CancellationToken ct)
    {
        var access = await _access.RequireAnyAsync(q.VahedCode, ct);
        var doc = await _repo.GetDocAsync(q.Id, false, ct);
        if (doc is null || doc.VahedCode != q.VahedCode || doc.IsDeleted) throw new NotFoundException("SalesDoc", q.Id);
        var ctx = await _engine.ContextAsync(q.VahedCode, ct);
        var items = await _repo.GetItemsAsync(doc.Lines.Select(l => l.ItemId).Distinct().ToList(), false, ct);
        var customer = (await _repo.ListCustomersAsync(q.VahedCode, null, [doc.CustomerId], ct)).FirstOrDefault();

        // زنجیره: از ریشه تا همهٔ نوادگان
        var root = doc;
        for (var guard = 0; root.SourceId is { } sid && guard < 10; guard++)
            root = await _repo.GetDocAsync(sid, false, ct) ?? root;
        var chain = new List<SalesChainNode>();
        async Task Walk(SalesDoc d, int depth)
        {
            chain.Add(new SalesChainNode(d.Id, d.DocType, d.DocNo, d.Status, depth));
            if (depth > 6) return;
            foreach (var k in (await _repo.GetChildrenAsync([d.Id], ct)).OrderBy(k => k.CreatedAtUtc)) await Walk(k, depth + 1);
        }
        await Walk(root, 0);
        var children = (await _repo.GetChildrenAsync([doc.Id], ct)).ToList();

        var conversions = new List<SalesConversionOption>();
        if (SalesDocEngine.IsConvertible(doc))
            foreach (var t in SalesDocEngine.TargetsOf(doc.DocType))
            {
                var rem = await _engine.RemainingAsync(doc, t, null, ctx.Setting.BillBeforeDelivery, items, ct);
                conversions.Add(new SalesConversionOption(t, rem.Count > 0, rem.Count > 0 ? null
                    : t == SalesDocType.Invoice && doc.DocType == SalesDocType.Order ? "اقلام کالا اول باید تحویل شوند (یا «فاکتور پیش از تحویل» را در تنظیمات فعال کنید)." : "مقدار باقی‌مانده‌ای نیست."));
            }

        IReadOnlyList<SalesJournalLine> journal = [];
        IReadOnlyList<string> journalErrors = [];
        if (SalesLabels.IsPostable(doc.DocType) && customer is not null && doc.Status != SalesDocStatus.Void)
        {
            var j = await _engine.JournalAsync(doc, ctx, items, customer.Customer, ct);
            (journal, journalErrors) = (j.Lines, j.Errors);
        }

        IReadOnlyList<SalesCheck> checks = [];
        if (doc.Status is SalesDocStatus.Draft or SalesDocStatus.Pending or SalesDocStatus.Approved)
            checks = (await _engine.CheckAsync(doc, ctx, items, customer, doc.Status == SalesDocStatus.Approved && SalesLabels.IsPostable(doc.DocType), q.Year, ct)).Checks;

        var sod = ctx.Setting.SegregationOfDuties && doc.CreatedBy == access.UserId;
        var can = new SalesDocPermissions(
            Edit: doc.Status == SalesDocStatus.Draft && access.Has(SalesRole.Seller),
            Submit: doc.Status == SalesDocStatus.Draft && access.Has(SalesRole.Seller),
            Approve: doc.Status == SalesDocStatus.Pending && access.Has(SalesRole.Manager) && !sod,
            Reject: doc.Status == SalesDocStatus.Pending && access.Has(SalesRole.Manager) && !sod,
            Post: doc.Status == SalesDocStatus.Approved && SalesLabels.IsPostable(doc.DocType) && access.Has(SalesRole.Accountant),
            Void: doc.Status is SalesDocStatus.Draft or SalesDocStatus.Pending or SalesDocStatus.Approved && children.Count == 0
                  && (access.Has(SalesRole.Seller) || access.Has(SalesRole.Manager)),
            Convert: access.Has(SalesRole.Seller));

        var history = (await _repo.ListHistoryAsync(doc.Id, ct)).Select(h => new SalesHistoryDto(h.Action, h.CreatedBy, h.CreatedAtUtc)).ToList();
        var lines = doc.Lines.OrderBy(l => l.Radif).Select(l =>
        {
            var it = items.GetValueOrDefault(l.ItemId);
            return new SalesDocLineDto(l.ItemId, it?.Code, it?.Name, it?.Kind, it?.UnitName, it?.TaxCode, it?.Stock ?? 0, l.Qty, l.Price,
                l.DiscountPercent, l.Gross, l.LineDiscount, l.HeaderDiscountShare, l.Taxable, l.VatRate, l.Vat, l.Total);
        }).ToList();

        return new SalesDocDto(doc.Id, doc.DocType, doc.DocNo, doc.DocDate, doc.Status,
            customer is null ? null : ListSalesCustomersHandler.ToDto(customer), doc.SourceId, doc.PayTerms,
            doc.DocType == SalesDocType.Invoice && SalesDates.IsValid(doc.DocDate) ? SalesDates.AddDays(doc.DocDate, doc.PayTerms) : null,
            doc.ValidUntil, doc.DeliveryDate, doc.SalesType, doc.RefNo, doc.Note, doc.PrintNote, doc.HeaderDiscount, doc.Gross,
            doc.Discount, doc.Taxable, doc.Vat, doc.Total, SplitReasons(doc.ApprovalReasons), doc.VoucherId, doc.CreatedBy,
            doc.ApprovedBy, doc.PostedBy, doc.CreatedAtUtc, lines, chain, conversions, journal, journalErrors, checks, history, can);
    }

    internal static IReadOnlyList<string> SplitReasons(string? s) =>
        string.IsNullOrWhiteSpace(s) ? [] : s.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

// ─────────── پیش‌نمایش (کنترل زندهٔ فرم) ───────────

public sealed record SalesDocPreviewDto(long Gross, long Discount, long Taxable, long Vat, long Total, decimal VatRate,
    IReadOnlyList<SalesDocLineDto> Lines, IReadOnlyList<SalesCheck> Checks, IReadOnlyList<string> ApprovalReasons,
    long? CustomerBalance, long? CustomerOpenOrders, long? CreditLimit);

public sealed record PreviewSalesDocQuery(Guid? Id, SalesDocInput Input) : IRequest<SalesDocPreviewDto>, IVahedScopedQuery
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
    [JsonIgnore] public string? Year { get; set; }
}

public sealed class PreviewSalesDocHandler : IRequestHandler<PreviewSalesDocQuery, SalesDocPreviewDto>
{
    private readonly SalesAccess _access;
    private readonly SalesDocEngine _engine;
    private readonly SalesDocBuilder _builder;

    public PreviewSalesDocHandler(SalesAccess access, SalesDocEngine engine, SalesDocBuilder builder) { _access = access; _engine = engine; _builder = builder; }

    public async Task<SalesDocPreviewDto> Handle(PreviewSalesDocQuery q, CancellationToken ct)
    {
        await _access.RequireAnyAsync(q.VahedCode, ct);
        var ctx = await _engine.ContextAsync(q.VahedCode, ct);
        var doc = new SalesDoc { Id = q.Id ?? Guid.NewGuid(), VahedCode = q.VahedCode };
        var (errors, items, customer) = await _builder.ApplyAsync(doc, q.Input, ctx, ct);
        if (errors.Count > 0)
            return new SalesDocPreviewDto(0, 0, 0, 0, 0, 0, [], errors.Select(e => new SalesCheck("err", e)).ToList(), [], null, null, null);
        var (checks, reasons) = await _engine.CheckAsync(doc, ctx, items, customer, false, q.Year, ct);
        (long Balance, long Open)? exposure = customer is null ? null : await _engine.ExposureAsync(customer.Customer, ctx, doc.Id, doc.Year, ct);
        var lines = doc.Lines.Select(l =>
        {
            var it = items.GetValueOrDefault(l.ItemId);
            return new SalesDocLineDto(l.ItemId, it?.Code, it?.Name, it?.Kind, it?.UnitName, it?.TaxCode, it?.Stock ?? 0, l.Qty, l.Price,
                l.DiscountPercent, l.Gross, l.LineDiscount, l.HeaderDiscountShare, l.Taxable, l.VatRate, l.Vat, l.Total);
        }).ToList();
        return new SalesDocPreviewDto(doc.Gross, doc.Discount, doc.Taxable, doc.Vat, doc.Total,
            SalesCalculator.VatRateFor(ctx.Rates, doc.DocDate), lines, checks, reasons,
            exposure?.Balance, exposure?.Open, customer?.Customer.CreditLimit);
    }
}

// ─────────── ذخیرهٔ پیش‌نویس (ایجاد/ویرایش) ───────────

public sealed record SaveSalesDocCommand(SalesDocInput Input) : IRequest<SalesDocResult>, IVahedScopedCommand
{
    [JsonIgnore] public Guid? Id { get; set; }
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
    [JsonIgnore] public string? Year { get; set; }
}

public sealed class SaveSalesDocHandler : IRequestHandler<SaveSalesDocCommand, SalesDocResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly SalesDocEngine _engine;
    private readonly SalesDocBuilder _builder;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public SaveSalesDocHandler(ISalesRepository repo, SalesAccess access, SalesDocEngine engine, SalesDocBuilder builder, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _engine = engine; _builder = builder; _uow = uow; _user = user; }

    public async Task<SalesDocResult> Handle(SaveSalesDocCommand c, CancellationToken ct)
    {
        await _access.RequireAsync(c.VahedCode, SalesRole.Seller, ct);
        var ctx = await _engine.ContextAsync(c.VahedCode, ct);
        SalesDoc doc;
        var isNew = c.Id is null;
        if (c.Id is { } id)
        {
            doc = await _repo.GetDocAsync(id, true, ct) ?? throw new NotFoundException("SalesDoc", id);
            if (doc.VahedCode != c.VahedCode || doc.IsDeleted) throw new NotFoundException("SalesDoc", id);
            if (doc.Status != SalesDocStatus.Draft) return SalesDocResult.Fail(["فقط پیش‌نویس قابل ویرایش است."]);
            if (c.Input.DocType != doc.DocType || c.Input.SourceId != doc.SourceId) return SalesDocResult.Fail(["نوع سند و سند مبدأ پس از ایجاد عوض نمی‌شوند."]);
        }
        else
        {
            doc = new SalesDoc { Id = Guid.NewGuid(), VahedCode = c.VahedCode, Status = SalesDocStatus.Draft, CreatedBy = _user.UserId, CreatedAtUtc = DateTime.UtcNow };
            if (c.Input.DocType == SalesDocType.Delivery && c.Input.SourceId is null)
                return SalesDocResult.Fail(["حواله تحویل فقط از سفارش فروش صادر می‌شود."]);
            if (c.Input.DocType == SalesDocType.Return && c.Input.SourceId is null)
                return SalesDocResult.Fail(["برگشت از فروش فقط از فاکتور قطعی صادر می‌شود."]);
        }

        var oldLines = doc.Lines.ToList();
        var (errors, items, customer) = await _builder.ApplyAsync(doc, c.Input, ctx, ct);
        if (errors.Count > 0) return SalesDocResult.Fail(errors);

        if (isNew) await _repo.AddDocAsync(doc, ct);
        else
        {
            _repo.RemoveLines(oldLines);
            _repo.AddLines(doc.Lines);
            doc.UpdatedAtUtc = DateTime.UtcNow;
        }
        await SalesHistory.AddAsync(_repo, doc, isNew ? (doc.SourceId is null ? "ایجاد پیش‌نویس" : "صدور از سند مبدأ") : "ویرایش پیش‌نویس", _user, ct);
        await _uow.SaveChangesAsync(ct);
        var (checks, _) = await _engine.CheckAsync(doc, ctx, items, customer, false, c.Year, ct);
        return new SalesDocResult(true, doc.Id, doc.DocNo, doc.Status, checks, []);
    }
}

// ─────────── ثبت (ارسال) ───────────

public sealed record SubmitSalesDocCommand : IRequest<SalesDocResult>, IVahedScopedCommand
{
    [JsonIgnore] public Guid Id { get; set; }
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
    [JsonIgnore] public string? Year { get; set; }
}

public sealed class SubmitSalesDocHandler : IRequestHandler<SubmitSalesDocCommand, SalesDocResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly SalesDocEngine _engine;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public SubmitSalesDocHandler(ISalesRepository repo, SalesAccess access, SalesDocEngine engine, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _engine = engine; _uow = uow; _user = user; }

    public async Task<SalesDocResult> Handle(SubmitSalesDocCommand c, CancellationToken ct)
    {
        await _access.RequireAsync(c.VahedCode, SalesRole.Seller, ct);
        var doc = await SalesHistory.LoadAsync(_repo, c.Id, c.VahedCode, ct);
        if (doc.Status != SalesDocStatus.Draft) return SalesDocResult.Fail(["فقط پیش‌نویس ثبت می‌شود."]);
        var ctx = await _engine.ContextAsync(c.VahedCode, ct);
        var items = await _repo.GetItemsAsync(doc.Lines.Select(l => l.ItemId).ToList(), false, ct);
        var customer = (await _repo.ListCustomersAsync(c.VahedCode, null, [doc.CustomerId], ct)).FirstOrDefault();

        // نرخ مالیات ممکن است از زمان ذخیرهٔ پیش‌نویس عوض شده باشد.
        var t = SalesCalculator.Apply(doc.Lines, doc.HeaderDiscount, SalesCalculator.VatRateFor(ctx.Rates, doc.DocDate), id => items.GetValueOrDefault(id)?.IsExempt ?? false);
        (doc.Gross, doc.Discount, doc.Taxable, doc.Vat, doc.Total) = (t.Gross, t.Discount, t.Taxable, t.Vat, t.Total);

        var (checks, reasons) = await _engine.CheckAsync(doc, ctx, items, customer, false, c.Year, ct);
        if (checks.Any(x => x.Level == "err")) return SalesDocResult.Fail(checks.Where(x => x.Level == "err").Select(x => x.Message), checks);

        doc.Status = reasons.Count > 0 ? SalesDocStatus.Pending : SalesDocStatus.Approved;
        doc.ApprovalReasons = reasons.Count > 0 ? string.Join("\n", reasons) : null;
        if (!SalesLabels.IsPostable(doc.DocType) && doc.DocNo is null)
            doc.DocNo = await SalesHistory.NextNoAsync(_repo, doc, ct);
        doc.UpdatedAtUtc = DateTime.UtcNow;
        await SalesHistory.AddAsync(_repo, doc, reasons.Count > 0 ? "ارسال برای تأیید: " + string.Join("، ", reasons) : "ثبت — بدون نیاز به تأیید", _user, ct);
        await SalesHistory.SaveAsync(_uow, ct);
        return new SalesDocResult(true, doc.Id, doc.DocNo, doc.Status, checks, []);
    }
}

// ─────────── تأیید / رد ───────────

public sealed record ApproveSalesDocCommand : IRequest<SalesDocResult>, IVahedScopedCommand
{
    [JsonIgnore] public Guid Id { get; set; }
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
    [JsonIgnore] public string? Year { get; set; }
}

public sealed class ApproveSalesDocHandler : IRequestHandler<ApproveSalesDocCommand, SalesDocResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly SalesDocEngine _engine;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public ApproveSalesDocHandler(ISalesRepository repo, SalesAccess access, SalesDocEngine engine, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _engine = engine; _uow = uow; _user = user; }

    public async Task<SalesDocResult> Handle(ApproveSalesDocCommand c, CancellationToken ct)
    {
        await _access.RequireAsync(c.VahedCode, SalesRole.Manager, ct);
        var doc = await SalesHistory.LoadAsync(_repo, c.Id, c.VahedCode, ct);
        if (doc.Status != SalesDocStatus.Pending) return SalesDocResult.Fail(["فقط سند «در انتظار تأیید» تأیید می‌شود."]);
        var ctx = await _engine.ContextAsync(c.VahedCode, ct);
        if (ctx.Setting.SegregationOfDuties && doc.CreatedBy == _user.UserId)
            throw new RoleAccessDeniedException("تفکیک وظایف: ایجادکنندهٔ سند نمی‌تواند آن را تأیید کند.");
        var items = await _repo.GetItemsAsync(doc.Lines.Select(l => l.ItemId).ToList(), false, ct);
        var customer = (await _repo.ListCustomersAsync(c.VahedCode, null, [doc.CustomerId], ct)).FirstOrDefault();
        var (checks, _) = await _engine.CheckAsync(doc, ctx, items, customer, false, c.Year, ct);
        if (checks.Any(x => x.Level == "err")) return SalesDocResult.Fail(checks.Where(x => x.Level == "err").Select(x => x.Message), checks);

        doc.Status = SalesDocStatus.Approved;
        doc.ApprovedBy = _user.UserId;
        doc.UpdatedAtUtc = DateTime.UtcNow;
        await SalesHistory.AddAsync(_repo, doc, "تأیید سند", _user, ct);
        await SalesHistory.SaveAsync(_uow, ct);
        return new SalesDocResult(true, doc.Id, doc.DocNo, doc.Status, checks, []);
    }
}

public sealed record RejectSalesDocCommand(string? Reason) : IRequest<SalesDocResult>, IVahedScopedCommand
{
    [JsonIgnore] public Guid Id { get; set; }
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class RejectSalesDocHandler : IRequestHandler<RejectSalesDocCommand, SalesDocResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public RejectSalesDocHandler(ISalesRepository repo, SalesAccess access, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _uow = uow; _user = user; }

    public async Task<SalesDocResult> Handle(RejectSalesDocCommand c, CancellationToken ct)
    {
        await _access.RequireAsync(c.VahedCode, SalesRole.Manager, ct);
        var doc = await SalesHistory.LoadAsync(_repo, c.Id, c.VahedCode, ct);
        if (doc.Status != SalesDocStatus.Pending) return SalesDocResult.Fail(["فقط سند «در انتظار تأیید» رد می‌شود."]);
        doc.Status = SalesDocStatus.Draft;
        doc.UpdatedAtUtc = DateTime.UtcNow;
        var reason = SalesDefaults.Clean(c.Reason, 300);
        await SalesHistory.AddAsync(_repo, doc, "رد و بازگشت به پیش‌نویس" + (reason is null ? "" : ": " + reason), _user, ct);
        await SalesHistory.SaveAsync(_uow, ct);
        return new SalesDocResult(true, doc.Id, doc.DocNo, doc.Status, [], []);
    }
}

// ─────────── قطعی‌سازی: شماره، سند حسابداری، موجودی — یک تراکنش ───────────

public sealed record PostSalesDocCommand : IRequest<SalesDocResult>, IVahedScopedCommand
{
    [JsonIgnore] public Guid Id { get; set; }
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
    [JsonIgnore] public string? Year { get; set; }
}

public sealed class PostSalesDocHandler : IRequestHandler<PostSalesDocCommand, SalesDocResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly SalesDocEngine _engine;
    private readonly IVoucherWriter _writer;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public PostSalesDocHandler(ISalesRepository repo, SalesAccess access, SalesDocEngine engine, IVoucherWriter writer, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _engine = engine; _writer = writer; _uow = uow; _user = user; }

    public async Task<SalesDocResult> Handle(PostSalesDocCommand c, CancellationToken ct)
    {
        await _access.RequireAsync(c.VahedCode, SalesRole.Accountant, ct);
        var doc = await SalesHistory.LoadAsync(_repo, c.Id, c.VahedCode, ct);
        if (!SalesLabels.IsPostable(doc.DocType)) return SalesDocResult.Fail([SalesLabels.Type(doc.DocType) + " اثر حسابداری ندارد و قطعی نمی‌شود."]);
        if (doc.Status != SalesDocStatus.Approved) return SalesDocResult.Fail(["فقط سند تأییدشده قطعی می‌شود."]);

        var ctx = await _engine.ContextAsync(c.VahedCode, ct);
        var items = await _repo.GetItemsAsync(doc.Lines.Select(l => l.ItemId).ToList(), true, ct);
        var customer = (await _repo.ListCustomersAsync(c.VahedCode, null, [doc.CustomerId], ct)).FirstOrDefault();
        if (customer is null) return SalesDocResult.Fail(["مشتری سند پیدا نشد."]);
        var (checks, _) = await _engine.CheckAsync(doc, ctx, items, customer, true, c.Year, ct);
        if (checks.Any(x => x.Level == "err")) return SalesDocResult.Fail(checks.Where(x => x.Level == "err").Select(x => x.Message), checks);

        foreach (var l in doc.Lines)
            l.UnitCost = items.GetValueOrDefault(l.ItemId) is { Kind: SalesItemKind.Goods } it ? it.StandardCost : 0;
        doc.DocNo ??= await SalesHistory.NextNoAsync(_repo, doc, ct);

        var journal = await _engine.JournalAsync(doc, ctx, items, customer.Customer, ct);
        if (journal.Errors.Count > 0) return SalesDocResult.Fail(journal.Errors, checks);

        Guid? voucherId = null;
        string? voucherNo = null;
        if (journal.Lines.Count > 0)
        {
            var draft = new VoucherDraft(
                c.VahedCode, SalesDates.ToDate(doc.DocDate),
                $"{SalesLabels.Type(doc.DocType)} {doc.DocNo} — {customer.TafsiliName}",
                "SALES",
                journal.Lines.Select(l => new VoucherDraftLine(l.AccountId, l.AccountTitle ?? "", l.Details, l.Debit, l.Credit, l.Description, l.AccountCode)).ToList(),
                SystemTypeId: ctx.Setting.SystemTypeId);
            (var vid, voucherNo) = await _writer.CreateDraftVoucherAsync(draft, ct);
            voucherId = vid;
        }

        // موجودی: حواله و فاکتور مستقیم خارج می‌کنند، برگشت وارد می‌کند.
        var sign = doc.DocType switch
        {
            SalesDocType.Delivery => -1,
            SalesDocType.Invoice when doc.SourceId is null => -1,
            SalesDocType.Return => 1,
            _ => 0,
        };
        if (sign != 0)
            foreach (var l in doc.Lines)
                if (items.GetValueOrDefault(l.ItemId) is { Kind: SalesItemKind.Goods } it)
                    it.Stock += sign * l.Qty;

        doc.Status = SalesDocStatus.Posted;
        doc.PostedBy = _user.UserId;
        doc.VoucherId = voucherId;
        doc.UpdatedAtUtc = DateTime.UtcNow;
        await SalesHistory.AddAsync(_repo, doc, voucherNo is null ? "قطعی‌سازی (بدون اثر حسابداری)" : $"قطعی‌سازی و ثبت سند حسابداری {voucherNo} ({journal.Debit:N0} ریال)", _user, ct);
        await SalesHistory.SaveAsync(_uow, ct);
        return new SalesDocResult(true, doc.Id, doc.DocNo, doc.Status, checks, [], voucherId, voucherNo);
    }
}

// ─────────── ابطال ───────────

public sealed record VoidSalesDocCommand : IRequest<SalesDocResult>, IVahedScopedCommand
{
    [JsonIgnore] public Guid Id { get; set; }
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class VoidSalesDocHandler : IRequestHandler<VoidSalesDocCommand, SalesDocResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public VoidSalesDocHandler(ISalesRepository repo, SalesAccess access, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _uow = uow; _user = user; }

    public async Task<SalesDocResult> Handle(VoidSalesDocCommand c, CancellationToken ct)
    {
        var a = await _access.RequireAnyAsync(c.VahedCode, ct);
        if (!a.Has(SalesRole.Seller) && !a.Has(SalesRole.Manager)) throw new RoleAccessDeniedException("ابطال سند فروش نقش کارشناس یا مدیر فروش را لازم دارد.");
        var doc = await SalesHistory.LoadAsync(_repo, c.Id, c.VahedCode, ct);
        if (doc.Status == SalesDocStatus.Posted) return SalesDocResult.Fail(["سند قطعی باطل نمی‌شود؛ برگشت از فروش صادر کنید."]);
        if (doc.Status == SalesDocStatus.Void) return SalesDocResult.Fail(["سند قبلاً باطل شده است."]);
        if ((await _repo.GetChildrenAsync([doc.Id], ct)).Count > 0) return SalesDocResult.Fail(["از این سند، سند دیگری صادر شده است؛ اول آن را باطل کنید."]);
        doc.Status = SalesDocStatus.Void;
        doc.UpdatedAtUtc = DateTime.UtcNow;
        await SalesHistory.AddAsync(_repo, doc, "ابطال سند", _user, ct);
        await SalesHistory.SaveAsync(_uow, ct);
        return new SalesDocResult(true, doc.Id, doc.DocNo, doc.Status, [], []);
    }
}

// ─────────── پیش‌نویس تبدیل: مقادیر باقی‌ماندهٔ مبدأ ───────────

public sealed record GetSalesConversionQuery(Guid SourceId, SalesDocType Target) : IRequest<SalesDocInput>, IVahedScopedQuery
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetSalesConversionHandler : IRequestHandler<GetSalesConversionQuery, SalesDocInput>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly SalesDocEngine _engine;

    public GetSalesConversionHandler(ISalesRepository repo, SalesAccess access, SalesDocEngine engine) { _repo = repo; _access = access; _engine = engine; }

    public async Task<SalesDocInput> Handle(GetSalesConversionQuery q, CancellationToken ct)
    {
        await _access.RequireAsync(q.VahedCode, SalesRole.Seller, ct);
        var src = await SalesHistory.LoadAsync(_repo, q.SourceId, q.VahedCode, ct);
        if (!SalesDocEngine.IsConvertible(src) || !SalesDocEngine.TargetsOf(src.DocType).Contains(q.Target))
            throw new BusinessRuleException("این سند به نوع خواسته‌شده تبدیل نمی‌شود.");
        var ctx = await _engine.ContextAsync(q.VahedCode, ct);
        var items = await _repo.GetItemsAsync(src.Lines.Select(l => l.ItemId).ToList(), false, ct);
        var rem = await _engine.RemainingAsync(src, q.Target, null, ctx.Setting.BillBeforeDelivery, items, ct);
        if (rem.Count == 0) throw new BusinessRuleException("مقدار باقی‌مانده‌ای برای تبدیل نیست.");
        var lines = src.Lines.OrderBy(l => l.Radif).Where(l => rem.ContainsKey(l.ItemId))
            .Select(l => new SalesDocLineInput(l.ItemId, rem[l.ItemId], l.Price, l.DiscountPercent)).ToList();
        return new SalesDocInput(q.Target, SalesDates.Today(), src.CustomerId, src.Id, src.PayTerms, null, src.DeliveryDate,
            src.SalesType, src.RefNo, null, null, 0, lines);
    }
}

internal static class SalesHistory
{
    public static async Task<SalesDoc> LoadAsync(ISalesRepository repo, Guid id, string vahedCode, CancellationToken ct)
    {
        var doc = await repo.GetDocAsync(id, true, ct);
        if (doc is null || doc.VahedCode != vahedCode || doc.IsDeleted) throw new NotFoundException("SalesDoc", id);
        return doc;
    }

    public static Task AddAsync(ISalesRepository repo, SalesDoc doc, string action, ICurrentUser user, CancellationToken ct) =>
        repo.AddHistoryAsync(new SalesDocHistory { Id = Guid.NewGuid(), DocId = doc.Id, Action = action.Length > 500 ? action[..500] : action, CreatedBy = user.UserId, CreatedAtUtc = DateTime.UtcNow }, ct);

    public static async Task<string> NextNoAsync(ISalesRepository repo, SalesDoc doc, CancellationToken ct)
    {
        var seq = await repo.MaxDocSequenceAsync(doc.VahedCode, doc.Year, doc.DocType, ct) + 1;
        return $"{SalesLabels.Prefix(doc.DocType)}-{doc.Year}-{seq:0000}";
    }

    /// <summary>شمارهٔ تکراری (دو ثبت هم‌زمان) ⇒ پیام قابل فهم به‌جای خطای پایگاه داده.</summary>
    public static async Task SaveAsync(IUnitOfWork uow, CancellationToken ct)
    {
        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            throw new BusinessRuleException("هم‌زمان سند دیگری همین شماره را گرفت؛ دوباره تلاش کنید.");
        }
    }
}
