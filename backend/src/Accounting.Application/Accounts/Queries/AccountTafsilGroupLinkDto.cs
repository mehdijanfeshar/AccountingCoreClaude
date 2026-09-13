namespace Accounting.Application.Accounts.Queries;

/// <summary>
/// Read-side projection of one <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> row (Legacy معین↔گروه‌تفصیلی
/// link — "ارتباط معین با گروه تفصیلی" in the legacy Angular app). Backs
/// <c>GET /api/account-codes/{accountCodeId}/tafsil-group-links</c>.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountId">ACCOUNT_ID column — the معین (<c>TB_ACCOUNTCODE.ID</c>) this link belongs to.</param>
/// <param name="LevelId">LEVEL_ID column — the تفصیلی level (<c>TB_LEVEL_TAFSIL.ID</c>) this link applies to.</param>
/// <param name="TafsilGroupId">TAFSILGROUP_ID column — the گروه تفصیلی (<c>TB_TAFSIL_GROUP.ID</c>) linked for that (معین, سطح) pair.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag. Exposed as-is.</param>
public sealed record AccountTafsilGroupLinkDto(
    Guid Id,
    Guid AccountId,
    Guid LevelId,
    Guid TafsilGroupId,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
