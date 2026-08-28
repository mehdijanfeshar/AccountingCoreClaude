using System.Linq.Expressions;
using Accounting.Application.IdentityGroups.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IIdentityGroupReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class IdentityGroupReadRepository : IIdentityGroupReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_IDENTITYGROUP, IdentityGroupDto>> ToDto = g => new IdentityGroupDto(
        g.ID,
        g.IDENTITYGROUPS_DESC,
        g.IDENTITYGROUPS_CODE,
        g.VAHEDCODE,
        g.TAFSILI_ID,
        g.CREATEDDATE,
        g.UPDATEDDATE,
        g.ADDUSERID,
        g.CHANGEUSERID,
        g.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public IdentityGroupReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<IdentityGroupDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_IDENTITYGROUPs
            .AsNoTracking()
            .Where(g => g.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // No stable natural business key exists that is guaranteed non-null/unique on its
            // own (IDENTITYGROUPS_CODE is optional), so rows are ordered by creation time with
            // ID as a pure tie-breaker for stable paging.
            .OrderBy(g => g.CREATEDDATE)
            .ThenBy(g => g.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<IdentityGroupDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<IdentityGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on IdentityGroupDto.IsDeleted.
        return _dbContext.TB_IDENTITYGROUPs
            .AsNoTracking()
            .Where(g => g.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
