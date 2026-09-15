using MediatR;

namespace Accounting.Application.CheckBooks.Commands.DeleteCheckBook;

/// <summary>
/// Soft-deletes a <c>TB_CHECKBOOK</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
///
/// ⚠️ Does NOT cascade to the permanently-embedded child <c>TB_CHECK</c> table — no
/// repository/write path exists for it in this project at all (per team rule, every parent
/// builder concept from the reference project — here <c>AddCheckPapers</c> — stays embedded and
/// untouched). Soft-deleting a checkbook leaves its <c>TB_CHECK</c> rows active — a known gap,
/// not fixed here.
/// </summary>
/// <param name="Id">The <c>TB_CHECKBOOK.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteCheckBookCommand(Guid Id) : IRequest;
