using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.BankCartDetails.Queries.GetBankCartDetails;

/// <summary>
/// Returns a page of <c>TB_BANKCARTDETAIL</c> rows projected to <see cref="BankCartDetailDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetBankCartDetailsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetBankCartDetailsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<BankCartDetailDto>>;
