using MediatR;

namespace Accounting.Application.RevolvingFunds.Queries.GetRevolvingFundById;

/// <summary>
/// Returns a single <c>TB_REVOLVING_FUND</c> row projected to <see cref="RevolvingFundDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="RevolvingFundDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetRevolvingFundByIdQuery(Guid Id) : IRequest<RevolvingFundDto?>;
