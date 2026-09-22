using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Tafsilis.Commands.UpdateTafsili;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_TAFSILI</c> row (PUT semantics, not
/// PATCH) — mirrors <c>UpdateExpenseCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c> (immutable after insert, or owned
/// exclusively by another Command). <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise absent —
/// the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
///
/// <b>IS <see cref="IVahedScopedCommand"/></b> — <c>TB_TAFSILI.VAHEDCODE</c> is re-stamped to the
/// caller's own unit on every update, exactly like <c>UpdateExpenseCommand</c>. See that
/// command's XML doc "Scope note" for what this does and does NOT guarantee (it prevents
/// <c>VAHEDCODE</c> from being changed to an arbitrary unit; it does not verify the caller was
/// allowed to touch this particular row — record-ownership verification on Update remains an
/// open IDOR gap, same as every other entity in this project).
///
/// <see cref="TafsilGroupIds"/> replaces the entire linked-group set: the handler reconciles it
/// against <c>TB_TAFSIL_LINK_TAFSILGROUP</c> (soft-deletes rows for groups no longer present in
/// the list, adds rows for newly-added ones), exactly like
/// <c>UpdateVoucherDetailCommandHandler</c> does for <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c>. Only
/// newly-added link rows get <see cref="TafsilGroupLinkVahedType"/> stamped (see that parameter's
/// doc and <c>CreateTafsiliCommand</c>'s) — links that remain selected keep whatever visibility
/// scope they already had; this command never retroactively changes it for them.
///
/// Like Create, this can violate <c>UK_TASILI</c> against a *different* row, surfacing as the
/// same central ORA-00001 → 409 mapping.
/// </summary>
/// <param name="Id">The <c>TB_TAFSILI.ID</c> to update (bound from the route, never the body).</param>
/// <param name="TafsiliCode">TAFSILI_CODE column (required, max 15 chars).</param>
/// <param name="TafsiliName">TAFSILI_NAME column (required, max 200 chars).</param>
/// <param name="TafsilDesc">TAFSIL_DESC column (optional, max 200 chars).</param>
/// <param name="IsActive">ISACTIVE column — <see cref="TafsiliActiveState"/>. See <see cref="Accounting.Application.Tafsilis.Commands.CreateTafsili.CreateTafsiliCommand.IsActive"/> for the phase-27-batch-1 fix this reflects.</param>
/// <param name="PersonType">PERSONTYPE column — <see cref="PersonTypes"/>. Same fix.</param>
/// <param name="Owner">OWNER column — <see cref="Owners"/>. Same fix.</param>
/// <param name="VahedType">VAHEDTYPE column — reuses <see cref="VahedCategory"/>. Same fix.</param>
/// <param name="TafsilGroupIds"><c>TB_TAFSIL_GROUP.ID</c> values this تفصیلی should be linked to after this update (may be empty — clears every existing link).</param>
/// <param name="TafsilGroupLinkVahedType">
/// Visibility scope stamped onto any newly-added <c>TB_TAFSIL_LINK_TAFSILGROUP</c> row (ids
/// present in <see cref="TafsilGroupIds"/> that didn't already have an active link) — see
/// <c>CreateTafsiliCommand.TafsilGroupLinkVahedType</c> for the full rationale. Does not affect
/// links that remain selected from before this update.
/// </param>
public sealed record UpdateTafsiliCommand(
    Guid Id,
    string TafsiliCode,
    string TafsiliName,
    string? TafsilDesc,
    TafsiliActiveState? IsActive,
    PersonTypes? PersonType,
    Owners? Owner,
    VahedCategory? VahedType,
    IReadOnlyList<Guid> TafsilGroupIds,
    VahedCategory? TafsilGroupLinkVahedType = null) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (max 4 chars). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>UpdateTafsiliCommandHandler</c>. See <see cref="IVahedScopedCommand"/>
    /// for the full mechanism, and the class XML doc's scope note for what this does not cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
