using MediatR;

namespace Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeadById;

/// <summary>
/// Returns a single <c>TB_PAYRECIVHEAD</c> row projected to <see cref="PayReciveHeadDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="PayReciveHeadDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetPayReciveHeadByIdQuery(Guid Id) : IRequest<PayReciveHeadDto?>;
