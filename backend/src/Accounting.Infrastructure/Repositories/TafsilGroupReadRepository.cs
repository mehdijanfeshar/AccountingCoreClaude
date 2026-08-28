using System.Linq.Expressions;
using Accounting.Application.TafsilGroups.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ITafsilGroupReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports read
/// from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class TafsilGroupReadRepository : ITafsilGroupReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate it
    /// into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_TAFSIL_GROUP, TafsilGroupDto>> ToDto = t => new TafsilGroupDto(
        t.ID,
        t.TAFSILGROUP_CODE,
        t.TAFSILGROUP_NAME,
        t.PERSONTYPE,
        t.CREATEDDATE,
        t.UPDATEDDATE,
        t.ADDUSERID,
        t.CHANGEUSERID,
        t.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public TafsilGroupReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<TafsilGroupDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_TAFSIL_GROUPs
            .AsNoTracking()
            .Where(t => t.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // TAFSILGROUP_CODE participates in UK_TBTAFSILGROUP alongside ISDELETED but is not
            // itself unique, so rows are ordered by TAFSILGROUP_CODE (the natural lookup key)
            // with ID as a pure tie-breaker for stable paging.
            .OrderBy(t => t.TAFSILGROUP_CODE)
            .ThenBy(t => t.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<TafsilGroupDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<TafsilGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on TafsilGroupDto.IsDeleted.
        return _dbContext.TB_TAFSIL_GROUPs
            .AsNoTracking()
            .Where(t => t.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
