using System.Linq.Expressions;
using Accounting.Application.BankAccounts.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IBankAccountReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class BankAccountReadRepository : IBankAccountReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity. Deliberately
    /// excludes <c>CHECKFILE</c> (the BLOB column) — see <see cref="BankAccountDto"/> XML doc.
    /// </summary>
    private static readonly Expression<Func<TB_ACCOUNT, BankAccountDto>> ToDto = a => new BankAccountDto(
        a.ID,
        a.ACCOUNTNUMBER,
        a.ACCOUNTHOLDER,
        a.CARDNUMBER,
        a.SHEBANUMBER,
        a.FIRSTAMOUNT,
        a.BANK_ID,
        a.BRANCH_ID,
        a.ACCOUNTTYPE_ID,
        a.ACCOUNTCODE_ID,
        a.VAHEDCODE,
        a.ACCOUNTOPENINGDATE,
        a.CREATEDDATE,
        a.UPDATEDDATE,
        a.ADDUSERID,
        a.CHANGEUSERID,
        a.ISDELETED,
        // Projected inline (not Include'd) so the list/by-id queries stay single round-trips and
        // the DTO never carries EF-tracked entities. Soft-deleted links are filtered out here.
        a.TB_ACCOUNT_LINK_TAFSILIs
            .Where(l => l.ISDELETED == false)
            .Select(l => new BankAccountTafsiliLinkDto(l.TAFSILI_ID, l.LEVEL_ID))
            .ToList());

    private readonly LegacyDbContext _dbContext;

    public BankAccountReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<BankAccountDto>> GetPagedAsync(
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
        // Also deliberately exact-equality only (never "|| a.VAHEDCODE == null"): rows with
        // VAHEDCODE IS NULL are fail-closed — invisible to every caller, not just callers outside
        // the row's unit — per explicit project-owner decision.
        var query = _dbContext.TB_ACCOUNTs
            .AsNoTracking()
            .Where(a => a.ISDELETED != true && a.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // ACCOUNTNUMBER is required/non-null but not unique on its own (UK_ACCOUNT_ACCOUNTCODE
            // is a composite over ACCOUNTCODE_ID+VAHEDCODE), so ID is a tie-breaker for stable
            // paging.
            .OrderBy(a => a.ACCOUNTNUMBER)
            .ThenBy(a => a.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<BankAccountDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<BankAccountDto?> GetByIdAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // The owning unit is read first as a scalar so the access decision can tell "no such
        // row" (null, caller gets null, 404) from "another unit's row" (403). Projecting it
        // alongside the DTO in one query is not possible while ToDto stays a reusable
        // Expression, and this is a single-record edit-form fetch, not a hot path.
        var ownerVahedCode = await _dbContext.TB_ACCOUNTs
            .AsNoTracking()
            .Where(a => a.ID == id)
            .Select(a => a.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "BankAccount");

        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on BankAccountDto.IsDeleted.
        return await _dbContext.TB_ACCOUNTs
            .AsNoTracking()
            .Where(a => a.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
