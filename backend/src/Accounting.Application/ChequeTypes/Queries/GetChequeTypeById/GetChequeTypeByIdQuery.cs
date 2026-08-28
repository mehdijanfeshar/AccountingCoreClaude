using MediatR;

namespace Accounting.Application.ChequeTypes.Queries.GetChequeTypeById;

/// <summary>
/// Returns a single <c>TB_CHECK_TYPE</c> row projected to <see cref="ChequeTypeDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="ChequeTypeDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetChequeTypeByIdQuery(Guid Id) : IRequest<ChequeTypeDto?>;
