using System.Linq.Expressions;
using Accounting.Application.WhiteLists.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IWhiteListReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class WhiteListReadRepository : IWhiteListReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_WHITELIST, WhiteListDto>> ToDto = w => new WhiteListDto(
        w.ID,
        w.ACCOUNTCODE_ID,
        w.VAHEDTYPE_ID,
        w.VAHEDINFO_ID,
        w.CREATEDDATE,
        w.UPDATEDDATE,
        w.ADDUSERID,
        w.CHANGEUSERID,
        w.ISDELETED,
        w.FROMAUTHORIZEDDATE,
        w.TOAUTHORIZEDDATE,
        w.FROMLIMITATIONDATE,
        w.TOLIMITATIONDATE);

    private readonly LegacyDbContext _dbContext;

    public WhiteListReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<WhiteListDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        var query = _dbContext.TB_WHITELISTs
            .AsNoTracking()
            .Where(w => w.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // CREATEDDATE is non-nullable on this table, so this is a clean chronological sort.
            // ID is a pure tie-breaker so paging stays stable across identical CreatedDate values.
            .OrderBy(w => w.CREATEDDATE)
            .ThenBy(w => w.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<WhiteListDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<WhiteListDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on WhiteListDto.IsDeleted.
        return _dbContext.TB_WHITELISTs
            .AsNoTracking()
            .Where(w => w.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
