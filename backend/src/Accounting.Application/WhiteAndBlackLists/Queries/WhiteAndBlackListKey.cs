namespace Accounting.Application.WhiteAndBlackLists.Queries;

/// <summary>
/// The natural key of a <c>TB_WHITEANDBLACKLIST</c> row as Oracle enforces it —
/// <c>UK_WHITEANDBLACKLIST</c> on
/// <c>(ACCOUNTCODE_ID, VAHEDTYPE_ID, FROMAUTHORIZEDDATE, TOAUTHORIZEDDATE)</c>, confirmed by
/// reading <c>user_constraints</c> on the live schema.
///
/// ⚠️ Note what is <b>not</b> in that key: the two limitation dates and <c>STATE</c>. That is why
/// the same (account, unit type) pair can legitimately appear more than once — and on live data it
/// already does, 291 times. Anything that reasons about "the permission row" for a pair must cope
/// with several, not assume one.
/// </summary>
/// <param name="AccountCodeId">ACCOUNTCODE_ID component.</param>
/// <param name="VahedTypeId">VAHEDTYPE_ID component (nullable in Legacy, and nullable in the UNIQUE key).</param>
/// <param name="FromAuthorizedDate">FROMAUTHORIZEDDATE component.</param>
/// <param name="ToAuthorizedDate">TOAUTHORIZEDDATE component.</param>
public readonly record struct WhiteAndBlackListKey(
    Guid AccountCodeId,
    Guid? VahedTypeId,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate);
