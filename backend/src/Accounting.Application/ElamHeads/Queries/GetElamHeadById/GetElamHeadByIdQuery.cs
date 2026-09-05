using MediatR;

namespace Accounting.Application.ElamHeads.Queries.GetElamHeadById;

/// <summary>
/// Returns a single <c>TB_ELAMHEAD</c> row projected to <see cref="ElamHeadDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="ElamHeadDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetElamHeadByIdQuery(Guid Id) : IRequest<ElamHeadDto?>;
