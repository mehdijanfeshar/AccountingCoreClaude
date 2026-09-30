using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Commands.Common;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.UpdateFsTemplateRow;

/// <summary>
/// <c>POST api/fs/template-versions/{versionId}/rows/{rowId}/update</c> — جایگزینی کامل فیلدهای ردیف
/// (فقط پیش‌نویس). <c>OrderNo</c> خالی = ترتیب فعلی حفظ شود. ردیف «عنوان»ی که فرزند دارد نمی‌تواند
/// نوعش را عوض کند (۴۰۹).
/// </summary>
public sealed record UpdateFsTemplateRowCommand(Guid VersionId, Guid RowId, FsTemplateRowInput Row) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
