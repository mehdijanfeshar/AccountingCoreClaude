using MediatR;

namespace Accounting.Application.WhiteLists.Commands.CreateWhiteList;

/// <summary>
/// Creates a new <c>TB_WHITELIST</c> row (Legacy account-code allow-list entry). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
/// </summary>
/// <param name="AccountCodeId">Required link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNTCODE_LINK_WHITELIST</c>); the column is non-nullable in Legacy.</param>
/// <param name="VahedTypeId">Optional link to <c>TB_VAHED_TYPE</c> (<c>FK_VAHEDTYPE_LINK_WHITELIST</c>).</param>
/// <param name="VahedInfoId">Optional link to <c>TB_VAHED_INFO</c> (<c>FK_VAHEDINFO_LINK_WHITELIST</c>).</param>
/// <param name="FromAuthorizedDate">FROMAUTHORIZEDDATE column (max 8 chars — Legacy string-encoded date).</param>
/// <param name="ToAuthorizedDate">TOAUTHORIZEDDATE column (max 8 chars).</param>
/// <param name="FromLimitationDate">FROMLIMITATIONDATE column (max 8 chars).</param>
/// <param name="ToLimitationDate">TOLIMITATIONDATE column (max 8 chars).</param>
public sealed record CreateWhiteListCommand(
    Guid AccountCodeId,
    Guid? VahedTypeId,
    Guid? VahedInfoId,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate,
    string? FromLimitationDate,
    string? ToLimitationDate) : IRequest<Guid>;
