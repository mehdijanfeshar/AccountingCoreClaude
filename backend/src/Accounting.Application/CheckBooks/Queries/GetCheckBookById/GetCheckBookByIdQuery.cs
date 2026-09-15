using MediatR;

namespace Accounting.Application.CheckBooks.Queries.GetCheckBookById;

/// <summary>
/// Returns a single <c>TB_CHECKBOOK</c> row projected to <see cref="CheckBookDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="CheckBookDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetCheckBookByIdQuery(Guid Id) : IRequest<CheckBookDto?>;
