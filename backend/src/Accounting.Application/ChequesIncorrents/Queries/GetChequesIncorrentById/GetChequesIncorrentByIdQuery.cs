using MediatR;

namespace Accounting.Application.ChequesIncorrents.Queries.GetChequesIncorrentById;

/// <summary>
/// Returns a single <c>TB_CHEQUES_INCORRENT</c> row projected to
/// <see cref="ChequesIncorrentDto"/>, or <see langword="null"/> if no row with the given
/// <see cref="Id"/> exists. Unlike the list query, no logical-delete filter is applied — the row
/// is returned regardless of <c>ISDELETED</c>, and <see cref="ChequesIncorrentDto.IsDeleted"/>
/// lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetChequesIncorrentByIdQuery(Guid Id) : IRequest<ChequesIncorrentDto?>;
