using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherDetailById;

/// <summary>
/// Returns a single <c>TB_VOUCHERSDETAIL</c> row projected to <see cref="VoucherDetailDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="VoucherDetailDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetVoucherDetailByIdQuery(Guid Id) : IRequest<VoucherDetailDto?>;
