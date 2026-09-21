using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PersonActions.Commands.UpdatePersonAction;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_PERSON_ACTION</c> row (PUT
/// semantics, not PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes
/// <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>.
///
/// Like Create, this can violate <c>UK_PERSON_ACTION</c> (<c>UserId</c>/<c>FromDate</c>/<c>ToDate</c>)
/// against a *different* row, surfacing as the same central ORA-00001 → 409 mapping.
/// </summary>
/// <param name="Id">The <c>TB_PERSON_ACTION.ID</c> to update (bound from the route, never the body).</param>
/// <param name="UserName">USERNAME column (max 30 chars, optional).</param>
/// <param name="UserId">USERID column (max 10 chars, required).</param>
/// <param name="FromDate">FROMDATE column (max 8 chars).</param>
/// <param name="ToDate">TODATE column (max 8 chars).</param>
/// <param name="Status">STATUS column (nullable <c>bool</c>, genuinely boolean — left untouched).</param>
/// <param name="OperatorRole">
/// OPERATORROLE column (non-nullable <see cref="Accounting.Domain.ValueObjects.OperatorRole"/>). See
/// <see cref="Accounting.Application.PersonActions.Commands.CreatePersonAction.CreatePersonActionCommand.OperatorRole"/>
/// for the resolution reference (§24-1).
/// </param>
/// <param name="VahedCode">VAHEDCODE column (max 4 chars, optional).</param>
public sealed record UpdatePersonActionCommand(
    Guid Id,
    string? UserName,
    string UserId,
    string? FromDate,
    string? ToDate,
    bool? Status,
    OperatorRole OperatorRole) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column, server-assigned by <c>VahedScopeBehavior</c> from the authenticated
    /// caller - see <c>CreatePersonActionCommand.VahedCode</c> for why it is no longer a
    /// constructor parameter. On update it matters twice over: it both stops a caller moving an
    /// existing row into another unit, and is the value the ownership guard compares against.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
