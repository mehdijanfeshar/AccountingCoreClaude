namespace Accounting.Application.TafsilGroups.Queries;

/// <summary>
/// Read-side projection of <c>TB_TAFSIL_GROUP</c> (Legacy tafsili-group lookup). Used by both
/// <c>GetTafsilGroups</c> (list) and <c>GetTafsilGroupById</c> — the Domain entity never crosses
/// the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="TafsilGroupCode">TAFSILGROUP_CODE column (max 3 chars) — participates in <c>UK_TBTAFSILGROUP</c> alongside <c>ISDELETED</c>.</param>
/// <param name="TafsilGroupName">TAFSILGROUP_NAME column (max 200 chars).</param>
/// <param name="PersonType">
/// PERSONTYPE column (<c>NUMBER(1)</c>, mapped as nullable <c>bool</c>). Unverified against the
/// CLAUDE.md Phase 12 <c>bool?</c>/enum scaffolding-bug pattern — modeled as-is.
/// </param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag. Exposed as-is — also participates in <c>UK_TBTAFSILGROUP</c>, see the Create command's XML doc for the implication.</param>
public sealed record TafsilGroupDto(
    Guid Id,
    string TafsilGroupCode,
    string TafsilGroupName,
    bool? PersonType,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
