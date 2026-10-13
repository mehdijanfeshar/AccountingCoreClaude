using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Queries.PettyCash;
using MediatR;

namespace Accounting.Application.Queries.PettyCash.GetPettyCashFundReviewers;

/// <summary><c>GET api/petty-cash/funds/{fundId}/reviewers</c>.</summary>
public sealed record GetPettyCashFundReviewersQuery(Guid FundId)
    : IRequest<IReadOnlyList<PettyCashFundReviewerDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
