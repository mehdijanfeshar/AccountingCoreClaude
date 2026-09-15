using System.Linq.Expressions;
using Accounting.Application.Rabets.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IRabetReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class RabetReadRepository : IRabetReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_RABET, RabetDto>> ToDto = r => new RabetDto(
        r.ID,
        r.RABETTYPE_ID,
        r.ACCOUNTCODE_ID,
        r.CREATEDDATE,
        r.UPDATEDDATE,
        r.ADDUSERID,
        r.CHANGEUSERID,
        r.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public RabetReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<RabetDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        var query = _dbContext.TB_RABETs
            .AsNoTracking()
            .Where(r => r.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // No meaningful business/text column exists on this table to sort by (both FKs are
            // opaque Guids), so CREATEDDATE is used as the primary sort key. CREATEDDATE is
            // nullable, so rows with a NULL value sort last under Oracle's default NULLS LAST
            // for ascending order. ID is a pure tie-breaker so paging stays stable across
            // identical/NULL CreatedDate values.
            .OrderBy(r => r.CREATEDDATE)
            .ThenBy(r => r.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<RabetDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<RabetDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on RabetDto.IsDeleted.
        return _dbContext.TB_RABETs
            .AsNoTracking()
            .Where(r => r.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
