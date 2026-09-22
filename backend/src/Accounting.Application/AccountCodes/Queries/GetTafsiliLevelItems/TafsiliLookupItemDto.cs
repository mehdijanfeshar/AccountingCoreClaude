namespace Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;

/// <summary>
/// One selectable تفصیلی item for a given (معین, level) pair — a page item of
/// <see cref="GetTafsiliLevelItemsQuery"/>. Deliberately excludes <c>TB_TAFSILI.ISACTIVE</c> —
/// see that query's XML doc for why leaving out that filter is a known, deliberately-deferred
/// gap (independent of that column's phase-27-batch-1 <c>bool?</c>→enum type fix) — and the
/// item's own <c>VAHEDCODE</c>/owner info, which describe the تفصیلی's OWNING unit and are
/// unrelated to Rule B (already applied server-side to decide whether the row is even eligible
/// to appear here at all).
/// </summary>
/// <param name="Id">TB_TAFSILI.ID.</param>
/// <param name="TafsiliCode">TB_TAFSILI.TAFSILI_CODE.</param>
/// <param name="TafsiliName">TB_TAFSILI.TAFSILI_NAME.</param>
/// <param name="Label">
/// <c>"{TafsiliCode} - {TafsiliName}"</c> — matches the reference project's own
/// <c>Describtion</c> field (<c>TbAccountLinkTafsilGroupRepository.cs</c>). Composed directly in
/// the repository's EF projection (<c>TafsiliLookupReadRepository.ToDto</c>) via plain string
/// concatenation, which EF Core translates to SQL concatenation — never evaluated client-side.
/// </param>
public sealed record TafsiliLookupItemDto(Guid Id, string? TafsiliCode, string? TafsiliName, string Label);
