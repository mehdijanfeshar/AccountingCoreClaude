using System.Linq.Expressions;
using Accounting.Application.CheckBooks.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ICheckBookReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class CheckBookReadRepository : ICheckBookReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_CHECKBOOK, CheckBookDto>> ToDto = c => new CheckBookDto(
        c.ID,
        c.ACCOUNT_ID,
        c.CHECKBOOK_TITLE,
        c.CHECKBOOK_DATE,
        c.FROMCHECKNUMBER,
        c.TOCHECKNUMBER,
        c.CHECKTYPE_ID,
        c.VAHEDCODE,
        c.CHECKBOOK_TYPE,
        c.SERIAL,
        c.CREATEDDATE,
        c.UPDATEDDATE,
        c.ADDUSERID,
        c.CHANGEUSERID,
        c.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public CheckBookReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<CheckBookDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // ISDELETED is non-nullable bool on this table, so a simple != true (equivalently
        // == false) comparison is sufficient — no NULL-handling ambiguity, unlike TB_ACCOUNT.
        //
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That exact conditional pattern is precisely the IDOR hole CLAUDE.md risk #1
        // describes: it lets a caller with no usable unit scope see every unit's rows instead of
        // none. VahedScopeBehavior guarantees vahedCode is always a real, non-empty value here,
        // so no such guard is needed — and adding one back would silently reopen the hole for any
        // future caller path that manages to reach this method with an empty string.
        //
        // Also deliberately exact-equality only (never "|| c.VAHEDCODE == null"): rows with
        // VAHEDCODE IS NULL are fail-closed — invisible to every caller, not just callers outside
        // the row's unit — per explicit project-owner decision.
        var query = _dbContext.TB_CHECKBOOKs
            .AsNoTracking()
            .Where(c => c.ISDELETED != true && c.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // FROMCHECKNUMBER is required/non-null but not unique on its own (UK_CHECKBOOK is a
            // composite over ACCOUNT_ID+FROMCHECKNUMBER+TOCHECKNUMBER+VAHEDCODE), so ID is a
            // tie-breaker for stable paging.
            .OrderBy(c => c.FROMCHECKNUMBER)
            .ThenBy(c => c.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<CheckBookDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<CheckBookDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on CheckBookDto.IsDeleted.
        return _dbContext.TB_CHECKBOOKs
            .AsNoTracking()
            .Where(c => c.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
