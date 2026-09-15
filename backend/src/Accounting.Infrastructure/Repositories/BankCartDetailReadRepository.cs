using System.Linq.Expressions;
using Accounting.Application.BankCartDetails.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IBankCartDetailReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class BankCartDetailReadRepository : IBankCartDetailReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_BANKCARTDETAIL, BankCartDetailDto>> ToDto = b => new BankCartDetailDto(
        b.ID,
        b.RECEIP_ID,
        b.CHECK_ID,
        b.BANK_ID,
        b.BRANCH_ID,
        b.ACCOUNTNUMBER,
        b.MONTH,
        b.CHEQNO,
        b.RECIVDATE,
        b.CHECKRECEIPTTYPE,
        b.DEBTOR,
        b.CREDITOR,
        b.VAHEDCODE,
        b.YEAR,
        b.CHECK_INCORRENT_ID,
        b.CREATEDDATE,
        b.UPDATEDDATE,
        b.ADDUSERID,
        b.CHANGEUSERID,
        b.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public BankCartDetailReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<BankCartDetailDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        //
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That exact conditional pattern is precisely the IDOR hole CLAUDE.md risk #1
        // describes: it lets a caller with no usable unit scope see every unit's rows instead of
        // none. VahedScopeBehavior guarantees vahedCode is always a real, non-empty value here, so
        // no such guard is needed — and adding one back would silently reopen the hole for any
        // future caller path that manages to reach this method with an empty string.
        //
        // Also deliberately exact-equality only (never "|| b.VAHEDCODE == null"): rows with
        // VAHEDCODE IS NULL are fail-closed — invisible to every caller, not just callers outside
        // the row's unit — per explicit project-owner decision.
        var query = _dbContext.TB_BANKCARTDETAILs
            .AsNoTracking()
            .Where(b => b.ISDELETED != true && b.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // YEAR and MONTH are both nullable, so Oracle sorts NULLs last; ID is a pure
            // tie-breaker for stable paging.
            .OrderBy(b => b.YEAR)
            .ThenBy(b => b.MONTH)
            .ThenBy(b => b.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<BankCartDetailDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<BankCartDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on BankCartDetailDto.IsDeleted.
        return _dbContext.TB_BANKCARTDETAILs
            .AsNoTracking()
            .Where(b => b.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
