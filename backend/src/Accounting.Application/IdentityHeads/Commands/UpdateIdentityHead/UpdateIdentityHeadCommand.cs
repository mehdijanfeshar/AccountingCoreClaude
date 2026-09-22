using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.IdentityHeads.Commands.Common;
using MediatR;

namespace Accounting.Application.IdentityHeads.Commands.UpdateIdentityHead;

/// <summary>
/// Replaces the fixed values of an existing شناسنامه. PUT-semantics, not PATCH: the
/// <c>FixItems</c> set is the complete replacement set — an item left out is soft-deleted.
///
/// <para>
/// <b>Neither <c>IdentityGroupId</c> nor <c>Serial</c> can be changed.</b> The group decides which
/// subgroups the values even mean, so re-pointing a شناسنامه at a different group would leave its
/// items dangling against subgroups of the old one; and the serial is the record's identity
/// within that group. Changing either is a different operation from editing values, and the
/// reference project does not offer it. If it is ever needed it should be its own command rather
/// than a field added here — the same separation the reference applies to voucher state
/// (<c>ChangeState</c>).
/// </para>
///
/// <para>
/// Audit columns (<c>CHANGEUSERID</c>, <c>UPDATEDDATE</c>) come from <c>ICurrentUser</c> and the
/// server clock, never from client input.
/// </para>
///
/// ⚠️ Carries the same unvalidated-subgroup caveat as <c>CreateIdentityHeadCommand</c>.
/// </summary>
/// <param name="Id">ID of the شناسنامه to update. Taken from the route, never the body.</param>
/// <param name="FixItems">
/// The complete replacement set of fixed values. Passing an empty list clears every value;
/// passing <see langword="null"/> is treated the same way, so "not supplied" is never silently
/// interpreted as "leave them alone" — that ambiguity is exactly what PUT-semantics avoids.
/// </param>
public sealed record UpdateIdentityHeadCommand(
    Guid Id,
    IReadOnlyList<IdentityHeadFixItemInput>? FixItems = null) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column. Never bound from the request body and never trusted from the caller —
    /// <c>VahedScopeBehavior</c> overwrites it with the authenticated caller's own unit code.
    ///
    /// ⚠️ <b>Scope note:</b> this guarantees the value <i>written</i> is the caller's own unit; it
    /// does <b>not</b> check that the row being updated already belonged to that unit. That is the
    /// still-open half of risk #1 and applies identically to every Update in this project.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
