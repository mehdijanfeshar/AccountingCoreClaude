namespace Accounting.Application.AccountCodeInterfaces.Queries;

/// <summary>
/// Read-side projection of <c>TB_ACCOUNTCODE_INTERFACE</c>. Used by both
/// <c>GetAccountCodeInterfaces</c> (list) and <c>GetAccountCodeInterfaceById</c> — the Domain
/// entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="Type">
/// TYPE column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). NOTE: suspected to
/// actually be a multi-valued enum — see
/// <see cref="Accounting.Application.AccountCodeInterfaces.Commands.CreateAccountCodeInterface.CreateAccountCodeInterfaceCommand.Type"/>.
/// </param>
/// <param name="AccountCodeId">ACCOUNTCODEID column — FK to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record AccountCodeInterfaceDto(
    Guid Id,
    bool Type,
    Guid AccountCodeId,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
