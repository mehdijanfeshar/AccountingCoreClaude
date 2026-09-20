using System.Linq.Expressions;
using Accounting.Application.AttribForAccountCodes.Queries;
using Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;
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
        // Projected through the required ACCOUNTCODE navigation, so EF emits a single JOIN rather
        // than a second round trip per row. Deliberately NOT ".Include(a => a.ACCOUNTCODE)": that
        // would materialize the whole account entity; this stays a column-level projection.
        a.ACCOUNTCODE.ACCCODE,
        a.ACCOUNTCODE.ACCCODENAME,
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
        AttribForAccountCodeFilter? filter = null,
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

        if (filter is not null)
        {
            // Each filter is opt-in: a null/blank value means "do not narrow", never "match
            // nothing". Note this is the opposite of the VAHEDCODE guard above — narrowing
            // filters are safe to skip, a security scope is not.
            if (!string.IsNullOrWhiteSpace(filter.Year))
            {
                query = query.Where(a => a.YEAR == filter.Year);
            }

            if (filter.AttribSum is not null)
            {
                query = query.Where(a => a.ATTRIBSUM == filter.AttribSum);
            }

            if (filter.Flag is not null)
            {
                query = query.Where(a => a.FLAG == filter.Flag);
            }

            // Plain lexicographic range, deliberately: a معین ACCCODE is always exactly 6 digits
            // (see AttribForAccountCodeFilter XML doc), so unlike the voucher list's DOC_NUM range
            // no length comparison is needed. Rows whose account is a group (2 digits) or kol
            // (4 digits) simply fall outside any 6-digit bound, which is the desired behaviour for
            // a list that is about معین accounts.
            if (!string.IsNullOrWhiteSpace(filter.MoinCodeFrom))
            {
                query = query.Where(a =>
                    a.ACCOUNTCODE.ACCCODE != null
                    && string.Compare(a.ACCOUNTCODE.ACCCODE, filter.MoinCodeFrom) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(filter.MoinCodeTo))
            {
                query = query.Where(a =>
                    a.ACCOUNTCODE.ACCCODE != null
                    && string.Compare(a.ACCOUNTCODE.ACCCODE, filter.MoinCodeTo) <= 0);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Ordered by the معین code first: this list is "حساب‌های شناسه‌دار", so that is the
            // column a user scans, and it makes the from/to range filter above read naturally.
            // VAHEDCODE is no longer part of the ordering because every row in this result already
            // shares the caller's own unit (see the scope filter above), so it could only ever
            // have been a constant. YEAR then ID remain, the latter as a pure tie-breaker for
            // stable paging — ACCCODE is unique (UK_ACCOUNTCODE) but the attrib row is not.
            .OrderBy(a => a.ACCOUNTCODE.ACCCODE)
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
