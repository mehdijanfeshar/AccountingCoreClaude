using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeadById;

/// <summary>
/// Returns a single <c>TB_TMP_VOUCHERHEAD</c> row projected to <see cref="TmpVoucherHeadDto"/>,
/// or <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="TmpVoucherHeadDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetTmpVoucherHeadByIdQuery(Guid Id) : IRequest<TmpVoucherHeadDto?>;
