using MediatR;

namespace Accounting.Application.TafsilGroups.Queries.GetTafsilGroupById;

/// <summary>
/// Returns a single <c>TB_TAFSIL_GROUP</c> row projected to <see cref="TafsilGroupDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="TafsilGroupDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetTafsilGroupByIdQuery(Guid Id) : IRequest<TafsilGroupDto?>;
