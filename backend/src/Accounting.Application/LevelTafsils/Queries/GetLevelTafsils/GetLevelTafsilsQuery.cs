using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.LevelTafsils.Queries.GetLevelTafsils;

/// <summary>
/// Returns a page of <c>TB_LEVEL_TAFSIL</c> rows projected to <see cref="LevelTafsilDto"/>. Only
/// non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetLevelTafsilsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetLevelTafsilsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<LevelTafsilDto>>;
