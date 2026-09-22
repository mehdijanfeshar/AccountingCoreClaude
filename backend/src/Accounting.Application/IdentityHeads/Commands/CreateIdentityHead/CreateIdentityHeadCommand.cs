using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.IdentityHeads.Commands.Common;
using MediatR;

namespace Accounting.Application.IdentityHeads.Commands.CreateIdentityHead;

/// <summary>
/// Creates one شناسنامه (<c>TB_IDENTITYHEAD</c>) together with its fixed-value items
/// (<c>TB_IDENTITYFIXITEMS</c>), in a single transaction. Returns the new head's
/// <see cref="Guid"/>.
///
/// <para>
/// This mirrors the reference project exactly: its <c>IdentityHead</c> entity builds the fix
/// items itself in <c>AddIdentityFixItems(...)</c>, and the reference UI posts one request
/// carrying <c>identitygroupsId</c> plus an <c>itemDtos</c> array. A head with no items would be
/// meaningless — it is the values that make a شناسنامه — so this is a composite create, not a
/// head-only one followed by N item calls.
/// </para>
///
/// <para>
/// <b><c>Serial</c> is not a parameter.</b> It is assigned server-side as one past the highest
/// serial already stored for the same (group, unit, year) — the reference UI does not send it
/// either. A concurrent create can compute the same serial; that loses to the real UNIQUE
/// constraint <c>AK_AK_IDENTYHEAD_IDENTYHE</c> and surfaces as a 409, which is the project's
/// established answer to this shape of race rather than a racy pre-check.
/// </para>
///
/// ⚠️ <b>The fixed subgroups are not validated against the chosen group.</b> Nothing here checks
/// that each <c>IdentitySubGroupId</c> belongs to <c>IdentityGroupId</c>, or that it is a
/// <c>Fixed</c> rather than a <c>Variable</c> subgroup — Legacy has no constraint for either and
/// inventing one would be fabricating a business rule. The UI builds its inputs from
/// <c>GET /api/identity-sub-groups?identityGroupId=…&amp;kind=1</c>, so a correct client cannot
/// produce a mismatch; a hand-crafted request can. Recorded in <c>docs/open-decisions.md</c>.
/// </summary>
/// <param name="IdentityGroupId">
/// IDENTITYGROUPS_ID — the شناسنامه group this record belongs to. Backed by a real FK
/// (<c>FK_IDENTYHE_IDENTYGR</c>), so an unknown id is mapped centrally to a 400.
/// </param>
/// <param name="Year">YEAR column (max 4 chars) — fiscal year.</param>
/// <param name="FixItems">
/// The value of each fixed subgroup of the group. May be empty, which creates a شناسنامه with no
/// values — allowed because Legacy allows it, though the UI never does it.
/// </param>
public sealed record CreateIdentityHeadCommand(
    Guid IdentityGroupId,
    string Year,
    IReadOnlyList<IdentityHeadFixItemInput>? FixItems = null) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars; part of <c>AK_AK_IDENTYHEAD_IDENTYHE</c>). Never bound from
    /// the request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and
    /// the Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites it with the authenticated caller's own
    /// unit code before the request reaches the handler.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
