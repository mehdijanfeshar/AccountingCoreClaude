using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository backing the frontend voucher-entry form's dynamic تفصیلی fields (phase
/// 20-b). Two independent read-only lookups over the domain chain:
/// <c>TB_ACCOUNTCODE → TB_ACCOUNT_LINK_LEVEL (+ TB_LEVEL_TAFSIL) → TB_ACCOUNT_LINK_TAFSILGROUP →
/// TB_TAFSIL_LINK_TAFSILGROUP → TB_TAFSILI</c>. Neither <c>TB_ACCOUNT_LINK_LEVEL</c> nor
/// <c>TB_TAFSIL_LINK_TAFSILGROUP</c> gets its own write repository or Command — both are
/// section-2 embedded children per <c>docs/tamin-core-entity-reference.md</c>, never independent
/// aggregates. There is no write side here at all: this is a read-only phase.
/// </summary>
public interface ITafsiliLookupReadRepository
{
    /// <summary>
    /// Returns every "active" تفصیلی level configured for <paramref name="accountCodeId"/> — see
    /// <see cref="Accounting.Application.AccountCodes.Queries.GetTafsiliLevels.GetTafsiliLevelsQuery"/>
    /// XML doc for Rule A. Ordered by numeric <c>TB_LEVEL_TAFSIL.LEVEL_CODE</c> ascending, never
    /// more than 7 rows. Returns an empty list — never <see langword="null"/> — for an account
    /// with no configured levels or an unknown <paramref name="accountCodeId"/>.
    /// </summary>
    Task<IReadOnlyList<TafsiliLevelDto>> GetActiveLevelsAsync(
        Guid accountCodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of selectable تفصیلی items for one (<paramref name="accountCodeId"/>,
    /// <paramref name="levelId"/>) pair, scoped by Rule B (see
    /// <see cref="Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems.GetTafsiliLevelItemsQuery"/>
    /// XML doc) to <paramref name="callerVahedCode"/>, optionally filtered by
    /// <paramref name="search"/> (see that query's <c>Search</c> parameter doc for the exact
    /// digit-vs-name heuristic). The caller's <see cref="Accounting.Domain.ValueObjects.VahedCategory"/>
    /// used by Rule B is derived INSIDE this repository from <paramref name="callerVahedCode"/>
    /// (a further database read: <c>TB_VAHED_INFO → TB_VAHED_TYPE</c>) — it is not a parameter,
    /// so the handler never needs to know how that derivation works. Distinct by
    /// <c>TB_TAFSILI.ID</c> — the same تفصیلی row can be reachable through more than one
    /// تفصیلی گروه.
    /// </summary>
    Task<PagedResult<TafsiliLookupItemDto>> GetSelectableItemsAsync(
        Guid accountCodeId,
        Guid levelId,
        string? search,
        string callerVahedCode,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
