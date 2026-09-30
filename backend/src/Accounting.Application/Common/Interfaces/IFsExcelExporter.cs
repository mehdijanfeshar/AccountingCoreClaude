using Accounting.Application.FinancialStatements.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// خروجی Excel یک اجرای صورت‌های مالی (بخش ۴۵-د، سند منبع §۱۶): هر صورت/یادداشت یک برگه، ردیف‌های فرمول
/// به‌صورت فرمول Excel تا حسابرس بتواند جمع‌ها را ردیابی کند. پیاده‌سازی در Infrastructure (ClosedXML).
/// </summary>
public interface IFsExcelExporter
{
    byte[] Export(FsRunDetailDto run);
}
