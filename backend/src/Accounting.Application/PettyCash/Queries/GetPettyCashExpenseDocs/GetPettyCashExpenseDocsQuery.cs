using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocs;

/// <summary>
/// <c>GET api/petty-cash/expense-docs?state=&amp;states=&amp;fundId=&amp;search=&amp;pageNumber=&amp;pageSize=</c>
/// — the کارتابل list, per <c>docs/tankhah-khazaneh-module.md</c> §5. Returns
/// <see cref="PettyCashExpenseDocListResult"/> (page + stateCounts), not a bare
/// <see cref="Accounting.Application.Common.PagedResult{T}"/>.
/// </summary>
/// <param name="States">
/// OR filter — matches any of the listed states. Takes precedence over <see cref="State"/> when
/// non-empty (see <see cref="PettyCashExpenseDocFilter"/>), so the «در جریان» tab (New +
/// PendingReview + Returned) can be fetched in one request.
/// </param>
public sealed record GetPettyCashExpenseDocsQuery(
    int PageNumber,
    int PageSize,
    Guid? FundId = null,
    PettyCashDocState? State = null,
    IReadOnlyList<PettyCashDocState>? States = null,
    string? Search = null) : IRequest<PettyCashExpenseDocListResult>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
