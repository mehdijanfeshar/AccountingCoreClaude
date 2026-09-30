using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.GetFsRun;

/// <summary><c>GET api/fs/runs/{id}</c> — اجرا با همهٔ صورت‌ها از Snapshot؛ اجرای واحد دیگر = ۴۰۴.</summary>
public sealed record GetFsRunQuery(Guid Id) : IRequest<FsRunDetailDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
