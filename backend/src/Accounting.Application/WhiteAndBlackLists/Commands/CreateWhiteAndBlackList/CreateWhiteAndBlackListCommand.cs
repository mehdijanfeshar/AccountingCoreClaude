using Accounting.Domain.ValueObjects;
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
/// STATE column (<c>NUMBER(1)</c>, mapped as nullable <see cref="Accounting.Domain.ValueObjects.WhiteBlackListState"/>).
/// Resolved per <c>docs/centralaccount-business-reference.md</c> §24-1 (phase 27 batch 3) —
/// previously an incorrect <c>bool?</c>; see the historical note preserved in the open risk #2
/// entry of CLAUDE.md.
/// </param>
public sealed record CreateWhiteAndBlackListCommand(
    Guid AccountCodeId,
    Guid? VahedTypeId,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate,
    string? FromLimitationDate,
    string? ToLimitationDate,
    WhiteBlackListState? State) : IRequest<Guid>;
