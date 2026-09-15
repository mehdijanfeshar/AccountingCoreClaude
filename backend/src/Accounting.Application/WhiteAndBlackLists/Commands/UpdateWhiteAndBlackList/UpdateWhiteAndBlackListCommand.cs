using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.UpdateWhiteAndBlackList;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_WHITEANDBLACKLIST</c> row (PUT
/// semantics, not PATCH) — the same replace-vs-patch rationale as <c>UpdateAccountCodeCommand</c>
/// applies here.
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteWhiteAndBlackListCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c>
/// are likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
/// </summary>
/// <param name="Id">The <c>TB_WHITEANDBLACKLIST.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountCodeId">Required link to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="VahedTypeId">Optional link to <c>TB_VAHED_TYPE</c>.</param>
/// <param name="FromAuthorizedDate">FROMAUTHORIZEDDATE column (max 8 chars).</param>
/// <param name="ToAuthorizedDate">TOAUTHORIZEDDATE column (max 8 chars).</param>
/// <param name="FromLimitationDate">FROMLIMITATIONDATE column (max 8 chars).</param>
/// <param name="ToLimitationDate">TOLIMITATIONDATE column (max 8 chars).</param>
/// <param name="State">
/// STATE column. WARNING: really a 3-valued enum in Legacy, currently modeled as <c>bool?</c> —
/// see <see cref="Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackList.CreateWhiteAndBlackListCommand.State"/>
/// for the full rationale. This field's contract is expected to change.
/// </param>
public sealed record UpdateWhiteAndBlackListCommand(
    Guid Id,
    Guid AccountCodeId,
    Guid? VahedTypeId,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate,
    string? FromLimitationDate,
    string? ToLimitationDate,
    bool? State) : IRequest;
