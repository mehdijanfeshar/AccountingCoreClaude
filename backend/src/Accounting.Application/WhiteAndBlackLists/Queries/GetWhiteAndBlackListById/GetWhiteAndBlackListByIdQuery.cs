using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackListById;

/// <summary>
/// Returns a single <c>TB_WHITEANDBLACKLIST</c> row projected to
/// <see cref="WhiteAndBlackListDto"/>, or <see langword="null"/> if no row with the given
/// <see cref="Id"/> exists. Unlike the list query, no logical-delete filter is applied — the
/// row is returned regardless of <c>ISDELETED</c>, and
/// <see cref="WhiteAndBlackListDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetWhiteAndBlackListByIdQuery(Guid Id) : IRequest<WhiteAndBlackListDto?>;
