using Accounting.Application.FinancialStatements;
using Accounting.Domain.ValueObjects;
using Accounting.Application.FinancialStatements.Access;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Commands.FinancialStatements.UpdateFsTemplateVersion;

/// <summary><c>POST api/fs/template-versions/{id}/update</c> — فقط توضیح نسخهٔ پیش‌نویس.</summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record UpdateFsTemplateVersionCommand(Guid Id, string? Description) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
