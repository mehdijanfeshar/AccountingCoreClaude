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
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity. Deliberately
    /// excludes <c>CHECKFILE</c> (the BLOB column) — see <see cref="WorkShopDto"/> XML doc.
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
        w.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public WorkShopReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
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

        return new PagedResult<WorkShopDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<WorkShopDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on WorkShopDto.IsDeleted.
        return _dbContext.TB_WORKSHOPs
            .AsNoTracking()
            .Where(w => w.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
