using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsTemplateRow;

/// <summary>
/// <c>POST api/fs/template-versions/{versionId}/rows/{rowId}/delete</c> — حذف <b>سخت</b> ردیف
/// نسخهٔ پیش‌نویس (فرزند تعبیه‌شده، نه سابقه). فرزندان ردیف حذف‌شده بی‌والد می‌شوند، حذف نمی‌شوند.
/// </summary>
public sealed record DeleteFsTemplateRowCommand(Guid VersionId, Guid RowId) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
