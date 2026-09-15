using System.Linq.Expressions;
using Accounting.Application.BillLogs.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IBillLogReadRepository"/>. Reads directly from
/// <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> — same narrow exception to the
/// "reports read from a Read Model" rule as <c>AccountCodeReadRepository</c>.
/// </summary>
public sealed class BillLogReadRepository : IBillLogReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/>, EF Core translates it into a column-level SQL projection.
    /// </summary>
    private static readonly Expression<Func<TB_BILL_LOG, BillLogDto>> ToDto = b => new BillLogDto(
        b.ID,
        b.LOG_DESC,
        b.LOG_DATE,
        b.VAHEDCODE,
        b.YEAR,
        b.CREATEDDATE,
        b.UPDATEDDATE,
        b.ADDUSERID,
        b.CHANGEUSERID,
        b.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public BillLogReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<BillLogDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That exact conditional pattern is precisely the IDOR hole CLAUDE.md risk #1
        // describes: it lets a caller with no usable unit scope see every unit's rows instead of
        // none. VahedScopeBehavior guarantees vahedCode is always a real, non-empty value here, so
        // no such guard is needed — and adding one back would silently reopen the hole for any
        // future caller path that manages to reach this method with an empty string.
        //
        // Also deliberately exact-equality only (never "|| b.VAHEDCODE == null"): rows with
        // VAHEDCODE IS NULL are fail-closed — invisible to every caller, not just callers outside
        // the row's unit — per explicit project-owner decision. (TB_BILL_LOG.VAHEDCODE is
        // non-nullable in this schema, so this is largely defense-in-depth here, but kept
        // consistent with every other *ReadRepository in this project.)
        var query = _dbContext.TB_BILL_LOGs
            .AsNoTracking()
            .Where(b => b.ISDELETED != true && b.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // No stable natural business key exists (VAHEDCODE/YEAR are not unique), so rows
            // are ordered by creation time with ID as a pure tie-breaker for stable paging.
            .OrderBy(b => b.CREATEDDATE)
            .ThenBy(b => b.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<BillLogDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<BillLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_BILL_LOGs
            .AsNoTracking()
            .Where(b => b.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
