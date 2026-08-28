using System.Linq.Expressions;
using Accounting.Application.VahedInfos.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVahedInfoReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
///
/// Neither query below applies an <c>ISDELETED</c> filter: <c>TB_VAHED_INFO</c> has no such
/// column at all.
/// </summary>
public sealed class VahedInfoReadRepository : IVahedInfoReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_VAHED_INFO, VahedInfoDto>> ToDto = v => new VahedInfoDto(
        v.ID,
        v.VAHEDCODE,
        v.VAHEDNAME,
        v.CITY_ID,
        v.VAHEDTYPE_ID,
        v.PARENT_ID);

    private readonly LegacyDbContext _dbContext;

    public VahedInfoReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<VahedInfoDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here — the column does not exist on this table.
        var query = _dbContext.TB_VAHED_INFOs.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // VAHEDCODE is required/non-null, so this sort is stable on its own for equal
            // values only via the ID tie-breaker below (needed for stable paging).
            .OrderBy(v => v.VAHEDCODE)
            .ThenBy(v => v.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<VahedInfoDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<VahedInfoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
