using MediatR;

namespace Accounting.Application.LevelTafsils.Commands.UpdateLevelTafsil;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_LEVEL_TAFSIL</c> row (PUT semantics,
/// not PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>: identity and creation audit are
/// immutable after insert, and <c>ISDELETED</c> is owned exclusively by
/// <c>DeleteLevelTafsilCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise absent
/// because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
/// </summary>
/// <param name="Id">The <c>TB_LEVEL_TAFSIL.ID</c> to update (bound from the route, never the body).</param>
/// <param name="LevelCode">LEVEL_CODE column (max 2 chars, required).</param>
/// <param name="LevelName">LEVEL_NAME column (max 50 chars, required).</param>
public sealed record UpdateLevelTafsilCommand(
    Guid Id,
    string LevelCode,
    string LevelName) : IRequest;
