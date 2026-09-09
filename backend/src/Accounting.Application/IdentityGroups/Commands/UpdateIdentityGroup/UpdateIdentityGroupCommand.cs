using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.IdentityGroups.Commands.UpdateIdentityGroup;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_IDENTITYGROUP</c> row (PUT semantics,
/// not PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>: identity and creation audit are
/// immutable after insert, and <c>ISDELETED</c> is owned exclusively by
/// <c>DeleteIdentityGroupCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise absent
/// because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update is explicitly out of scope for this pass, by
/// project-owner decision. The IDOR risk on direct-by-id access therefore remains open for
/// Update; only the "what unit does this row end up in" half of the problem is closed here.
/// </summary>
/// <param name="Id">The <c>TB_IDENTITYGROUP.ID</c> to update (bound from the route, never the body).</param>
/// <param name="IdentityGroupsDesc">IDENTITYGROUPS_DESC column (max 100 chars, required).</param>
/// <param name="IdentityGroupsCode">
/// IDENTITYGROUPS_CODE column (max 3 chars, optional — participates in <c>UK_IDENTITYGROUPCODE</c>).
/// </param>
/// <param name="TafsiliId">Optional FK to <c>TB_TAFSILI</c> (constraint <c>FK_IDENTITY_TAFSILI</c>).</param>
public sealed record UpdateIdentityGroupCommand(
    Guid Id,
    string IdentityGroupsDesc,
    string? IdentityGroupsCode,
    Guid? TafsiliId) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars, required organizational unit code). Never bound from the
    /// request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and
    /// the Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>UpdateIdentityGroupCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism, and the scope note above for
    /// what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
