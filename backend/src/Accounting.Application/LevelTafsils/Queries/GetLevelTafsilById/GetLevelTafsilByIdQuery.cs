using MediatR;

namespace Accounting.Application.LevelTafsils.Queries.GetLevelTafsilById;

/// <summary>
/// Returns a single <c>TB_LEVEL_TAFSIL</c> row projected to <see cref="LevelTafsilDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="LevelTafsilDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetLevelTafsilByIdQuery(Guid Id) : IRequest<LevelTafsilDto?>;
