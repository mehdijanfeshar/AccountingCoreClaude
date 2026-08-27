using MediatR;

namespace Accounting.Application.Rabets.Queries.GetRabetById;

/// <summary>
/// Returns a single <c>TB_RABET</c> row projected to <see cref="RabetDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="RabetDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetRabetByIdQuery(Guid Id) : IRequest<RabetDto?>;
