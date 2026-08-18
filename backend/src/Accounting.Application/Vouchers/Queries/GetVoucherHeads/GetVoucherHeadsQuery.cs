using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherHeads;

/// <summary>
/// Returns a page of <c>TB_VOUCHERSHEAD</c> rows projected to <see cref="VoucherHeadDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included. <see cref="Year"/> and
/// <see cref="VahedCode"/> are optional filters — when supplied, only matching rows are
/// returned.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetVoucherHeadsQueryValidator.MaxPageSize"/>.</param>
/// <param name="Year">Optional exact-match filter on the YEAR column.</param>
/// <param name="VahedCode">Optional exact-match filter on the VAHEDCODE column.</param>
public sealed record GetVoucherHeadsQuery(
    int PageNumber,
    int PageSize,
    string? Year,
    string? VahedCode) : IRequest<PagedResult<VoucherHeadDto>>;
