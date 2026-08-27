using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackList;

/// <summary>
/// Creates a new <c>TB_WHITEANDBLACKLIST</c> row (Legacy account-code allow/deny-list entry).
/// Carries primitive fields only — the handler is responsible for constructing the Domain
/// entity. Returns the newly generated <see cref="Guid"/> ID.
/// </summary>
/// <param name="AccountCodeId">Required link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNTCODE_LINK_WHITELISTS</c>); the column is non-nullable in Legacy.</param>
/// <param name="VahedTypeId">Optional link to <c>TB_VAHED_TYPE</c> (<c>FK_VAHEDTYPE_WHITEANDBLACKLIST</c>).</param>
/// <param name="FromAuthorizedDate">FROMAUTHORIZEDDATE column (max 8 chars — Legacy string-encoded date).</param>
/// <param name="ToAuthorizedDate">TOAUTHORIZEDDATE column (max 8 chars).</param>
/// <param name="FromLimitationDate">FROMLIMITATIONDATE column (max 8 chars).</param>
/// <param name="ToLimitationDate">TOLIMITATIONDATE column (max 8 chars).</param>
/// <param name="State">
/// STATE column. WARNING: the underlying Oracle column is <c>NUMBER(1)</c> and is documented
/// (<c>docs/centralaccount-business-reference.md</c> section 10-2, row 12) as really being a
/// 3-valued <c>StateEnum</c> (1=allowed, 2=system-only, 3=disallowed), not a boolean. Modeled as
/// <c>bool?</c> here only because that is the entity's current (known-imprecise) CLR type —
/// fixing this is a separate, explicitly out-of-scope task. Treat this field's contract as
/// subject to change.
/// </param>
public sealed record CreateWhiteAndBlackListCommand(
    Guid AccountCodeId,
    Guid? VahedTypeId,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate,
    string? FromLimitationDate,
    string? ToLimitationDate,
    bool? State) : IRequest<Guid>;
