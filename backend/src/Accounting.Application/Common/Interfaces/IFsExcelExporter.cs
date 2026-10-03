using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// خروجی Excel یک اجرای صورت‌های مالی (بخش ۴۵-د، سند منبع §۱۶): هر صورت/یادداشت یک برگه، ردیف‌های فرمول
/// به‌صورت فرمول Excel تا حسابرس بتواند جمع‌ها را ردیابی کند. پیاده‌سازی در Infrastructure (ClosedXML).
/// </summary>
public interface IFsExcelExporter
{
    byte[] Export(FsRunDetailDto run);

    /// <summary>
    /// خروجی یک ردیف Drill-down (بخش ۴۵-د، سند منبع §۱۲-۳ «هر سطح قابل خروجی Excel است»): برگه‌های
    /// «معین‌ها» و «واحدها» (مبلغ نمایشی با ماهیت ردیف)، و اگر <paramref name="vouchers"/> داده شود برگهٔ «اسناد»
    /// (بدهکار/بستانکار خام).
    /// </summary>
    byte[] ExportDrill(
        FsDrillTarget target,
        FsNormalBalance? normalBalance,
        IReadOnlyList<FsDrillAccountDto> accounts,
        IReadOnlyList<FsDrillUnitDto> units,
        string? accCode,
        FsDrillVoucherPageDto? vouchers);
}
