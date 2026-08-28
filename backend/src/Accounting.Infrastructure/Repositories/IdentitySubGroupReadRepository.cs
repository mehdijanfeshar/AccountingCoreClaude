using System.Linq.Expressions;
using Accounting.Application.IdentitySubGroups.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IIdentitySubGroupReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class IdentitySubGroupReadRepository : IIdentitySubGroupReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_IDENTITYSUBGRP, IdentitySubGroupDto>> ToDto = s => new IdentitySubGroupDto(
        s.ID,
        s.IDENTYGROUPS_ID,
        s.SUBGRPS_DESC,
        s.SUBGRPS_LEN,
        s.SUMFLAG,
        s.FIXED,
        s.SUBGRPS_TYPE,
        s.VAHEDCODE,
        s.YEAR,
        s.IDENTYSUBGROUPS_CODE,
        s.CREATEDDATE,
        s.UPDATEDDATE,
        s.ADDUSERID,
        s.CHANGEUSERID,
        s.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public IdentitySubGroupReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<IdentitySubGroupDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_IDENTITYSUBGRPs
            .AsNoTracking()
            .Where(s => s.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // No stable natural business key exists that is guaranteed non-null on its own
            // (IDENTYSUBGROUPS_CODE is optional), so rows are ordered by creation time with ID
            // as a pure tie-breaker for stable paging.
            .OrderBy(s => s.CREATEDDATE)
            .ThenBy(s => s.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<IdentitySubGroupDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<IdentitySubGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on IdentitySubGroupDto.IsDeleted.
        return _dbContext.TB_IDENTITYSUBGRPs
            .AsNoTracking()
            .Where(s => s.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
