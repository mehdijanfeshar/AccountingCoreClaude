using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.GetFsRuns;

/// <summary><c>GET api/fs/runs?year=</c> — اجراهای واحد هدر (جدیدترین اول، حداکثر ۲۰۰).</summary>
public sealed record GetFsRunsQuery(string? Year) : IRequest<IReadOnlyList<FsRunSummaryDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
