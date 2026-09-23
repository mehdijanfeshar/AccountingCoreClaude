using Accounting.Domain.ValueObjects;

namespace Accounting.Application.WhiteAndBlackLists.Queries;

/// <summary>
/// Read-side projection of <c>TB_WHITEANDBLACKLIST</c>. Used by both
/// <c>GetWhiteAndBlackLists</c> (list) and <c>GetWhiteAndBlackListById</c> — the Domain entity
/// never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — link to <c>TB_ACCOUNTCODE</c> (non-nullable in Legacy).</param>
/// <param name="VahedTypeId">VAHEDTYPE_ID column — optional link to <c>TB_VAHED_TYPE</c>.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp (non-nullable in Legacy).</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier (non-nullable in Legacy).</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
/// <param name="FromAuthorizedDate">FROMAUTHORIZEDDATE column.</param>
/// <param name="ToAuthorizedDate">TOAUTHORIZEDDATE column.</param>
/// <param name="FromLimitationDate">FROMLIMITATIONDATE column.</param>
/// <param name="ToLimitationDate">TOLIMITATIONDATE column.</param>
/// <param name="State">
/// STATE column (nullable <see cref="Accounting.Domain.ValueObjects.WhiteBlackListState"/>).
/// Resolved per <c>docs/centralaccount-business-reference.md</c> §24-1 (phase 27 batch 3).
/// </param>
/// <param name="AccCode">
/// Display-only: <c>TB_ACCOUNTCODE.ACCCODE</c> of the linked account, resolved by the read
/// repository through the <c>FK_ACCOUNTCODE_LINK_WHITELISTS</c> navigation. Present so the
/// «دسترسی کدینگ حسابداری» grid can render its «کد معین» column without a second round-trip
/// per row. Never written back — the write side still takes <c>AccountCodeId</c>.
/// </param>
/// <param name="AccCodeName">Display-only: <c>TB_ACCOUNTCODE.ACCCODENAME</c>. Same rationale as <paramref name="AccCode"/>.</param>
/// <param name="VahedTypeCode">Display-only: <c>TB_VAHED_TYPE.TYPECODE</c>, or <see langword="null"/> when <paramref name="VahedTypeId"/> is null.</param>
/// <param name="VahedTypeName">Display-only: <c>TB_VAHED_TYPE.TYPENAME</c> — the grid's «نوع واحد» column.</param>
/// <param name="VahedTypeParentCode">
/// Display-only: <c>TB_VAHED_TYPE.PARENTTYPECODE</c> — what the grid's «بخش» column groups on.
/// ⚠️ It is a bare code (<c>"1"</c>/<c>"2"</c>/<c>"3"</c> in live data) with <b>no lookup table
/// anywhere in the schema</b>, and the reference project's own tree endpoint
/// (<c>GetVahedTypeQueryTreeHandler</c>) returns it unlabelled. Turning it into
/// «بیمه»/«درمان»/«ستاد» is therefore a presentation-layer decision and is deliberately NOT made
/// here — see the recorded assumption in <c>docs/open-decisions.md</c>.
/// </param>
public sealed record WhiteAndBlackListDto(
    Guid Id,
    Guid AccountCodeId,
    Guid? VahedTypeId,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    string AddUserId,
    string? ChangeUserId,
    bool? IsDeleted,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate,
    string? FromLimitationDate,
    string? ToLimitationDate,
    WhiteBlackListState? State,
    string? AccCode = null,
    string? AccCodeName = null,
    string? VahedTypeCode = null,
    string? VahedTypeName = null,
    string? VahedTypeParentCode = null);
