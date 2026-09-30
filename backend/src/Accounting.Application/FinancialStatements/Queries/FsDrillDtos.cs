using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Queries;

/// <summary>
/// ردیف هدف Drill-down و مشخصات اجرای آن (بخش ۴۵-د) — فقط برای اجرای واحد هدر پیدا می‌شود.
/// </summary>
public sealed record FsDrillTarget(
    Guid RunId,
    string VahedCode,
    bool IncludeSubUnits,
    string Year,
    string FromDate,
    string ToDate,
    int MinDocLife,
    bool HasPrior,
    Guid RowId,
    string RowCode,
    FsRowType RowType,
    FsValueType? ValueType,
    string? TitleFa);

/// <summary>سطح «حساب»: سهم یک معین در ردیف (جمع همهٔ زیرواحدها)، از Snapshot.</summary>
public sealed record FsDrillAccountDto(string AccCode, string? AccName, decimal? AmountCur, decimal? AmountPrv);

/// <summary>سطح «واحد»: سهم یک زیرواحد سطح اول (یا خود واحد اجرا) در ردیف یا در یک معین، از Snapshot.</summary>
public sealed record FsDrillUnitDto(string VahedCode, string? VahedName, decimal? AmountCur, decimal? AmountPrv);

/// <summary>سطح «سند»: یک ردیف سند که در مبلغ معین سهم دارد — زنده از اسناد (نه Snapshot).</summary>
public sealed record FsDrillVoucherLineDto(
    Guid VoucherHeadId,
    string? DocNum,
    string? DateDoc,
    string? VahedCode,
    string? HeadDesc,
    string? LineDesc,
    decimal Debtor,
    decimal Creditor,
    bool IsOpening);

/// <summary>
/// صفحه‌ای از ردیف‌های سند. <paramref name="SumDebtor"/>/<paramref name="SumCreditor"/> روی همهٔ صفحه‌هاست؛
/// اگر اسناد پس از اجرا تغییر کرده باشند، (بدهکار − بستانکار) با مبلغ Snapshot فرق می‌کند.
/// </summary>
public sealed record FsDrillVoucherPageDto(
    IReadOnlyList<FsDrillVoucherLineDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    decimal SumDebtor,
    decimal SumCreditor);

/// <summary>کدام بخش ردیف‌های سند برای یک <see cref="FsValueType"/> حساب می‌شود.</summary>
public enum FsDrillWindow
{
    /// <summary>مانده پایان: افتتاحیه + دوره.</summary>
    All = 0,
    /// <summary>مانده ابتدا: فقط افتتاحیه و پیش از دوره.</summary>
    Opening = 1,
    /// <summary>گردش دوره: بدون افتتاحیه.</summary>
    Period = 2,
}

/// <summary>خروجی فایل (Excel).</summary>
public sealed record FsFileDto(string FileName, string ContentType, byte[] Content);
