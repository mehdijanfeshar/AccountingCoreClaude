using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PersonActions.Queries.GetPersonActions;

/// <summary>
/// Returns a page of <c>TB_PERSON_ACTION</c> rows projected to <see cref="PersonActionDto"/>.
/// Only non-deleted rows are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetPersonActionsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetPersonActionsQuery(int PageNumber, int PageSize)
    : IRequest<PagedResult<PersonActionDto>>, IVahedScopedQuery
{
    /// <summary>
    /// Caller's own unit, server-assigned by <c>VahedScopeBehavior</c>. Until 2026-09-21 this
    /// list was unscoped, so any authenticated user could enumerate every unit's rows - including
    /// <c>USERID</c>, which is a <b>national identification number</b>, not a username.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
