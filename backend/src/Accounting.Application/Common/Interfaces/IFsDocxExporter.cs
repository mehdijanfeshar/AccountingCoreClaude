using Accounting.Application.FinancialStatements.Narratives;
using Accounting.Application.FinancialStatements.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// ح-۶ — خروجی Word یادداشت‌های توضیحی یک اجرا (سند منبع §۱۶: «Word برای ویرایش نهایی متن یادداشت‌ها»): متن‌های
/// مستقل، سپس هر یادداشت عددی با متن پیوسته و جدولش. متغیرها با <see cref="FsNarrativeVariables"/> از همان اجرا.
/// </summary>
public interface IFsDocxExporter
{
    byte[] ExportNarratives(FsRunDetailDto run, IReadOnlyList<FsRunNarrativeDto> narratives, decimal divisor, string unitLabel);
}
