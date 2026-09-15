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
/// column at all (unlike every other entity in this project so far). <see cref="GetPagedAsync"/>
/// DOES apply the standard <c>VAHEDCODE</c> unit-scope filter.
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
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here — the column does not exist on this table.
        //
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That exact conditional pattern is precisely the IDOR hole CLAUDE.md risk #1
        // describes: it lets a caller with no usable unit scope see every unit's rows instead of
        // none. VahedScopeBehavior guarantees vahedCode is always a real, non-empty value here, so
        // no such guard is needed — and adding one back would silently reopen the hole for any
        // future caller path that manages to reach this method with an empty string.
        //
        // Also deliberately exact-equality only (never "|| p.VAHEDCODE == null"): rows with
        // VAHEDCODE IS NULL are fail-closed — invisible to every caller, not just callers outside
        // the row's unit — per explicit project-owner decision.
        var query = _dbContext.TB_PREDESCRIBs
            .AsNoTracking()
            .Where(p => p.VAHEDCODE == vahedCode);

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
