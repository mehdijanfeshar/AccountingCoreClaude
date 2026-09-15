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
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That exact conditional pattern is precisely the IDOR hole CLAUDE.md risk #1
        // describes: it lets a caller with no usable unit scope see every unit's rows instead of
        // none. VahedScopeBehavior guarantees vahedCode is always a real, non-empty value here,
        // so no such guard is needed — and adding one back would silently reopen the hole for any
        // future caller path that manages to reach this method with an empty string.
        //
        // Also deliberately exact-equality only: rows are matched with exact equality against
        // VAHEDCODE (non-nullable string on this table).
        var query = _dbContext.TB_IDENTITYGROUPs
            .AsNoTracking()
            .Where(g => g.ISDELETED != true && g.VAHEDCODE == vahedCode);

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
