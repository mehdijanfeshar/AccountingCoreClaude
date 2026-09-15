namespace Accounting.Application.PreDescribs.Queries;

/// <summary>
/// Read-side projection of <c>TB_PREDESCRIB</c>. Used by both <c>GetPreDescribs</c> (list) and
/// <c>GetPreDescribById</c> — the Domain entity never crosses the Application boundary.
///
/// Deliberately has NO <c>IsDeleted</c> field: <c>TB_PREDESCRIB</c> has no <c>ISDELETED</c>
/// column at all (locked in by
/// <c>Accounting.Application.Tests.PreDescribs.PreDescribSchemaAssumptionsTests</c>).
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountId">ACCOUNTID column — optional link to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="Descrip">Description text.</param>
/// <param name="AddUserId">Audit trail: creating user identifier (the only audit column this entity has).</param>
/// <param name="VahedCode">Optional organizational unit code.</param>
/// <param name="FlagVoucher">FLAGVOUCHER column — Oracle comment "head=0 Detail=1".</param>
public sealed record PreDescribDto(
    Guid Id,
    Guid? AccountId,
    string? Descrip,
    string? AddUserId,
    string? VahedCode,
    bool? FlagVoucher);
