using MediatR;

namespace Accounting.Application.LevelTafsils.Commands.CreateLevelTafsil;

/// <summary>
/// Creates a new <c>TB_LEVEL_TAFSIL</c> row (Legacy tafsili-level lookup). Carries primitive
/// fields only — the handler is responsible for constructing the Domain entity. Returns the
/// newly generated <see cref="Guid"/> ID.
///
/// <c>TB_LEVEL_TAFSIL</c> has no UNIQUE constraint and no FK of its own, so no write to this
/// table can ever fail with a constraint violation from another row's data — see
/// <c>Accounting.Api.Controllers.LevelTafsilsController</c> XML doc.
/// </summary>
/// <param name="LevelCode">LEVEL_CODE column (max 2 chars, required).</param>
/// <param name="LevelName">LEVEL_NAME column (max 50 chars, required).</param>
public sealed record CreateLevelTafsilCommand(
    string LevelCode,
    string LevelName) : IRequest<Guid>;
