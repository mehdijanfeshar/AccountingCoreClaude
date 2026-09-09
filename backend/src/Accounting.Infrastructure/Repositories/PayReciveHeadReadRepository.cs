using System.Linq.Expressions;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PayReciveHeads.Queries;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPayReciveHeadReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class PayReciveHeadReadRepository : IPayReciveHeadReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_PAYRECIVHEAD, PayReciveHeadDto>> ToDto = e => new PayReciveHeadDto(
        e.ID,
        e.PAYRECIVCODE,
        e.PAYRECIVDATE,
        e.PAYRECIVDESCRIPTION,
        e.PAYRECIVTYPE,
        e.VAHEDCODE,
        e.YEAR,
        e.VOUCHERSHEAD_ID,
        e.CREATEDDATE,
        e.UPDATEDDATE,
        e.ADDUSERID,
        e.CHANGEUSERID,
        e.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public PayReciveHeadReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PayReciveHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is a non-nullable bool on this table, so the simple
        // == false comparison is both correct and complete — there is no NULL branch to handle
        // (contrast TB_TMP_VOUCHERHEAD, TB_ELAMHEAD and TB_RABET, where ISDELETED is bool?).
        //
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That exact conditional pattern is precisely the IDOR hole CLAUDE.md risk #1
        // describes: it lets a caller with no usable unit scope see every unit's rows instead of
        // none. VahedScopeBehavior guarantees vahedCode is always a real, non-empty value here,
        // so no such guard is needed — and adding one back would silently reopen the hole for any
        // future caller path that manages to reach this method with an empty string.
        //
        // Also deliberately exact-equality only: rows are matched with exact equality against
        // VAHEDCODE (non-nullable string on this table).
        var query = _dbContext.TB_PAYRECIVHEADs
            .AsNoTracking()
            .Where(e => e.ISDELETED == false && e.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // PAYRECIVCODE is NOT NULL but NOT unique — TB_PAYRECIVHEAD carries no UNIQUE
            // constraint at all — so ID is a pure tie-breaker to keep paging stable.
            .OrderBy(e => e.PAYRECIVCODE)
            .ThenBy(e => e.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<PayReciveHeadDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<PayReciveHeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on PayReciveHeadDto.IsDeleted.
        return _dbContext.TB_PAYRECIVHEADs
            .AsNoTracking()
            .Where(e => e.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
