using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.ChequesIncorrents.Queries.GetChequesIncorrents;

/// <summary>
/// Returns a page of <c>TB_CHEQUES_INCORRENT</c> rows projected to
/// <see cref="ChequesIncorrentDto"/>. Only non-deleted rows (<c>ISDELETED != true</c>) are
/// included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetChequesIncorrentsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetChequesIncorrentsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<ChequesIncorrentDto>>;
