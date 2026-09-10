namespace Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;

/// <summary>
/// One "active" تفصیلی level configured for a معین (<c>TB_ACCOUNTCODE</c>) — see
/// <see cref="GetTafsiliLevelsQuery"/> XML doc for Rule A ("active" == both allowed AND
/// required, by mere existence of the underlying <c>TB_ACCOUNT_LINK_LEVEL</c> row).
/// </summary>
/// <param name="LevelId">
/// <c>TB_LEVEL_TAFSIL.ID</c>. Pass this straight through as the <c>levelId</c> route segment of
/// <c>GET /api/account-codes/{accountCodeId}/tafsili-levels/{levelId}/items</c>.
/// </param>
/// <param name="Code">
/// Parsed <c>TB_LEVEL_TAFSIL.LEVEL_CODE</c> (expected range 1..7). Load-bearing for the
/// frontend: levels 1-3 render inline, 4-7 behind a modal.
/// </param>
/// <param name="LevelName"><c>TB_LEVEL_TAFSIL.LEVEL_NAME</c> — display label.</param>
/// <param name="IsRequired">
/// Always <see langword="true"/>. Exists purely so Rule A is expressed in the API contract
/// itself instead of being silently re-implemented/assumed on the frontend (CLAUDE.md team rule
/// #1: never leave a validation-relevant business rule implicit in the UI only). There is no
/// "allowed but optional" state in this schema, so this field is never <see langword="false"/>.
/// </param>
public sealed record TafsiliLevelDto(Guid LevelId, int Code, string LevelName, bool IsRequired);
