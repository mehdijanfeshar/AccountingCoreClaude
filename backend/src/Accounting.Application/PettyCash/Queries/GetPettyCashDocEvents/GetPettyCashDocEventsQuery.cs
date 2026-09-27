using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashDocEvents;

/// <summary>
/// <c>GET api/petty-cash/expense-docs/{id}/events</c> — the «گردش عملیات» audit trail.
/// </summary>
public sealed record GetPettyCashDocEventsQuery(Guid ExpenseDocId) : IRequest<IReadOnlyList<PettyCashDocEventDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
