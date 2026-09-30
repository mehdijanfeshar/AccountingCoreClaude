using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Treasury.Queries;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBankStatementBookCandidates;

/// <summary><c>GET api/treasury/statements/{id}/book-candidates?lineId=</c> — candidate unmatched
/// دفتری lines for the manual-match picker, ±3 days around <c>lineId</c>'s date, same direction
/// rule as auto-match.</summary>
public sealed record GetBankStatementBookCandidatesQuery(Guid StatementId, Guid LineId)
    : IRequest<IReadOnlyList<BankStatementBookLineDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
