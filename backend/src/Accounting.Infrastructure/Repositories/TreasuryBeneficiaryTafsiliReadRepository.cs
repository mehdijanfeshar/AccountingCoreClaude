using System.Linq.Expressions;
using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ITreasuryBeneficiaryTafsiliReadRepository"/> —
/// اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹). Reads directly from <see cref="LegacyDbContext"/> with
/// <c>AsNoTracking()</c>, same deliberate exception as <see cref="TafsiliLookupReadRepository"/>.
/// Simpler than that repository's <c>GetSelectableItemsAsync</c>: scoped to ONE caller-resolved
/// group id, no Rule B (VahedCategory) visibility layer — that rule is specific to
/// <c>TB_ACCOUNT_LINK_TAFSILGROUP</c>'s multi-unit sharing, which does not apply here.
/// </summary>
public sealed class TreasuryBeneficiaryTafsiliReadRepository : ITreasuryBeneficiaryTafsiliReadRepository
{
    private static readonly Expression<Func<TB_TAFSILI, TafsiliLookupItemDto>> ToDto = t => new TafsiliLookupItemDto(
        t.ID,
        t.TAFSILI_CODE,
        t.TAFSILI_NAME,
        (t.TAFSILI_CODE ?? string.Empty) + " - " + (t.TAFSILI_NAME ?? string.Empty));

    private readonly LegacyDbContext _dbContext;

    public TreasuryBeneficiaryTafsiliReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsMemberOfGroupAsync(
        Guid tafsiliId, Guid tafsilGroupId, CancellationToken cancellationToken = default)
    {
        var count = await _dbContext.TB_TAFSIL_LINK_TAFSILGROUPs
            .AsNoTracking()
            .Where(l => l.TAFSIL_ID == tafsiliId && l.TAFSILGROUP_ID == tafsilGroupId && l.ISDELETED == false)
            .CountAsync(cancellationToken);

        return count > 0;
    }

    public async Task<PagedResult<TafsiliLookupItemDto>> GetPagedAsync(
        Guid tafsilGroupId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var memberTafsiliIds = _dbContext.TB_TAFSIL_LINK_TAFSILGROUPs
            .AsNoTracking()
            .Where(l => l.TAFSILGROUP_ID == tafsilGroupId && l.ISDELETED == false)
            .Select(l => l.TAFSIL_ID);

        // TB_TAFSIL_LINK_TAFSILGROUP has no navigation/FK to TB_TAFSILI (TAFSIL_ID column is a
        // known Legacy gap — CLAUDE.md risk table), so filtering "ID IN (...)" is both the
        // workaround AND what keeps this query naturally distinct by TB_TAFSILI.ID.
        var query = _dbContext.TB_TAFSILIs
            .AsNoTracking()
            .Where(t => t.ISDELETED != true)
            .Where(t => memberTafsiliIds.Contains(t.ID));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Same digit-vs-name heuristic as TafsiliLookupReadRepository.GetSelectableItemsAsync.
            var hasDigit = search.Any(char.IsDigit);
            query = hasDigit
                ? query.Where(t => t.TAFSILI_CODE != null && t.TAFSILI_CODE.Contains(search))
                : query.Where(t => t.TAFSILI_NAME != null && t.TAFSILI_NAME.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
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
}
