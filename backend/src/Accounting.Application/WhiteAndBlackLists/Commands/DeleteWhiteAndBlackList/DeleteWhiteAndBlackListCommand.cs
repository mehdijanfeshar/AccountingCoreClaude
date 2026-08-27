using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.DeleteWhiteAndBlackList;

/// <summary>
/// Soft-deletes a <c>TB_WHITEANDBLACKLIST</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
/// </summary>
/// <param name="Id">The <c>TB_WHITEANDBLACKLIST.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteWhiteAndBlackListCommand(Guid Id) : IRequest;
