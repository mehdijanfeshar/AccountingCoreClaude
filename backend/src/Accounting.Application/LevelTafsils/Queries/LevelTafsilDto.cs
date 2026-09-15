namespace Accounting.Application.LevelTafsils.Queries;

/// <summary>
/// Read-side projection of <c>TB_LEVEL_TAFSIL</c> (Legacy tafsili-level lookup — many other
/// tables, e.g. <c>TB_ACCOUNT_LINK_LEVEL</c>, FK into this table). Used by both
/// <c>GetLevelTafsils</c> (list) and <c>GetLevelTafsilById</c> — the Domain entity never crosses
/// the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="LevelCode">LEVEL_CODE column (max 2 chars).</param>
/// <param name="LevelName">LEVEL_NAME column (max 50 chars).</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag. Exposed as-is.</param>
public sealed record LevelTafsilDto(
    Guid Id,
    string LevelCode,
    string LevelName,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
