using Accounting.Domain.ValueObjects;
using Accounting.Application.FinancialStatements.Access;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsTemplate;

/// <summary>
/// <c>POST api/fs/templates/{id}/delete</c> — حذف نرم قالب و همهٔ نسخه‌هایش. قالبی که نسخهٔ فعال
/// دارد حذف نمی‌شود (۴۰۹)؛ اول باید با نسخهٔ دیگری جایگزین یا کنار گذاشته شود.
/// </summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record DeleteFsTemplateCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
