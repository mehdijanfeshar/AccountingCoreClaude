using MediatR;

namespace Accounting.Application.Accounts.Commands.UnlinkAccountCodeFromTafsilGroup;

/// <summary>
/// Soft-deletes a <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
/// </summary>
/// <param name="AccountCodeId">TB_ACCOUNTCODE.ID — the معین this link must belong to (bound from the route).</param>
/// <param name="LinkId">The <c>TB_ACCOUNT_LINK_TAFSILGROUP.ID</c> to soft-delete (bound from the route).</param>
public sealed record UnlinkAccountCodeFromTafsilGroupCommand(Guid AccountCodeId, Guid LinkId) : IRequest;
