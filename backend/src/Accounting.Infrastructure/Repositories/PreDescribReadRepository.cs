using System.Linq.Expressions;
using Accounting.Application.PreDescribs.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPreDescribReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
///
/// Neither query below applies an <c>ISDELETED</c> filter: <c>TB_PREDESCRIB</c> has no such
/// column at all (unlike every other entity in this project so far).
/// </summary>
public sealed class PreDescribReadRepository : IPreDescribReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_PREDESCRIB, PreDescribDto>> ToDto = p => new PreDescribDto(
        p.ID,
        p.ACCOUNTID,
        p.DESCRIP,
        p.ADDUSERID,
        p.VAHEDCODE,
        p.FLAGVOUCHER);

    private readonly LegacyDbContext _dbContext;

    public PreDescribReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PreDescribDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here — the column does not exist on this table.
        var query = _dbContext.TB_PREDESCRIBs.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // DESCRIP is nullable, so rows with a NULL description sort last under Oracle's
            // default NULLS LAST for ascending order. ID is a pure tie-breaker so paging stays
            // stable across identical/NULL DESCRIP values.
            .OrderBy(p => p.DESCRIP)
            .ThenBy(p => p.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<PreDescribDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<PreDescribDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_PREDESCRIBs
            .AsNoTracking()
            .Where(p => p.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
