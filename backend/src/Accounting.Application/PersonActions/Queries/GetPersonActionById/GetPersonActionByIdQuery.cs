using MediatR;

namespace Accounting.Application.PersonActions.Queries.GetPersonActionById;

/// <summary>
/// Returns a single <c>TB_PERSON_ACTION</c> row projected to <see cref="PersonActionDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. No logical-delete
/// filter is applied.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetPersonActionByIdQuery(Guid Id) : IRequest<PersonActionDto?>;
