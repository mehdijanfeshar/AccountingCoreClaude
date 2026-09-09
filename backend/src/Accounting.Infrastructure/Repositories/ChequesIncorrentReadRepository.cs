using System.Linq.Expressions;
using Accounting.Application.ChequesIncorrents.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IChequesIncorrentReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class ChequesIncorrentReadRepository : IChequesIncorrentReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_CHEQUES_INCORRENT, ChequesIncorrentDto>> ToDto = c => new ChequesIncorrentDto(
        c.ID,
        c.CHECK_ID,
        c.DOC_NUM,
        c.DOC_DATE,
        c.CHEQ_NO,
        c.CHEQ_DATE,
        c.PAPER_DESC,
        c.PAYTO,
        c.RECIVDATE,
        c.ACCOUNTNUMBER,
        c.CREDITOR,
        c.VAHEDCODE,
        c.YEAR,
        c.CREATEDDATE,
        c.UPDATEDDATE,
        c.ADDUSERID,
        c.CHANGEUSERID,
        c.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public ChequesIncorrentReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ChequesIncorrentDto>> GetPagedAsync(
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
        var query = _dbContext.TB_CHEQUES_INCORRENTs
            .AsNoTracking()
            .Where(c => c.ISDELETED != true && c.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // No UNIQUE constraint exists on this table at all, so CHEQ_NO is not guaranteed
            // unique on its own — ID is a tie-breaker for stable paging.
            .OrderBy(c => c.CHEQ_NO)
            .ThenBy(c => c.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ChequesIncorrentDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<ChequesIncorrentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on ChequesIncorrentDto.IsDeleted.
        return _dbContext.TB_CHEQUES_INCORRENTs
            .AsNoTracking()
            .Where(c => c.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
