using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Tafsilis.Commands.CreateTafsili;

/// <summary>
/// Creates a new <c>TB_TAFSILI</c> row (Legacy detail/subsidiary-ledger account master). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
///
/// Uniqueness: <c>TAFSILI_CODE</c> alone enforces the Oracle constraint <c>UK_TASILI</c> — unlike
/// <c>TB_TAFSIL_GROUP</c>'s composite key, <c>ISDELETED</c> does NOT participate here, so a
/// soft-deleted code can never be reused. No pre-check is performed — a duplicate surfaces as an
/// Oracle ORA-00001, translated centrally by <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.DuplicateKeyException"/> → 409.
///
/// <b>Embedded گروه‌تفصیلی linking, not a separate Command.</b> <see cref="TafsilGroupIds"/> is
/// written to <c>TB_TAFSIL_LINK_TAFSILGROUP</c> by the handler itself via
/// <see cref="Accounting.Application.Common.Interfaces.ITafsiliRepository.AddTafsiliGroupLinkAsync"/>
/// (create — the row set is empty for a brand-new تفصیلی, so there is nothing to reconcile away)
/// — that table is one of the project's permanently-embedded <c>*_LINK_TAFSIL*</c> tables
/// (<c>docs/open-decisions.md</c>) and must never get an independent Command/Controller of its
/// own. Each created link row's own <c>VAHEDCODE</c> is always the caller's own unit;
/// <c>VAHEDTYPE</c> comes from <see cref="TafsilGroupLinkVahedType"/> — see that parameter's doc
/// and <see cref="CreateTafsiliCommandHandler"/> for the reference-project-verified Rule B this
/// implements.
/// </summary>
/// <param name="TafsiliCode">TAFSILI_CODE column (required, max 15 chars; alone enforces <c>UK_TASILI</c>).</param>
/// <param name="TafsiliName">TAFSILI_NAME column (required, max 200 chars).</param>
/// <param name="TafsilDesc">TAFSIL_DESC column (optional, max 200 chars).</param>
/// <param name="IsActive">
/// ISACTIVE column (<c>NUMBER(1)</c>, mapped nullable <c>bool</c>; Oracle default <c>1</c>). A
/// candidate for the known project-wide <c>bool?</c>/enum scaffolding bug (CLAUDE.md Phase 12) —
/// not scanned/confirmed yet, modeled as-is.
/// </param>
/// <param name="PersonType">PERSONTYPE column. Same unverified-enum caveat as <see cref="IsActive"/>.</param>
/// <param name="Owner">OWNER column — Domain XML comment says "2=setad 1=vahed", which a <see cref="bool"/>? cannot represent faithfully. Modeled as-is; not resolved here.</param>
/// <param name="VahedType">VAHEDTYPE column. Same unverified-enum caveat as <see cref="Owner"/>.</param>
/// <param name="TafsilGroupIds">
/// <c>TB_TAFSIL_GROUP.ID</c> values to link this تفصیلی to (may be empty). See the class XML doc
/// for how this is persisted.
/// </param>
/// <param name="TafsilGroupLinkVahedType">
/// Visibility scope stamped onto every <c>TB_TAFSIL_LINK_TAFSILGROUP</c> row created for
/// <see cref="TafsilGroupIds"/> — i.e. which other units' voucher-entry تفصیلی lookups
/// (<c>GetTafsiliLevelItemsQuery</c> Rule B) can see this تفصیلی through those groups.
/// <see langword="null"/> (the default) means "only my own unit" (narrowest — the caller's own
/// <c>VahedCode</c> still applies via Rule B's exact-match clause); <see cref="VahedCategory.All"/>
/// makes it visible org-wide. Traced directly from the reference project
/// (<c>add-base-tafsili.component.ts</c>): its form defaulted this to "All" for SETAD/admin-role
/// callers and forced it to <see langword="null"/> otherwise — we do not replicate that role gate
/// (no equivalent role concept exists here yet), so the caller explicitly opts in instead.
/// </param>
public sealed record CreateTafsiliCommand(
    string TafsiliCode,
    string TafsiliName,
    string? TafsilDesc,
    bool? IsActive,
    bool? PersonType,
    bool? Owner,
    bool? VahedType,
    IReadOnlyList<Guid> TafsilGroupIds,
    VahedCategory? TafsilGroupLinkVahedType = null) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (max 4 chars). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreateTafsiliCommandHandler</c>. See <see cref="IVahedScopedCommand"/>
    /// for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
