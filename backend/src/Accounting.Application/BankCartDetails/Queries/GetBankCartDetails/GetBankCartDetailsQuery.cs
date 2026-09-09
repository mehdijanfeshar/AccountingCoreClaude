using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.BankCartDetails.Queries.GetBankCartDetails;

/// <summary>
/// Returns a page of <c>TB_BANKCARTDETAIL</c> rows projected to <see cref="BankCartDetailDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included, and — via
/// <see cref="IVahedScopedQuery"/> — only rows belonging to the caller's own organizational unit
/// (<c>VAHEDCODE == VahedCode</c>, never <c>OR VAHEDCODE IS NULL</c>; see
/// <c>BankCartDetailReadRepository.GetPagedAsync</c> for why the filter is unconditional).
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetBankCartDetailsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetBankCartDetailsQuery(int PageNumber, int PageSize)
    : IRequest<PagedResult<BankCartDetailDto>>, IVahedScopedQuery
{
    /// <summary>
    /// Organizational unit code to filter by. Server-assigned by <c>VahedScopeBehavior</c> from
    /// the authenticated caller's own <c>VahedCode</c> — never bound from client input.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
