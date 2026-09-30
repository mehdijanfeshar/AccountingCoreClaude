using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.GetFsTemplates;

/// <summary><c>GET api/fs/templates?framework=</c> — قالب‌ها با نسخه‌هایشان؛ بدون فیلتر = همهٔ مجموعه‌ها.</summary>
public sealed record GetFsTemplatesQuery(FsFramework? Framework) : IRequest<IReadOnlyList<FsTemplateDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
