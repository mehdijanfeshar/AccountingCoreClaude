namespace Accounting.Application.Reports.AttributeAccountReconciliation;

/// <summary>
/// علت مغایرت یک شناسه در گزارش مغایرت‌گیری حساب‌های شناسه‌دار.
/// </summary>
public enum AttributeMismatchReason
{
    /// <summary>جمع بدهکار و بستانکار شناسه برابر نیست (مانده ≠ ۰).</summary>
    Balance = 1,

    /// <summary>
    /// فقط شناسهٔ جمع‌ناپذیر: جمع‌ها برابرند ولی مبالغ بدهکار یک‌به‌یک با مبالغ بستانکار جفت نمی‌شوند
    /// (مثلاً بدهکار ۱۰۰ در برابر دو بستانکار ۵۰).
    /// </summary>
    UnpairedAmounts = 2,

    /// <summary>ردیف‌های سند روی حساب شناسه‌دار که شناسه ندارند.</summary>
    MissingIdentifier = 3,
}

/// <summary>سطح ۱ — یک معین شناسه‌دار با گردش و شمار شناسه‌های مغایر.</summary>
public sealed record AttributeAccountMoeinDto(
    Guid AccountId,
    string AccCode,
    string? AccName,
    int AttribSum,
    decimal Debtor,
    decimal Creditor,
    int IdentifierCount,
    int MismatchCount,
    int LinesWithoutIdentifier);

/// <summary>سطح ۲ — یک شناسه (یا «بدون شناسه» با <c>AttributeValue = null</c>) زیر یک معین.</summary>
public sealed record AttributeAccountValueDto(
    string? AttributeValue,
    decimal Debtor,
    decimal Creditor,
    int LineCount,
    bool IsMismatch,
    AttributeMismatchReason? Reason);

/// <summary>سطح ۳ — ردیف سند.</summary>
public sealed record AttributeAccountLineDto(
    Guid LineId,
    Guid VoucherHeadId,
    string? DocNum,
    string? DateDoc,
    int? DocLife,
    string? HeadDesc,
    string? LineDesc,
    decimal Debtor,
    decimal Creditor);

/// <summary>
/// ردیف خامی که مخزن برمی‌گرداند: یک ردیف سند روی حساب شناسه‌دار، با مقدار شناسه‌اش (اگر داشته باشد).
/// گروه‌بندی و تشخیص مغایرت در <see cref="AttributeReconciliationCalculator"/> انجام می‌شود.
/// </summary>
public sealed record AttributeAccountRawLine(
    Guid LineId,
    Guid AccountId,
    string AccCode,
    string? AccName,
    int AttribSum,
    string? AttributeValue,
    Guid VoucherHeadId,
    string? DocNum,
    string? DateDoc,
    int? DocLife,
    string? HeadDesc,
    string? LineDesc,
    decimal Debtor,
    decimal Creditor);

/// <summary>معیارهای مشترک سه سطح گزارش. واحد همیشه از <c>VahedScopeBehavior</c> می‌آید.</summary>
public sealed record AttributeAccountFilter(
    string VahedCode,
    string Year,
    string? FromDate,
    string? ToDate,
    int? DocLife,
    Guid? AccountId);
