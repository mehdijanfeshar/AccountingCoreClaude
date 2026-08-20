using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherDetails;

/// <summary>
/// Returns a page of <c>TB_VOUCHERSDETAIL</c> rows projected to <see cref="VoucherDetailDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included. <see cref="VoucherHeadId"/>,
/// <see cref="Year"/> and <see cref="VahedCode"/> are optional filters — when supplied, only
/// matching rows are returned. <see cref="VoucherHeadId"/> is the primary way callers discover
/// the detail lines created together with a head via
/// <see cref="Accounting.Application.Vouchers.Commands.CreateVoucherHead.CreateVoucherHeadCommand.InitialDetails"/>
/// (that command returns only the head's <c>Guid</c>).
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetVoucherDetailsQueryValidator.MaxPageSize"/>.</param>
/// <param name="VoucherHeadId">Optional exact-match filter on the VOUCHERSHEAD_ID column.</param>
/// <param name="Year">Optional exact-match filter on the YEAR column.</param>
/// <param name="VahedCode">Optional exact-match filter on the VAHEDCODE column.</param>
public sealed record GetVoucherDetailsQuery(
    int PageNumber,
    int PageSize,
    Guid? VoucherHeadId,
    string? Year,
    string? VahedCode) : IRequest<PagedResult<VoucherDetailDto>>;
