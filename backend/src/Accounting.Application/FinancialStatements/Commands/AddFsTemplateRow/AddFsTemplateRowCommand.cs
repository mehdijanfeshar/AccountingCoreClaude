using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Commands.Common;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.AddFsTemplateRow;

/// <summary>
/// <c>POST api/fs/template-versions/{versionId}/rows</c> — ردیف جدید در نسخهٔ پیش‌نویس. کد تکراری در
/// نسخه = ۴۰۹. فقط نحو انتخاب‌گر/فرمول اینجا بررسی می‌شود؛ ارجاع و دور در validate/activate.
/// پاسخ = شناسهٔ ردیف.
/// </summary>
public sealed record AddFsTemplateRowCommand(Guid VersionId, FsTemplateRowInput Row) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
