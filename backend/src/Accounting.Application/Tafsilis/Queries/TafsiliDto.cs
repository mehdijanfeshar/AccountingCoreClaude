using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tafsilis.Queries;

/// <summary>
/// Read-side projection of <c>TB_TAFSILI</c> (Legacy detail/subsidiary-ledger account master).
/// Used by both <c>GetTafsilis</c> (list) and <c>GetTafsiliById</c> — the Domain entity never
/// crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="TafsiliCode">TAFSILI_CODE column (max 15 chars) — alone enforces <c>UK_TASILI</c> (NOT composite with <c>ISDELETED</c>, unlike <c>TafsilGroupDto.TafsilGroupCode</c>; a soft-deleted code can never be reused).</param>
/// <param name="TafsiliName">TAFSILI_NAME column (max 200 chars).</param>
/// <param name="TafsilDesc">TAFSIL_DESC column (max 200 chars, optional).</param>
/// <param name="IsActive">ISACTIVE column — <see cref="TafsiliActiveState"/> (1=IsActive, 2=DeActive; Oracle default <c>1</c>). Resolved from the CLAUDE.md risk #2 <c>bool?</c>/enum scaffolding bug in phase 27 batch 1 — see <c>docs/centralaccount-business-reference.md</c> §24-1.</param>
/// <param name="PersonType">PERSONTYPE column — <see cref="PersonTypes"/> (1=Person, 2=Legal, 3=Other). Same phase-27-batch-1 fix as <see cref="IsActive"/>.</param>
/// <param name="Owner">OWNER column — <see cref="Owners"/> (1=Global/سراسری, 2=Unit/داخلی). Same phase-27-batch-1 fix. The Oracle column's own comment ("2=setad 1=vahed") is stale/inverted relative to the reference project's actual enum — see <see cref="Owners"/> XML doc; do not "correct" the enum values to match that comment.</param>
/// <param name="VahedType">VAHEDTYPE column — reuses <see cref="VahedCategory"/> (1=Insurance, 2=Treatment, 3=All). Same phase-27-batch-1 fix. ⚠️ Strong evidence, not proven on our data — see <see cref="VahedCategory"/> XML doc.</param>
/// <param name="VahedCode">VAHEDCODE column (max 4 chars) — real FK <c>FK_VAHEDCODE</c> to <c>TB_VAHED_INFO</c>; server-assigned on write, see <c>CreateTafsiliCommand.VahedCode</c>.</param>
/// <param name="TafsilGroupIds">
/// Non-deleted <c>TB_TAFSIL_GROUP.ID</c> values currently linked to this تفصیلی via the embedded
/// <c>TB_TAFSIL_LINK_TAFSILGROUP</c> join table (see <c>ITafsiliRepository.SetTafsilGroupLinksAsync</c>
/// XML doc for why that table has no independent write path of its own). Never empty vs. null —
/// always a (possibly empty) list.
/// </param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag. Exposed as-is.</param>
public sealed record TafsiliDto(
    Guid Id,
    string? TafsiliCode,
    string? TafsiliName,
    string? TafsilDesc,
    TafsiliActiveState? IsActive,
    PersonTypes? PersonType,
    Owners? Owner,
    VahedCategory? VahedType,
    string? VahedCode,
    IReadOnlyList<Guid> TafsilGroupIds,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
