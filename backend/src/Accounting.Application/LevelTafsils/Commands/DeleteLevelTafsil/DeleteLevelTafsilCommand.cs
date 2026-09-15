using MediatR;

namespace Accounting.Application.LevelTafsils.Commands.DeleteLevelTafsil;

/// <summary>
/// Soft-deletes a <c>TB_LEVEL_TAFSIL</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
///
/// ⚠️ Does NOT check for dependent rows in tables that FK into <c>TB_LEVEL_TAFSIL</c> (e.g.
/// <c>TB_ACCOUNT_LINK_LEVEL</c>, <c>TB_ACCOUNT_LINK_TAFSILGROUP</c>,
/// <c>TB_ACCOUNT_LINK_TAFSILI</c>) before soft-deleting — mirroring the recorded open risk on
/// <c>DeleteAccountCodeCommand</c> in CLAUDE.md ("حذف گرهٔ کدینگ هیچ بررسی وابستگی ندارد").
/// Implementing such a check is an unmade business decision, not attempted here.
/// </summary>
/// <param name="Id">The <c>TB_LEVEL_TAFSIL.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteLevelTafsilCommand(Guid Id) : IRequest;
