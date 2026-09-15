using MediatR;

namespace Accounting.Application.WhiteLists.Queries.GetWhiteListById;

/// <summary>
/// Returns a single <c>TB_WHITELIST</c> row projected to <see cref="WhiteListDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="WhiteListDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetWhiteListByIdQuery(Guid Id) : IRequest<WhiteListDto?>;
