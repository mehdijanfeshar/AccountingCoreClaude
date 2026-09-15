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
/// <param name="IsActive">ISACTIVE column (<c>NUMBER(1)</c>, mapped as nullable <c>bool</c>; Oracle default <c>1</c>). Unverified against the CLAUDE.md Phase 12 <c>bool?</c>/enum scaffolding-bug pattern — modeled as-is.</param>
/// <param name="PersonType">PERSONTYPE column. Same unverified-enum caveat as <see cref="IsActive"/>.</param>
/// <param name="Owner">OWNER column — Domain XML comment says "2=setad 1=vahed", which a <see cref="bool"/>? cannot actually represent (a two-valued enum stored where only true/false/null fit). Modeled as-is; do not treat <see langword="true"/>/<see langword="false"/> as confirmed to mean "vahed"/"setad" — this is flagged, not resolved.</param>
/// <param name="VahedType">VAHEDTYPE column. Same unverified-enum caveat as <see cref="Owner"/>.</param>
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
    bool? IsActive,
    bool? PersonType,
    bool? Owner,
    bool? VahedType,
    string? VahedCode,
    IReadOnlyList<Guid> TafsilGroupIds,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
