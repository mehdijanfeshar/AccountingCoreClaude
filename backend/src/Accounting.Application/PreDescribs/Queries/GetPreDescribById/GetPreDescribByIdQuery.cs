using MediatR;

namespace Accounting.Application.PreDescribs.Queries.GetPreDescribById;

/// <summary>
/// Returns a single <c>TB_PREDESCRIB</c> row projected to <see cref="PreDescribDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetPreDescribByIdQuery(Guid Id) : IRequest<PreDescribDto?>;
