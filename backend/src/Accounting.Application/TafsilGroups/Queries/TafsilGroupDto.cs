using Accounting.Domain.ValueObjects;

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
/// PERSONTYPE column — <see cref="PersonTypes"/> (1=Person, 2=Legal, 3=Other). Resolved from the
/// CLAUDE.md risk #2 <c>bool?</c>/enum scaffolding bug in phase 27 batch 1 — see
/// <c>docs/centralaccount-business-reference.md</c> §24-1.
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
    PersonTypes? PersonType,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
