using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.ReorderFsTemplateRows;

/// <summary>
/// <c>POST api/fs/template-versions/{versionId}/rows/reorder</c> — ترتیب کامل ردیف‌های نسخهٔ پیش‌نویس
/// (برای جابه‌جایی در صفحهٔ ویرایش). <paramref name="RowIds"/> باید دقیقاً همهٔ ردیف‌های نسخه باشد؛
/// <c>ORDER_NO</c> به ۱۰، ۲۰، ۳۰، … بازنویسی می‌شود. <paramref name="ParentChanges"/> (اختیاری، طراح درختی ۴۵-و)
/// والد ردیف‌های جابه‌جاشده را در همان تراکنش عوض می‌کند — کد والد <c>null</c> = ریشه.
/// </summary>
public sealed record ReorderFsTemplateRowsCommand(
    Guid VersionId,
    IReadOnlyList<Guid> RowIds,
    IReadOnlyList<FsRowParentChange>? ParentChanges = null) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record FsRowParentChange(Guid RowId, string? ParentCode);
