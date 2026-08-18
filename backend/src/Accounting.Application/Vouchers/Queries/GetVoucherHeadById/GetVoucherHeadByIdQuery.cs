using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherHeadById;

/// <summary>
/// Returns a single <c>TB_VOUCHERSHEAD</c> row projected to <see cref="VoucherHeadDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="VoucherHeadDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetVoucherHeadByIdQuery(Guid Id) : IRequest<VoucherHeadDto?>;
