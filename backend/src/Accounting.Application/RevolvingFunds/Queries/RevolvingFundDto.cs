namespace Accounting.Application.RevolvingFunds.Queries;

/// <summary>
/// Read-side projection of <c>TB_REVOLVING_FUND</c>. Used by both <c>GetRevolvingFunds</c>
/// (list) and <c>GetRevolvingFundById</c> — the Domain entity never crosses the Application
/// boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="Code">CODE column — part of <c>UK_REVOLVING_CODE</c>.</param>
/// <param name="Name">NAME column.</param>
/// <param name="Description">DESCRIPTION column.</param>
/// <param name="DefaultAmount">DEFAULTAMOUNT column (<c>NUMBER(25)</c>).</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — optional link to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="VahedCode">VAHEDCODE column — part of <c>UK_REVOLVING_CODE</c>.</param>
/// <param name="Year">YEAR column — part of <c>UK_REVOLVING_CODE</c>.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record RevolvingFundDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal? DefaultAmount,
    Guid? AccountCodeId,
    string? VahedCode,
    string? Year,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
