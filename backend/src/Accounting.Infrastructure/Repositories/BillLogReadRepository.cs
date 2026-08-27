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
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_BILL_LOGs
            .AsNoTracking()
            .Where(b => b.ISDELETED != true);

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
