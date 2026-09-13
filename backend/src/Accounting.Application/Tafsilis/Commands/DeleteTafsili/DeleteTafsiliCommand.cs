using MediatR;

namespace Accounting.Application.Tafsilis.Commands.DeleteTafsili;

/// <summary>
/// Soft-deletes a <c>TB_TAFSILI</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md). Does NOT cascade to
/// <c>TB_TAFSIL_LINK_TAFSILGROUP</c> — those rows are left as-is (matching the project-wide
/// precedent that embedded link-table cascades are handled per-parent, and this one has not been
/// specced); a stale link pointing at a soft-deleted تفصیلی is a known, accepted characteristic,
/// not a bug introduced here.
/// </summary>
/// <param name="Id">The <c>TB_TAFSILI.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteTafsiliCommand(Guid Id) : IRequest;
