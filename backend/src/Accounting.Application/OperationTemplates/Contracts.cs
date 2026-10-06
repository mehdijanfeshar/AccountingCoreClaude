using Accounting.Application.Vouchers.Commands.Common;

namespace Accounting.Application.OperationTemplates;

// ─────────────────────────────────────────────────────────────
// پورت‌ها: موتور به جداول موجود پروژه مستقیم وابسته نیست.
// پیاده‌سازی خواندنی‌ها در Infrastructure/OperationTemplates روی زنجیرهٔ
// TB_ACCOUNTCODE → TB_ACCOUNT_LINK_LEVEL/TAFSILGROUP و TB_TAFSILI → TB_TAFSIL_LINK_TAFSILGROUP است.
// ─────────────────────────────────────────────────────────────

/// <summary>تعریف یک معین از دید موتور: فعال بودن و سطوح تفصیلی‌اش.</summary>
public sealed record SubsidiaryAccountInfo(
    Guid Id,
    string Code,
    string Title,
    bool IsActive,
    IReadOnlyList<DetailLevelRule> DetailLevels);

/// <summary>
/// سطح N این معین. برخلاف فرض اولیهٔ طراحی، در این اسکیما هر سطح می‌تواند به
/// <b>چند</b> گروه تفصیلی وصل باشد (<c>TB_ACCOUNT_LINK_TAFSILGROUP</c>)، پس مجموعه است.
/// <paramref name="LevelId"/> همان <c>TB_LEVEL_TAFSIL.ID</c> است که در لینک تفصیلی سند نوشته می‌شود.
/// </summary>
public sealed record DetailLevelRule(int Level, Guid LevelId, IReadOnlySet<Guid> DetailGroupIds, bool IsRequired);

// DetailGroupIds: گروه‌هایی که این تفصیلی از طریق آن‌ها برای واحد کاربر قابل استفاده است (قاعدهٔ B).
// IsVisibleToUnit: false = تفصیلی عضو گروهی هست ولی هیچ‌کدام برای واحد کاربر مجاز نیست.
public sealed record DetailInfo(
    Guid Id,
    string Code,
    string Title,
    IReadOnlySet<Guid> DetailGroupIds,
    bool IsActive,
    bool IsVisibleToUnit);

public interface ISubsidiaryAccountReader
{
    Task<IReadOnlyDictionary<Guid, SubsidiaryAccountInfo>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct);
}

public interface IDetailReader
{
    // vahedCode: واحد کاربر — فیلتر دسترسی واحد (قاعدهٔ B) با همین اعمال می‌شود.
    Task<IReadOnlyDictionary<Guid, DetailInfo>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, string vahedCode, CancellationToken ct);
}

/// <summary>
/// Adapter به مسیر ثبت سند خودکار پروژه (همان مخزن‌ها، شماره‌گذاری و گارد سطح تفصیلی
/// که خزانه‌داری/تنخواه استفاده می‌کنند). <b>فقط stage می‌کند و ذخیره نمی‌کند</b> تا
/// هندلر سند و <c>OperationExecution</c> را با یک <c>SaveChangesAsync</c> (یک تراکنش) ثبت کند.
/// </summary>
public interface IVoucherWriter
{
    /// <returns>شناسه و شماره سند ایجادشده (در وضعیت «موقت»)</returns>
    Task<(Guid VoucherId, string VoucherNo)> CreateDraftVoucherAsync(
        VoucherDraft draft, CancellationToken ct);
}

// ─────────────────────────── خروجی موتور ───────────────────────────

public sealed record VoucherDraft(
    string VahedCode,
    DateOnly VoucherDate,
    string Description,
    string SourceTemplateCode,
    IReadOnlyList<VoucherDraftLine> Lines,
    string? Apendix = null,
    Guid? SystemTypeId = null)
{
    public long TotalDebit => Lines.Sum(l => l.Debit);
    public long TotalCredit => Lines.Sum(l => l.Credit);
}

public sealed record VoucherDraftLine(
    Guid SubsidiaryAccountId,
    string SubsidiaryAccountTitle,
    IReadOnlyList<VoucherDraftDetail> Details,
    long Debit,
    long Credit,
    string Description,
    string? SubsidiaryAccountCode = null,
    // چک ردیف (فقط مسیر «سند کامل»): چک موجود یا دسته‌چک صوری — همان VoucherChequeService ردیف سند.
    Guid? CheckId = null,
    VoucherChequeInfoInput? Cheque = null,
    // شناسه/ویژگی/فیش ردیف — همان VoucherLineExtrasService فرم سند.
    VoucherLineExtrasInput? Extras = null);

public sealed record VoucherDraftDetail(int Level, Guid LevelId, Guid DetailId, string DetailTitle);

public enum EngineErrorCode
{
    TemplateNotFound,
    TemplateInactive,
    MissingParameter,
    InvalidParameterValue,
    UnknownParameter,
    DetailNotFound,
    DetailInactive,
    DetailWrongGroup,
    DetailNotInUnit,
    AccountNotFound,
    AccountInactive,
    MissingRequiredDetailLevel,
    DetailLevelNotDefined,
    NonPositiveAmount,
    Unbalanced
}

/// <summary>
/// خطای موتور. ParameterKey و AskPrompt عمداً برگردانده می‌شوند:
/// در فاز Agent، خطای MissingParameter مستقیماً تبدیل به سؤال از کاربر می‌شود.
/// </summary>
public sealed record EngineError(
    EngineErrorCode Code,
    string Message,
    string? ParameterKey = null,
    string? AskPrompt = null);

public sealed class EngineResult
{
    public VoucherDraft? Draft { get; private init; }
    public IReadOnlyList<EngineError> Errors { get; private init; } = Array.Empty<EngineError>();
    public bool IsSuccess => Draft is not null && Errors.Count == 0;

    public static EngineResult Ok(VoucherDraft d) => new() { Draft = d };
    public static EngineResult Fail(IEnumerable<EngineError> e) => new() { Errors = e.ToList() };
    public static EngineResult Fail(EngineError e) => new() { Errors = new[] { e } };
}
