namespace Accounting.Application.CheckBooks.Queries;

/// <summary>
/// Read-side projection of <c>TB_CHECKBOOK</c>. Used by both <c>GetCheckBooks</c> (list) and
/// <c>GetCheckBookById</c> — the Domain entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountId">ACCOUNT_ID column — required link to <c>TB_ACCOUNT</c>.</param>
/// <param name="CheckBookTitle">CHECKBOOK_TITLE column — optional.</param>
/// <param name="CheckBookDate">CHECKBOOK_DATE column — Legacy string date.</param>
/// <param name="FromCheckNumber">FROMCHECKNUMBER column — part of <c>UK_CHECKBOOK</c>.</param>
/// <param name="ToCheckNumber">TOCHECKNUMBER column — part of <c>UK_CHECKBOOK</c>.</param>
/// <param name="CheckTypeId">CHECKTYPE_ID column — optional link to <c>TB_CHECK_TYPE</c>.</param>
/// <param name="VahedCode">VAHEDCODE column — part of <c>UK_CHECKBOOK</c>.</param>
/// <param name="CheckBookType">CHECKBOOK_TYPE column — see <c>CreateCheckBookCommand</c> XML doc for the unverified-enum note.</param>
/// <param name="Serial">SERIAL column — optional.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp (non-nullable on this table).</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier (non-nullable on this table).</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag (non-nullable <see cref="bool"/> on this table, unlike <c>TB_ACCOUNT</c>).
/// Exposed as-is (including on list results, where deleted rows have already been filtered out)
/// so callers can distinguish a not-deleted row from one that slipped through.
/// </param>
public sealed record CheckBookDto(
    Guid Id,
    Guid AccountId,
    string? CheckBookTitle,
    string CheckBookDate,
    string FromCheckNumber,
    string ToCheckNumber,
    Guid? CheckTypeId,
    string VahedCode,
    bool? CheckBookType,
    string? Serial,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    string AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
