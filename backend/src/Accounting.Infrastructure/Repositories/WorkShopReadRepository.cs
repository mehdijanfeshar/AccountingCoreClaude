using System.Linq.Expressions;
using Accounting.Application.WorkShops.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IWorkShopReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class WorkShopReadRepository : IWorkShopReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public WorkShopReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity. Deliberately
    /// excludes <c>CHECKFILE</c> (the BLOB column) — see <see cref="WorkShopDto"/> XML doc.
    ///
    /// The تفصیلی links are left empty here and filled by <see cref="AttachLinksAsync"/>:
    /// <c>TB_WORKSHOP_LINK_TAFSILI</c> has no PK constraint in Oracle, and a correlated collection
    /// subquery inside the projection failed on it ("Unable to translate a collection subquery
    /// in a projection") while it was mapped keyless. Two flat queries avoid that shape entirely.
    /// </summary>
    private static readonly Expression<Func<TB_WORKSHOP, WorkShopDto>> ToDto = w => new WorkShopDto(
        w.ID,
        w.ACCOUNTCODE_ID,
        w.BRANCH_ID,
        w.WORKSHOPNAME,
        w.WORKSHOPCODE,
        w.VAHEDCODE,
        w.ISACTIVE,
        w.CREATEDDATE,
        w.UPDATEDDATE,
        w.ADDUSERID,
        w.CHANGEUSERID,
        w.ISDELETED,
        Array.Empty<WorkShopTafsiliLinkDto>());

    /// <summary>
    /// Loads the live تفصیلی links of the given workshops in one query and attaches them.
    /// </summary>
    private async Task<List<WorkShopDto>> AttachLinksAsync(
        List<WorkShopDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return items;

        var ids = items.Select(i => i.Id).ToList();
        var links = await _dbContext.TB_WORKSHOP_LINK_TAFSILIs
            .AsNoTracking()
            .Where(l => ids.Contains(l.WORKSHOP_ID) && l.ISDELETED == false)
            .Select(l => new { l.WORKSHOP_ID, l.TAFSILI_ID, l.LEVEL_ID })
            .ToListAsync(cancellationToken);

        var byWorkShop = links.ToLookup(l => l.WORKSHOP_ID);
        return items
            .Select(i => i with
            {
                TafsiliLinks = byWorkShop[i.Id]
                    .Select(l => new WorkShopTafsiliLinkDto(l.TAFSILI_ID, l.LEVEL_ID))
                    .ToList(),
            })
            .ToList();
    }

    public async Task<PagedResult<WorkShopDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        //
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That exact conditional pattern exists on VoucherHeadReadRepository.GetPagedAsync
        // and is precisely the IDOR hole CLAUDE.md risk #1 describes: it lets a caller with no
        // usable unit scope see every unit's rows instead of none. VahedScopeBehavior guarantees
        // vahedCode is always a real, non-empty value here, so no such guard is needed — and
        // adding one back would silently reopen the hole for any future caller path that manages
        // to reach this method with an empty string.
        //
        // Also deliberately exact-equality only (never "|| w.VAHEDCODE == null"): rows with
        // VAHEDCODE IS NULL are fail-closed — invisible to every caller, not just callers outside
        // the row's unit — per explicit project-owner decision.
        var query = _dbContext.TB_WORKSHOPs
            .AsNoTracking()
            .Where(w => w.ISDELETED != true && w.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // WORKSHOPCODE is required/non-null, so ID is a pure tie-breaker so paging stays
            // stable across identical WORKSHOPCODE values (WORKSHOPCODE alone is not unique —
            // UK_WORKSHOP is a composite over WORKSHOPCODE+ISACTIVE+VAHEDCODE).
            .OrderBy(w => w.WORKSHOPCODE)
            .ThenBy(w => w.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        items = await AttachLinksAsync(items, cancellationToken);

        return new PagedResult<WorkShopDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<WorkShopDto?> GetByIdAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // Two round trips, deliberately. The owning unit is read first as a scalar so the access
        // decision can distinguish "no such row" (null → caller gets null → 404) from "another
        // unit's row" (→ 403). Projecting VAHEDCODE alongside the DTO in one query is not possible
        // while ToDto stays a reusable Expression, and an extra primary-key lookup on a
        // single-record edit-form fetch is not a path worth complicating the projection for.
        var ownerVahedCode = await _dbContext.TB_WORKSHOPs
            .AsNoTracking()
            .Where(w => w.ID == id)
            .Select(w => w.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "WorkShop");

        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on WorkShopDto.IsDeleted.
        var dto = await _dbContext.TB_WORKSHOPs
            .AsNoTracking()
            .Where(w => w.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null ? null : (await AttachLinksAsync([dto], cancellationToken))[0];
    }
}
