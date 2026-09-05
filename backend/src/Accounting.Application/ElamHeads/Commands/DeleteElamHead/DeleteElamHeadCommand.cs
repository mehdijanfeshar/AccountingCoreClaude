using MediatR;

namespace Accounting.Application.ElamHeads.Commands.DeleteElamHead;

/// <summary>
/// Soft-deletes a <c>TB_ELAMHEAD</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
///
/// ⚠️ HEAD ONLY — does NOT cascade to <c>TB_ELAMDETAIL</c>. See
/// <c>Accounting.Api.Controllers.ElamHeadsController</c> XML doc.
/// </summary>
/// <param name="Id">The <c>TB_ELAMHEAD.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteElamHeadCommand(Guid Id) : IRequest;
