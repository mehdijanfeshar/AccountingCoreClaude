using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Commands.UpdateAttribForAccountCode;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_ATTRIBFORACCOUNTCODE</c> row (PUT
/// semantics, not PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes
/// <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>: identity and creation
/// audit are immutable after insert, and <c>ISDELETED</c> is owned exclusively by
/// <c>DeleteAttribForAccountCodeCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise
/// absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
///
/// Like Create, this can violate <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c>
/// (<c>AccountCodeId</c>/<c>VahedCode</c>/<c>Year</c>) against a *different* row, surfacing as the
/// same central ORA-00001 → 409 mapping, and can violate <c>FK_ATTRIBFO_ACCOUNTCODE</c> the same
/// way Create can, surfacing as the same central ORA-02291 → 400 mapping.
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update (i.e. "does this <c>Id</c> already belong to
/// the caller's unit?") is explicitly out of scope for this pass, by project-owner decision. The
/// IDOR risk on direct-by-id access therefore remains open for Update; only the "what unit does
/// this row end up in" half of the problem is closed here.
/// </summary>
/// <param name="Id">The <c>TB_ATTRIBFORACCOUNTCODE.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountCodeId">Required link to <c>TB_ACCOUNTCODE</c> (<c>FK_ATTRIBFO_ACCOUNTCODE</c>).</param>
/// <param name="AttribBoxNo">ATTRIBBOXNO column. See <see cref="Accounting.Application.AttribForAccountCodes.Commands.CreateAttribForAccountCode.CreateAttribForAccountCodeCommand.AttribBoxNo"/> for the unverified-enum caveat.</param>
/// <param name="Flag">FLAG column. Same unverified-enum caveat as <see cref="AttribBoxNo"/>.</param>
/// <param name="LenAtr">LENATR column (<c>NUMBER(2)</c>, attribute digit length).</param>
/// <param name="AttribSum">ATTRIBSUM column. Same unverified-enum caveat as <see cref="AttribBoxNo"/>.</param>
/// <param name="ControlId">CONTROLID column. Same odd-mapping and unverified-enum caveats as the Create command.</param>
/// <param name="Year">YEAR column (max 4 chars, required).</param>
public sealed record UpdateAttribForAccountCodeCommand(
    Guid Id,
    Guid AccountCodeId,
    bool AttribBoxNo,
    bool Flag,
    byte LenAtr,
    bool AttribSum,
    bool? ControlId,
    string Year) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars, required). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>UpdateAttribForAccountCodeCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism, and the scope note above for
    /// what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
