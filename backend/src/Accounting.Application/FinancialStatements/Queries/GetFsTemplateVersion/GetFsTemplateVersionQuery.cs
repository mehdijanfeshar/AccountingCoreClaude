using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.GetFsTemplateVersion;

/// <summary><c>GET api/fs/template-versions/{id}</c> — نسخه با همهٔ ردیف‌ها؛ <see langword="null"/> = ۴۰۴.</summary>
public sealed record GetFsTemplateVersionQuery(Guid Id) : IRequest<FsTemplateVersionDetailDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
