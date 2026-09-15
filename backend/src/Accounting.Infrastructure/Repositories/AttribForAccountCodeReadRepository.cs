using System.Linq.Expressions;
using Accounting.Application.AttribForAccountCodes.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAttribForAccountCodeReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports read
/// from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class AttribForAccountCodeReadRepository : IAttribForAccountCodeReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate it
    /// into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_ATTRIBFORACCOUNTCODE, AttribForAccountCodeDto>> ToDto = a => new AttribForAccountCodeDto(
        a.ID,
        a.ACCOUNTCODE_ID,
        a.ATTRIBBOXNO,
        a.FLAG,
        a.LENATR,
        a.ATTRIBSUM,
        a.CONTROLID,
        a.VAHEDCODE,
        a.YEAR,
        a.CREATEDDATE,
        a.UPDATEDDATE,
        a.ADDUSERID,
        a.CHANGEUSERID,
        a.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public AttribForAccountCodeReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AttribForAccountCodeDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That exact conditional pattern is precisely the IDOR hole CLAUDE.md risk #1
        // describes: it lets a caller with no usable unit scope see every unit's rows instead of
        // none. VahedScopeBehavior guarantees vahedCode is always a real, non-empty value here, so
        // no such guard is needed — and adding one back would silently reopen the hole for any
        // future caller path that manages to reach this method with an empty string.
        //
        // Also deliberately exact-equality only (never "|| a.VAHEDCODE == null"): rows with
        // VAHEDCODE IS NULL are fail-closed — invisible to every caller, not just callers outside
        // the row's unit — per explicit project-owner decision.
        var query = _dbContext.TB_ATTRIBFORACCOUNTCODEs
            .AsNoTracking()
            .Where(a => a.ISDELETED != true && a.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // VAHEDCODE/YEAR participate in AK_AK_ATTRIBFORMAINCO_ATTRIBFO alongside
            // ACCOUNTCODE_ID but neither is itself unique, so rows are ordered by VAHEDCODE then
            // YEAR (both string columns) with ID as a pure tie-breaker for stable paging.
            .OrderBy(a => a.VAHEDCODE)
            .ThenBy(a => a.YEAR)
            .ThenBy(a => a.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<AttribForAccountCodeDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<AttribForAccountCodeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on AttribForAccountCodeDto.IsDeleted.
        return _dbContext.TB_ATTRIBFORACCOUNTCODEs
            .AsNoTracking()
            .Where(a => a.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
