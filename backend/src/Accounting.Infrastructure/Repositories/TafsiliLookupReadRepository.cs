using System.Linq.Expressions;
using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ITafsiliLookupReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports read
/// from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
///
/// ⚠️ Deliberately does NOT query the Oracle view <c>VWTAFSILILIST</c> that the reference project
/// uses for this exact lookup: its internal definition is unknown to us, so every join below is
/// written out explicitly in LINQ over the four already-mapped tables instead, so the semantics
/// stay auditable. Follow-up: consider scaffolding/using that view once its definition is known.
///
/// ⚠️ Performance follow-up (found in code review, not acted on here — no schema changes are in
/// scope for this phase): <c>TB_TAFSIL_LINK_TAFSILGROUP</c> has NO index at all on
/// <c>TAFSILGROUP_ID</c> or <c>TAFSIL_ID</c> (verified against <c>LegacyDbContext</c> — its only
/// index is the implicit one backing its primary key). Both the atg/tlg join in
/// <see cref="GetSelectableItemsAsync"/> and the <c>Contains</c> subquery against
/// <c>TAFSIL_ID</c> will therefore full-scan this table on every call. Flag for
/// <c>performance-reviewer</c> if/when this table grows large enough to matter.
/// </summary>
public sealed class TafsiliLookupReadRepository : ITafsiliLookupReadRepository
{
    /// <summary>
    /// Projection expression for <see cref="GetSelectableItemsAsync"/>. <see cref="TafsiliLookupItemDto.Label"/>
    /// is composed here with plain string concatenation (<c>??</c> + <c>+</c>), which EF Core
    /// translates into a SQL <c>COALESCE</c>/concatenation expression — never evaluated
    /// client-side, so paging still happens at the database.
    /// </summary>
    private static readonly Expression<Func<TB_TAFSILI, TafsiliLookupItemDto>> ToDto = t => new TafsiliLookupItemDto(
        t.ID,
        t.TAFSILI_CODE,
        t.TAFSILI_NAME,
        (t.TAFSILI_CODE ?? string.Empty) + " - " + (t.TAFSILI_NAME ?? string.Empty));

    private readonly LegacyDbContext _dbContext;

    public TafsiliLookupReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TafsiliLevelDto>> GetActiveLevelsAsync(
        Guid accountCodeId,
        CancellationToken cancellationToken = default)
    {
        // ISDELETED is non-nullable bool on both TB_ACCOUNT_LINK_LEVEL and TB_LEVEL_TAFSIL, so
        // "== false" (not "!= true") is the correct/only "not deleted" predicate for either.
        var rows = await _dbContext.TB_ACCOUNT_LINK_LEVELs
            .AsNoTracking()
            .Where(l => l.ACCOUNT_ID == accountCodeId && l.ISDELETED == false && l.LEVEL.ISDELETED == false)
            .Select(l => new { l.LEVEL.ID, l.LEVEL.LEVEL_CODE, l.LEVEL.LEVEL_NAME })
            .ToListAsync(cancellationToken);

        // LEVEL_CODE is a plain string column with no CHECK constraint enforcing "parsable
        // integer". A row that fails to parse is skipped — not surfaced as a crash, and not
        // surfaced with some default/garbage Code value that would look like a real level to the
        // frontend (Code is load-bearing there: 1-3 render inline, 4-7 behind a modal). Parsing
        // has to happen client-side (int.TryParse cannot be translated to SQL), so the query
        // above is materialized first and this loop runs in memory.
        //
        // Deduplicated by TB_LEVEL_TAFSIL.ID: unlike TB_ACCOUNT_LINK_TAFSILGROUP (which has a real
        // UK_ACCOUNTLINKTAFSILGROUP unique constraint), TB_ACCOUNT_LINK_LEVEL has NO unique
        // constraint on (ACCOUNT_ID, LEVEL_ID) — two non-deleted link rows to the same level for
        // the same account are a data anomaly the schema does not prevent. Without this guard such
        // an anomaly would silently surface as a duplicate entry in the array the frontend renders
        // levels 1-3/4-7 from.
        var seenLevelIds = new HashSet<Guid>();
        var levels = new List<TafsiliLevelDto>(rows.Count);
        foreach (var row in rows)
        {
            if (int.TryParse(row.LEVEL_CODE, out var code) && seenLevelIds.Add(row.ID))
            {
                levels.Add(new TafsiliLevelDto(row.ID, code, row.LEVEL_NAME, IsRequired: true));
            }
        }

        return levels
            .OrderBy(l => l.Code)
            .ToList();
    }

    public async Task<PagedResult<TafsiliLookupItemDto>> GetSelectableItemsAsync(
        Guid accountCodeId,
        Guid levelId,
        string? search,
        string callerVahedCode,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var callerCategory = await DeriveCallerCategoryAsync(callerVahedCode, cancellationToken);
        var callerCategoryValue = (short)callerCategory;
        var allCategoryValue = (short)VahedCategory.All;

        // Step 1: which TAFSIL_IDs are reachable for this (account, level) at all, and — Rule B —
        // visible to this caller? This query never touches TB_TAFSILI, so it cannot multiply rows
        // even though the same TAFSIL_ID can appear in more than one تفصیلی گروه.
        var visibleTafsilIds =
            from accountLink in _dbContext.TB_ACCOUNT_LINK_TAFSILGROUPs.AsNoTracking()
            where accountLink.ACCOUNT_ID == accountCodeId
                && accountLink.LEVEL_ID == levelId
                && accountLink.ISDELETED == false
            join groupLink in _dbContext.TB_TAFSIL_LINK_TAFSILGROUPs.AsNoTracking()
                on accountLink.TAFSILGROUP_ID equals groupLink.TAFSILGROUP_ID
            where groupLink.ISDELETED == false
                // Rule B, verbatim (see GetTafsiliLevelItemsQuery XML doc): visible iff the link's
                // own VAHEDCODE matches the caller, OR its VAHEDTYPE is the "All" wildcard, OR its
                // VAHEDTYPE equals the caller's own category. A NULL VAHEDTYPE is deliberately NOT
                // treated as a wildcard — only an exact VAHEDCODE match makes such a row visible.
                && (groupLink.VAHEDCODE == callerVahedCode
                    || groupLink.VAHEDTYPE == allCategoryValue
                    || groupLink.VAHEDTYPE == callerCategoryValue)
            select groupLink.TAFSIL_ID;

        // Step 2: TB_TAFSIL_LINK_TAFSILGROUP has NO navigation property to TB_TAFSILI (no FK on
        // TAFSIL_ID exists in the Legacy schema — a known gap, CLAUDE.md risk table). Filtering
        // TB_TAFSILI by "ID IN (...)" instead of joining OUT from the link table both works around
        // that missing navigation AND is exactly what keeps this query naturally distinct: each
        // TB_TAFSILI row is selected from its own table once, regardless of how many تفصیلی گروه
        // rows reference it.
        var tafsiliQuery = _dbContext.TB_TAFSILIs
            .AsNoTracking()
            .Where(t => t.ISDELETED != true)
            .Where(t => visibleTafsilIds.Contains(t.ID));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Heuristic copied verbatim from the reference project
            // (TbAccountLinkTafsilGroupRepository.cs:186-190) — surprising enough that a future
            // reader might otherwise "fix" it into an OR: a term containing any digit matches
            // TAFSILI_CODE only; a term with no digit matches TAFSILI_NAME only. Never both.
            var hasDigit = search.Any(char.IsDigit);
            tafsiliQuery = hasDigit
                ? tafsiliQuery.Where(t => t.TAFSILI_CODE != null && t.TAFSILI_CODE.Contains(search))
                : tafsiliQuery.Where(t => t.TAFSILI_NAME != null && t.TAFSILI_NAME.Contains(search));
        }

        var totalCount = await tafsiliQuery.CountAsync(cancellationToken);

        var items = await tafsiliQuery
            .OrderBy(t => t.TAFSILI_CODE)
            .ThenBy(t => t.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<TafsiliLookupItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    /// <summary>
    /// Derives the caller's <see cref="VahedCategory"/> from <paramref name="callerVahedCode"/>
    /// via <c>TB_VAHED_INFO → TB_VAHED_TYPE.TYPECODE</c>, mirroring the reference project's
    /// <c>VahedInfoRepository.GetTypeBimehDarmanAsync</c>.
    ///
    /// Deliberate divergence from the reference project: it would throw a
    /// <see cref="NullReferenceException"/> if <paramref name="callerVahedCode"/> does not match
    /// any <c>TB_VAHED_INFO</c> row. Here, a missing unit (or a <c>TYPECODE</c> that fails to
    /// parse as an integer) falls back to <see cref="VahedCategory.All"/> via
    /// <see cref="VahedCategoryMapper.FromTypeCode"/>'s own null-safe fallback — this endpoint
    /// must never crash on caller-scoped data it cannot fully resolve.
    /// </summary>
    private async Task<VahedCategory> DeriveCallerCategoryAsync(
        string callerVahedCode,
        CancellationToken cancellationToken)
    {
        var typeCode = await _dbContext.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.VAHEDCODE == callerVahedCode)
            .Select(v => v.VAHEDTYPE.TYPECODE)
            .FirstOrDefaultAsync(cancellationToken);

        return VahedCategoryMapper.FromTypeCode(typeCode);
    }
}
