namespace Accounting.Application.Accounts.Queries;

/// <summary>
/// Read-side projection of <c>TB_ACCOUNTCODE</c>. Used by both <c>GetAccountCodes</c> (list)
/// and <c>GetAccountCodeById</c> — the Domain entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="TypeCode">TYPECODE column (legacy boolean flag).</param>
/// <param name="ParentId">
/// PARENTID column. NOTE: this is a risk-flagged column per CLAUDE.md — <c>LegacyDbContext</c>
/// reads it strictly via <c>GuidToChar36Converter</c> (ParseExact) and will throw a loud
/// <see cref="FormatException"/> rather than silently rewrite the value if the underlying
/// Oracle value is ever a non-dashed / non-GUID sentinel.
/// </param>
/// <param name="AccCode">Account code (unique, enforced by DB constraint <c>UK_ACCOUNTCODE</c>).</param>
/// <param name="AccCodeName">Account code title.</param>
/// <param name="TypeActivity">TYPEACTIVITY column — نوع فعالیت (بستانکار/بدهکار/بد-بس).</param>
/// <param name="SourceAndConsumeId">Optional link to source/consume classification.</param>
/// <param name="IdentyGroupsId">Optional link to identity group.</param>
/// <param name="TypeAccCode">TYPEACCCODE column — نوع حساب (موقت/دائم).</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
/// <param name="MoInforClose">MOINFORCLOSE column.</param>
/// <param name="TypeAction">TYPEACTION column — نوع خلاف ماهیت.</param>
public sealed record AccountCodeDto(
    Guid Id,
    bool? TypeCode,
    Guid? ParentId,
    string? AccCode,
    string? AccCodeName,
    bool? TypeActivity,
    Guid? SourceAndConsumeId,
    Guid? IdentyGroupsId,
    bool? TypeAccCode,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted,
    string? MoInforClose,
    bool? TypeAction);
