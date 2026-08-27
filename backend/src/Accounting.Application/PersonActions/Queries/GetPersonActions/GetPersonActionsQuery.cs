using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.PersonActions.Queries.GetPersonActions;

/// <summary>
/// Returns a page of <c>TB_PERSON_ACTION</c> rows projected to <see cref="PersonActionDto"/>.
/// Only non-deleted rows are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetPersonActionsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetPersonActionsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<PersonActionDto>>;
