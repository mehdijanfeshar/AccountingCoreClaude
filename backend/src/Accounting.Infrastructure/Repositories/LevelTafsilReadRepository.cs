using System.Linq.Expressions;
using Accounting.Application.LevelTafsils.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ILevelTafsilReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports read
/// from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class LevelTafsilReadRepository : ILevelTafsilReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate it
    /// into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_LEVEL_TAFSIL, LevelTafsilDto>> ToDto = l => new LevelTafsilDto(
        l.ID,
        l.LEVEL_CODE,
        l.LEVEL_NAME,
        l.CREATEDDATE,
        l.UPDATEDDATE,
        l.ADDUSERID,
        l.CHANGEUSERID,
        l.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public LevelTafsilReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<LevelTafsilDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_LEVEL_TAFSILs
            .AsNoTracking()
            .Where(l => l.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Neither LEVEL_CODE nor LEVEL_NAME carries a UNIQUE constraint on this table, so
            // rows are ordered by LEVEL_CODE (the natural lookup key) with ID as a pure
            // tie-breaker for stable paging.
            .OrderBy(l => l.LEVEL_CODE)
            .ThenBy(l => l.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<LevelTafsilDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<LevelTafsilDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on LevelTafsilDto.IsDeleted.
        return _dbContext.TB_LEVEL_TAFSILs
            .AsNoTracking()
            .Where(l => l.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
