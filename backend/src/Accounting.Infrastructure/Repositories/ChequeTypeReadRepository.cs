using System.Linq.Expressions;
using Accounting.Application.ChequeTypes.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IChequeTypeReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class ChequeTypeReadRepository : IChequeTypeReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_CHECK_TYPE, ChequeTypeDto>> ToDto = c => new ChequeTypeDto(
        c.ID,
        c.CHEQUE_TYPE_TITLE,
        c.CHEQUE_WIDTH,
        c.CHEQUE_HEIGHT,
        c.CHEQUE_IMAGE,
        c.CHEQUE_ADATE_FONT,
        c.CHEQUE_ADATE_LEFT,
        c.CHEQUE_ADATE_TOP,
        c.CHEQUE_ADATE_WIDTH,
        c.CHEQUE_NDATE_FONT,
        c.CHEQUE_NDATE_LEFT,
        c.CHEQUE_NDATE_TOP,
        c.CHEQUE_NDATE_WIDTH,
        c.CHEQUE_AAMOUNT_FONT,
        c.CHEQUE_AAMOUNT_LEFT,
        c.CHEQUE_AAMOUNT_TOP,
        c.CHEQUE_AAMOUNT_WIDTH,
        c.CHEQUE_LAMOUNT_FONT,
        c.CHEQUE_LAMOUNT_LEFT,
        c.CHEQUE_LAMOUNT_TOP,
        c.CHEQUE_LAMOUNT_WIDTH,
        c.CHEQUE_NAMOUNT_FONT,
        c.CHEQUE_NAMOUNT_LEFT,
        c.CHEQUE_NAMOUNT_TOP,
        c.CHEQUE_NAMOUNT_WIDTH,
        c.CHEQUE_DESCRIBE1_FONT,
        c.CHEQUE_DESCRIBE1_LEFT,
        c.CHEQUE_DESCRIBE1_TOP,
        c.CHEQUE_DESCRIBE1_WIDTH,
        c.CHEQUE_DESCRIBE2_FONT,
        c.CHEQUE_DESCRIBE2_LEFT,
        c.CHEQUE_DESCRIBE2_TOP,
        c.CHEQUE_DESCRIBE2_WIDTH,
        c.CHEQUE_BREAKLINE_FONT,
        c.CHEQUE_BREAKLINE_LEFT,
        c.CHEQUE_BREAKLINE_TOP,
        c.CHEQUE_BREAKLINE_WIDTH,
        c.PRINTER_MARGINE_TOP,
        c.PRINTER_MARGINE_LEFT,
        c.PRINTER_TYPE,
        c.YEAR,
        c.VAHEDCODE,
        c.CREATEDDATE,
        c.UPDATEDDATE,
        c.ADDUSERID,
        c.CHANGEUSERID,
        c.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public ChequeTypeReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ChequeTypeDto>> GetPagedAsync(
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
        // Also deliberately exact-equality only (never "|| c.VAHEDCODE == null"): rows with
        // VAHEDCODE IS NULL are fail-closed — invisible to every caller, not just callers outside
        // the row's unit — per explicit project-owner decision.
        var query = _dbContext.TB_CHECK_TYPEs
            .AsNoTracking()
            .Where(c => c.ISDELETED != true && c.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // No stable natural business key exists (CHEQUE_TYPE_TITLE is optional and
            // unconstrained), so rows are ordered by creation time with ID as a pure
            // tie-breaker for stable paging.
            .OrderBy(c => c.CREATEDDATE)
            .ThenBy(c => c.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ChequeTypeDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<ChequeTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on ChequeTypeDto.IsDeleted.
        return _dbContext.TB_CHECK_TYPEs
            .AsNoTracking()
            .Where(c => c.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
