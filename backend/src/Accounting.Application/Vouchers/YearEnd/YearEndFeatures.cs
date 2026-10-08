using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Vouchers.YearEnd;

// سند افتتاحیه و اختتامیه — بازسازی AddOpeningVoucherCommand / AddClosingVoucherCommand پروژهٔ مرجع
// (D:\CentralAccount، مرجع §۱۷-۱ و §۱۷-۲) با تصمیم‌های صاحب پروژه ۲۰۲۶-۱۰-۰۸:
//
// اختتامیه (سال Y): ماندهٔ معین‌های گروه ۶، ۷، ۸ (به تفکیک تفصیلی‌ها) معکوس می‌شود و برای هر حساب رابط
//   اختتامیه (TB_RABET_CLOSING همان سال و همان نوع واحد) یک ردیف رابط سند را تراز می‌کند. یک سند،
//   FLAG_STATE = 1 (موتور صورت‌های مالی سند اختتامیه را با همین علامت کنار می‌گذارد).
// افتتاحیه (سال Y+1): ماندهٔ معین‌های گروه ۱، ۲، ۳، ۴، ۵، ۹ سال Y (به تفکیک تفصیلی‌ها) منتقل می‌شود؛
//   مثل سیستم قدیم یک سند برای هر گروه، ولی هر سند با ردیفی روی حساب رابط افتتاحیه
//   (TB_ACCOUNTCODE_INTERFACE، نوع ۱) تراز می‌شود. سند اختتامیه در این مانده حساب می‌شود، چون نتیجهٔ
//   سود و زیان روی حساب رابط اختتامیه نشسته و باید به سال بعد برود.
// هر دو: حساب‌های مستثنا (TB_ACCOUNTEXCEPTION همان نوع واحد) کنار می‌روند؛ سند «یادداشت» و سیستمی
//   (ISAUTOMATIC) صادر می‌شود؛ صدور تکراری ممنوع است (سند قبلی را حذف کنید و دوباره صادر کنید).
//
// تفاوت عمدی با مرجع (ایرادهای مرجع): (۱) مرجع ردیف‌های هر گروه را به سند گروه‌های بعدی هم اضافه می‌کرد؛
// (۲) سندهای افتتاحیهٔ مرجع تراز نبودند؛ (۳) اختتامیهٔ مرجع ماندهٔ جدول‌های موقت (tmp) را می‌خواند و
// تفصیلی‌های اولین حساب را روی همهٔ ردیف‌ها می‌گذاشت.

public static class YearEndKinds
{
    public const string Opening = "opening";
    public const string Closing = "closing";

    /// <summary>گروه‌هایی که افتتاحیه منتقل می‌کند (مرجع: «123459»).</summary>
    public static readonly IReadOnlyList<char> OpeningGroups = ['1', '2', '3', '4', '5', '9'];

    /// <summary>گروه‌هایی که اختتامیه می‌بندد (View مرجع: «6، 7، 8»).</summary>
    public static readonly IReadOnlyList<char> ClosingGroups = ['6', '7', '8'];

    public const decimal ClosingFlag = 1;
}

public sealed record YearEndTafsili(Guid LevelId, string? LevelCode, Guid TafsiliId, string? Code, string? Name);

/// <summary>ماندهٔ خالص یک ترکیب «معین + تفصیلی‌ها» (Net = بدهکار − بستانکار).</summary>
public sealed record YearEndBalance(Guid AccountId, string AccCode, string? AccName, IReadOnlyList<YearEndTafsili> Tafsilis, decimal Net);

public sealed record YearEndAccountRef(Guid Id, string AccCode, string? AccName);

public sealed record YearEndLine(
    Guid AccountId, string AccCode, string? AccName, IReadOnlyList<YearEndTafsili> Tafsilis,
    decimal Debtor, decimal Creditor, string Description, bool IsBalancing);

public sealed record YearEndVoucherPlan(string HeadDesc, string? Group, IReadOnlyList<YearEndLine> Lines)
{
    public decimal TotalDebtor => Lines.Sum(l => l.Debtor);
    public decimal TotalCreditor => Lines.Sum(l => l.Creditor);
}

public sealed record YearEndLineDto(
    string AccCode, string? AccName, string? Tafsilis, decimal Debtor, decimal Creditor, bool IsBalancing);

public sealed record YearEndVoucherDto(
    string HeadDesc, string? Group, IReadOnlyList<YearEndLineDto> Lines, decimal TotalDebtor, decimal TotalCreditor);

/// <summary>
/// پیش‌نمایش پیش از صدور. <c>Problems</c> خالی نباشد ⇒ صدور ممکن نیست؛ <c>Warnings</c> فقط اطلاع است.
/// </summary>
public sealed record YearEndPreviewDto(
    string Kind, string SourceYear, string TargetYear, string DateDoc,
    IReadOnlyList<YearEndVoucherDto> Vouchers, IReadOnlyList<string> Problems, IReadOnlyList<string> Warnings);

public sealed record YearEndIssueResultDto(IReadOnlyList<string> DocNums);

public interface IYearEndRepository
{
    Task<Guid?> GetVahedTypeIdAsync(string vahedCode, CancellationToken ct);

    /// <summary>
    /// ماندهٔ خالص هر ترکیب معین + تفصیلی‌ها در سال و واحد، فقط معین‌هایی که رقم اول کدشان در
    /// <paramref name="groups"/> است. ردیف و سند حذف‌شده حساب نمی‌شوند؛ سند اختتامیه (FLAG_STATE = 1) فقط
    /// وقتی <paramref name="includeClosing"/> باشد.
    /// </summary>
    Task<IReadOnlyList<YearEndBalance>> GetBalancesAsync(
        string vahedCode, string year, IReadOnlyCollection<char> groups, bool includeClosing, CancellationToken ct);

    Task<IReadOnlySet<Guid>> GetExceptionAccountIdsAsync(Guid vahedTypeId, CancellationToken ct);

    Task<YearEndAccountRef?> GetInterfaceAccountAsync(InterfaceType type, CancellationToken ct);

    /// <summary>معین ⇒ حساب رابط اختتامیه (رابطِ تعریف‌شده روی یک کل به همهٔ معین‌های زیرش می‌رسد؛ مثل VW_RABETCLOSING_ACCOUNTS).</summary>
    Task<IReadOnlyDictionary<Guid, YearEndAccountRef>> GetClosingRabetMapAsync(Guid vahedTypeId, string year, CancellationToken ct);

    /// <summary>شمارهٔ سندهای زندهٔ سال که ردیفی روی <paramref name="accountId"/> دارند.</summary>
    Task<IReadOnlyList<string>> GetDocNumsWithAccountAsync(string vahedCode, string year, Guid accountId, CancellationToken ct);

    Task<IReadOnlyList<string>> GetClosingDocNumsAsync(string vahedCode, string year, CancellationToken ct);

    Task<int> CountDraftVouchersAsync(string vahedCode, string year, CancellationToken ct);

    Task StageAsync(TB_VOUCHERSHEAD head, IReadOnlyList<TB_VOUCHERSDETAIL> details, IReadOnlyList<TB_VOUCHERDETAIL_LINK_TAFSILI> links, CancellationToken ct);
}

/// <summary>منطق خالص ساخت سند (بدون دیتابیس) — جدا تا مستقیم تست شود.</summary>
public static class YearEndPlanner
{
    public static IReadOnlyList<YearEndVoucherPlan> PlanOpening(
        IEnumerable<YearEndBalance> balances, IReadOnlySet<Guid> excluded, YearEndAccountRef openingInterface, string sourceYear)
    {
        var plans = new List<YearEndVoucherPlan>();
        foreach (var group in balances
                     .Where(b => b.Net != 0 && !excluded.Contains(b.AccountId))
                     .GroupBy(b => b.AccCode[0])
                     .OrderBy(g => g.Key))
        {
            var lines = Ordered(group)
                .Select(b => new YearEndLine(b.AccountId, b.AccCode, b.AccName, b.Tafsilis,
                    b.Net > 0 ? b.Net : 0, b.Net < 0 ? -b.Net : 0, $"انتقال ماندهٔ سال {sourceYear}", false))
                .ToList();

            var net = group.Sum(b => b.Net);
            if (net != 0)
                lines.Add(new YearEndLine(openingInterface.Id, openingInterface.AccCode, openingInterface.AccName, [],
                    net < 0 ? -net : 0, net > 0 ? net : 0, $"رابط افتتاحیهٔ گروه {group.Key}", true));

            plans.Add(new YearEndVoucherPlan($"سند افتتاحیه - گروه {group.Key}", group.Key.ToString(), lines));
        }
        return plans;
    }

    /// <returns>طرح سند (null اگر چیزی برای بستن نیست) و معین‌هایی که رابط اختتامیه ندارند.</returns>
    public static (YearEndVoucherPlan? Plan, IReadOnlyList<YearEndBalance> Unmapped) PlanClosing(
        IEnumerable<YearEndBalance> balances, IReadOnlySet<Guid> excluded,
        IReadOnlyDictionary<Guid, YearEndAccountRef> rabetByAccount, string year)
    {
        var open = balances.Where(b => b.Net != 0 && !excluded.Contains(b.AccountId)).ToList();
        var unmapped = open.Where(b => !rabetByAccount.ContainsKey(b.AccountId)).ToList();
        if (open.Count == 0)
            return (null, unmapped);

        var lines = new List<YearEndLine>();
        foreach (var byRabet in open
                     .Where(b => rabetByAccount.ContainsKey(b.AccountId))
                     .GroupBy(b => rabetByAccount[b.AccountId].Id)
                     .OrderBy(g => rabetByAccount[g.First().AccountId].AccCode, StringComparer.Ordinal))
        {
            foreach (var b in Ordered(byRabet))
                lines.Add(new YearEndLine(b.AccountId, b.AccCode, b.AccName, b.Tafsilis,
                    b.Net < 0 ? -b.Net : 0, b.Net > 0 ? b.Net : 0, $"بستن حساب {b.AccName ?? b.AccCode}", false));

            var rabet = rabetByAccount[byRabet.First().AccountId];
            var net = byRabet.Sum(b => b.Net);
            if (net != 0)
                lines.Add(new YearEndLine(rabet.Id, rabet.AccCode, rabet.AccName, [],
                    net > 0 ? net : 0, net < 0 ? -net : 0, $"رابط اختتامیه {rabet.AccName ?? rabet.AccCode}", true));
        }

        return (lines.Count == 0 ? null : new YearEndVoucherPlan($"سند اختتامیه سال {year}", null, lines), unmapped);
    }

    private static IEnumerable<YearEndBalance> Ordered(IEnumerable<YearEndBalance> rows)
        => rows.OrderBy(b => b.AccCode, StringComparer.Ordinal).ThenBy(b => TafsiliLabel(b.Tafsilis), StringComparer.Ordinal);

    public static string? TafsiliLabel(IReadOnlyList<YearEndTafsili> tafsilis)
        => tafsilis.Count == 0
            ? null
            : string.Join("، ", tafsilis.OrderBy(t => t.LevelCode, StringComparer.Ordinal)
                .Select(t => string.IsNullOrWhiteSpace(t.Name) ? t.Code : $"{t.Code} {t.Name}"));

    public static YearEndVoucherDto ToDto(YearEndVoucherPlan p) => new(
        p.HeadDesc, p.Group,
        p.Lines.Select(l => new YearEndLineDto(l.AccCode, l.AccName, TafsiliLabel(l.Tafsilis), l.Debtor, l.Creditor, l.IsBalancing)).ToList(),
        p.TotalDebtor, p.TotalCreditor);
}

/// <summary>هر آنچه پیش‌نمایش و صدور مشترک دارند: یک بار محاسبه، تا صدور دقیقاً همان پیش‌نمایش باشد.</summary>
internal sealed record YearEndComputation(
    string Kind, string SourceYear, string TargetYear, string DateDoc,
    IReadOnlyList<YearEndVoucherPlan> Plans, List<string> Problems, List<string> Warnings)
{
    public YearEndPreviewDto ToDto() => new(Kind, SourceYear, TargetYear, DateDoc,
        Plans.Select(YearEndPlanner.ToDto).ToList(), Problems, Warnings);
}

internal static class YearEndComputer
{
    public static async Task<YearEndComputation> ComputeAsync(IYearEndRepository repo, string kind, string year, string vahedCode, CancellationToken ct)
    {
        var problems = new List<string>();
        var warnings = new List<string>();
        var vahedType = await repo.GetVahedTypeIdAsync(vahedCode, ct)
            ?? throw new BusinessRuleException($"نوع واحد «{vahedCode}» پیدا نشد.");
        var excluded = await repo.GetExceptionAccountIdsAsync(vahedType, ct);

        if (kind == YearEndKinds.Opening)
        {
            var sourceYear = (int.Parse(year) - 1).ToString("0000");
            var dateDoc = year + "0101";
            var plans = new List<YearEndVoucherPlan>();
            var iface = await repo.GetInterfaceAccountAsync(InterfaceType.OpenVoucher, ct);
            if (iface is null)
            {
                problems.Add("حساب رابط افتتاحیه تعریف نشده است (اطلاعات پایه، انتساب رابط، نوع «افتتاحیه»).");
            }
            else
            {
                var existing = await repo.GetDocNumsWithAccountAsync(vahedCode, year, iface.Id, ct);
                if (existing.Count > 0)
                    problems.Add($"سند افتتاحیهٔ سال {year} قبلاً صادر شده است (سند {string.Join("، ", existing)}). برای صدور دوباره، اول آن را حذف کنید.");
                var balances = await repo.GetBalancesAsync(vahedCode, sourceYear, YearEndKinds.OpeningGroups, includeClosing: true, ct);
                plans.AddRange(YearEndPlanner.PlanOpening(balances, excluded, iface, sourceYear));
                if (plans.Count == 0)
                    problems.Add($"در سال {sourceYear} ماندهٔ قابل انتقالی برای گروه‌های ۱، ۲، ۳، ۴، ۵ و ۹ نیست.");
            }

            if ((await repo.GetClosingDocNumsAsync(vahedCode, sourceYear, ct)).Count == 0)
                warnings.Add($"سند اختتامیهٔ سال {sourceYear} هنوز صادر نشده است؛ نتیجهٔ سود و زیان آن سال در این افتتاحیه نیست.");
            await AddDraftWarningAsync(repo, vahedCode, sourceYear, warnings, ct);
            return new YearEndComputation(kind, sourceYear, year, dateDoc, plans, problems, warnings);
        }
        else
        {
            var dateDoc = year + "1229";
            var existing = await repo.GetClosingDocNumsAsync(vahedCode, year, ct);
            if (existing.Count > 0)
                problems.Add($"سند اختتامیهٔ سال {year} قبلاً صادر شده است (سند {string.Join("، ", existing)}). برای صدور دوباره، اول آن را حذف کنید.");

            var balances = await repo.GetBalancesAsync(vahedCode, year, YearEndKinds.ClosingGroups, includeClosing: false, ct);
            var rabets = await repo.GetClosingRabetMapAsync(vahedType, year, ct);
            var (plan, unmapped) = YearEndPlanner.PlanClosing(balances, excluded, rabets, year);
            foreach (var account in unmapped.GroupBy(u => u.AccCode).Take(20))
                problems.Add($"برای معین {account.Key} ({account.First().AccName}) در سال {year} حساب رابط اختتامیه تعریف نشده است.");
            if (plan is null && unmapped.Count == 0)
                problems.Add($"در سال {year} ماندهٔ بازی برای گروه‌های ۶، ۷ و ۸ نیست.");

            await AddDraftWarningAsync(repo, vahedCode, year, warnings, ct);
            return new YearEndComputation(kind, year, year, dateDoc, plan is null ? [] : [plan], problems, warnings);
        }
    }

    private static async Task AddDraftWarningAsync(IYearEndRepository repo, string vahedCode, string year, List<string> warnings, CancellationToken ct)
    {
        var drafts = await repo.CountDraftVouchersAsync(vahedCode, year, ct);
        if (drafts > 0)
            warnings.Add($"{drafts} سند «یادداشت» در سال {year} هست و در مانده‌ها حساب شده است.");
    }
}

public sealed record GetYearEndPreviewQuery(string Kind, string Year) : IRequest<YearEndPreviewDto>, IVahedScopedQuery
{
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record IssueYearEndVouchersCommand(string Kind, string Year) : IRequest<YearEndIssueResultDto>, IVahedScopedCommand
{
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetYearEndPreviewValidator : AbstractValidator<GetYearEndPreviewQuery>
{
    public GetYearEndPreviewValidator()
    {
        RuleFor(x => x.Kind).Must(k => k is YearEndKinds.Opening or YearEndKinds.Closing).WithMessage("نوع سند باید افتتاحیه یا اختتامیه باشد.");
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
    }
}

public sealed class IssueYearEndVouchersValidator : AbstractValidator<IssueYearEndVouchersCommand>
{
    public IssueYearEndVouchersValidator()
    {
        RuleFor(x => x.Kind).Must(k => k is YearEndKinds.Opening or YearEndKinds.Closing).WithMessage("نوع سند باید افتتاحیه یا اختتامیه باشد.");
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
    }
}

public sealed class GetYearEndPreviewHandler(IYearEndRepository repo) : IRequestHandler<GetYearEndPreviewQuery, YearEndPreviewDto>
{
    public async Task<YearEndPreviewDto> Handle(GetYearEndPreviewQuery q, CancellationToken ct)
        => (await YearEndComputer.ComputeAsync(repo, q.Kind, q.Year, q.VahedCode, ct)).ToDto();
}

/// <summary>
/// همان محاسبهٔ پیش‌نمایش، سپس stage و یک SaveChanges (همه یا هیچ). سند سیستمی است: ماتریس دسترسی
/// کدینگ و «تفصیلی الزامی» فرم سند اعمال نمی‌شود، چون حساب رابط عمداً «فقط سیستمی» است و تفصیلی‌ها از
/// خود اسناد سال می‌آیند.
/// </summary>
public sealed class IssueYearEndVouchersHandler(
    IYearEndRepository repo, IVoucherHeadRepository heads, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : IRequestHandler<IssueYearEndVouchersCommand, YearEndIssueResultDto>
{
    public async Task<YearEndIssueResultDto> Handle(IssueYearEndVouchersCommand c, CancellationToken ct)
    {
        var computed = await YearEndComputer.ComputeAsync(repo, c.Kind, c.Year, c.VahedCode, ct);
        if (computed.Problems.Count > 0)
            throw new BusinessRuleException(string.Join(" ", computed.Problems));

        var userId = currentUser.UserId;
        var now = DateTime.UtcNow;
        var docNum = await heads.GetNextDocNumAsync(c.VahedCode, computed.TargetYear, ct);
        var docNums = new List<string>();

        foreach (var plan in computed.Plans)
        {
            if (plan.TotalDebtor != plan.TotalCreditor)
                throw new InvalidOperationException($"{plan.HeadDesc} تراز نیست.");

            var head = new TB_VOUCHERSHEAD
            {
                ID = Guid.NewGuid(),
                DOC_NUM = docNum,
                DATE_DOC = computed.DateDoc,
                DOCLIFE = DocLife.Draft,
                HEAD_DESC = plan.HeadDesc,
                FLAG_STATE = c.Kind == YearEndKinds.Closing ? YearEndKinds.ClosingFlag : null,
                VAHEDCODE = c.VahedCode,
                YEAR = computed.TargetYear,
                ISAUTOMATIC = true,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            var details = new List<TB_VOUCHERSDETAIL>();
            var links = new List<TB_VOUCHERDETAIL_LINK_TAFSILI>();
            var radif = 1;
            foreach (var line in plan.Lines)
            {
                var detail = new TB_VOUCHERSDETAIL
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSHEAD_ID = head.ID,
                    ACCOUNT_ID = line.AccountId,
                    DESCRIPTION = line.Description.Length <= 200 ? line.Description : line.Description[..200],
                    RADIF = radif++,
                    DEBTOR = line.Debtor,
                    CREDITOR = line.Creditor,
                    VAHEDCODE = c.VahedCode,
                    YEAR = computed.TargetYear,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                };
                details.Add(detail);
                links.AddRange(line.Tafsilis.Select(t => new TB_VOUCHERDETAIL_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSDETAIL_ID = detail.ID,
                    TAFSILI_ID = t.TafsiliId,
                    LEVEL_ID = t.LevelId,
                    VAHEDCODE = c.VahedCode,
                    YEAR = computed.TargetYear,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                }));
            }

            await repo.StageAsync(head, details, links, ct);
            docNums.Add(docNum);
            // Staged vouchers are invisible to GetNextDocNumAsync until saved, so later vouchers of the
            // same batch take the next numbers here; the width of the first number is kept.
            docNum = (long.Parse(docNum) + 1).ToString().PadLeft(docNum.Length, '0');
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new YearEndIssueResultDto(docNums);
    }
}
