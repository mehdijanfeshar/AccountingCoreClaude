namespace Accounting.Application.PersonActions.Queries;

/// <summary>
/// Read-side projection of <c>TB_PERSON_ACTION</c>. Used by both <c>GetPersonActions</c> (list)
/// and <c>GetPersonActionById</c> — the Domain entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="UserName">USERNAME column.</param>
/// <param name="UserId">USERID column — participates in <c>UK_PERSON_ACTION</c>.</param>
/// <param name="FromDate">FROMDATE column (a Persian date string) — participates in <c>UK_PERSON_ACTION</c>.</param>
/// <param name="ToDate">TODATE column (a Persian date string) — participates in <c>UK_PERSON_ACTION</c>.</param>
/// <param name="Status">STATUS column (nullable <c>bool</c>).</param>
/// <param name="OperatorRole">
/// OPERATORROLE column (non-nullable <c>bool</c>). NOTE: suspected to actually be a
/// multi-valued enum — see
/// <see cref="Accounting.Application.PersonActions.Commands.CreatePersonAction.CreatePersonActionCommand.OperatorRole"/>.
/// </param>
/// <param name="VahedCode">VAHEDCODE column.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag. Exposed as-is.</param>
public sealed record PersonActionDto(
    Guid Id,
    string? UserName,
    string? UserId,
    string? FromDate,
    string? ToDate,
    bool? Status,
    bool OperatorRole,
    string? VahedCode,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
