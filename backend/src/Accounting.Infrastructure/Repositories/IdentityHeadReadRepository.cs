using System.Linq.Expressions;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.IdentityHeads.Queries;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IIdentityHeadReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports read
/// from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class IdentityHeadReadRepository : IIdentityHeadReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate it
    /// into a column-level SQL projection instead of loading the full entity.
    ///
    /// The fix items are projected as a correlated subquery inside the same statement, so a page
    /// of heads plus all their items is one round trip rather than one per row.
    /// </summary>
    private static readonly Expression<Func<TB_IDENTITYHEAD, IdentityHeadDto>> ToDto = h => new IdentityHeadDto(
        h.ID,
        h.IDENTITYGROUPS_ID,
        h.IDENTITYGROUPS.IDENTITYGROUPS_DESC,
        h.SERIAL,
        h.TB_IDENTITYFIXITEMs
            .Where(f => f.ISDELETED == false)
            .OrderBy(f => f.IDENTITYSUBGRPS.SUBGRPS_DESC)
            .Select(f => new IdentityHeadFixItemDto(
                f.ID,
                f.IDENTITYSUBGRPS_ID,
                f.IDENTITYSUBGRPS.SUBGRPS_DESC,
                f.FIXITEMS_VALUE))
            .ToList(),
        h.VAHEDCODE,
        h.YEAR,
        h.CREATEDDATE,
        h.UPDATEDDATE,
        h.ADDUSERID,
        h.CHANGEUSERID,
        h.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public IdentityHeadReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<IdentityHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        Guid? identityGroupId = null,
        string? year = null,
        CancellationToken cancellationToken = default)
    {
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That conditional is precisely the IDOR hole CLAUDE.md risk #1 describes: it lets
        // a caller with no usable unit scope see every unit's rows instead of none.
        // VahedScopeBehavior guarantees vahedCode is always a real, non-empty value here.
        var query = _dbContext.TB_IDENTITYHEADs
            .AsNoTracking()
            .Where(h => h.ISDELETED != true && h.VAHEDCODE == vahedCode);

        // Narrowing filters are opt-in: null means "do not narrow", never "match nothing" —
        // the opposite of the scope filter above.
        if (identityGroupId is not null)
        {
            query = query.Where(h => h.IDENTITYGROUPS_ID == identityGroupId);
        }

        if (!string.IsNullOrWhiteSpace(year))
        {
            query = query.Where(h => h.YEAR == year);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // SERIAL is the number a user actually reads off this record, and it is unique per
            // (group, unit, year) via AK_AK_IDENTYHEAD_IDENTYHE. Ordering by group first keeps
            // rows of one group together when no group filter is applied; ID is a pure
            // tie-breaker for stable paging.
            .OrderBy(h => h.IDENTITYGROUPS.IDENTITYGROUPS_DESC)
            .ThenBy(h => h.SERIAL)
            .ThenBy(h => h.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<IdentityHeadDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<IdentityHeadDto?> GetByIdAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // The owning unit is read first as a scalar so the access decision can tell "no such
        // row" (null, caller gets null, 404) from "another unit's row" (403). Projecting it
        // alongside the DTO in one query is not possible while ToDto stays a reusable
        // Expression, and this is a single-record edit-form fetch, not a hot path.
        var ownerVahedCode = await _dbContext.TB_IDENTITYHEADs
            .AsNoTracking()
            .Where(h => h.ID == id)
            .Select(h => h.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "IdentityHead");

        // No ISDELETED filter here on purpose: GetById returns the row regardless of its deletion
        // state; the caller decides what to do based on IdentityHeadDto.IsDeleted.
        return await _dbContext.TB_IDENTITYHEADs
            .AsNoTracking()
            .Where(h => h.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
