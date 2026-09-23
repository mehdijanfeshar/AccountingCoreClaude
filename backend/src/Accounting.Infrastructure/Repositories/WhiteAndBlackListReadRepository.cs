using System.Linq.Expressions;
using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IWhiteAndBlackListReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class WhiteAndBlackListReadRepository : IWhiteAndBlackListReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    ///
    /// The four display fields at the end reach through the <c>ACCOUNTCODE</c>/<c>VAHEDTYPE</c>
    /// navigations, which EF renders as LEFT JOINs — <c>VAHEDTYPE_ID</c> is nullable, so the
    /// unit-type columns come back null for a row that has no unit type, rather than dropping
    /// the row.
    /// </summary>
    private static readonly Expression<Func<TB_WHITEANDBLACKLIST, WhiteAndBlackListDto>> ToDto = w => new WhiteAndBlackListDto(
        w.ID,
        w.ACCOUNTCODE_ID,
        w.VAHEDTYPE_ID,
        w.CREATEDDATE,
        w.UPDATEDDATE,
        w.ADDUSERID,
        w.CHANGEUSERID,
        w.ISDELETED,
        w.FROMAUTHORIZEDDATE,
        w.TOAUTHORIZEDDATE,
        w.FROMLIMITATIONDATE,
        w.TOLIMITATIONDATE,
        w.STATE,
        w.ACCOUNTCODE.ACCCODE,
        w.ACCOUNTCODE.ACCCODENAME,
        w.VAHEDTYPE!.TYPECODE,
        w.VAHEDTYPE!.TYPENAME,
        w.VAHEDTYPE!.PARENTTYPECODE);

    private readonly LegacyDbContext _dbContext;

    public WhiteAndBlackListReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<WhiteAndBlackListDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        WhiteAndBlackListFilter filter,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        var query = _dbContext.TB_WHITEANDBLACKLISTs
            .AsNoTracking()
            .Where(w => w.ISDELETED != true);

        if (filter.AccountCodeId is { } accountCodeId)
        {
            query = query.Where(w => w.ACCOUNTCODE_ID == accountCodeId);
        }

        if (filter.VahedTypeId is { } vahedTypeId)
        {
            query = query.Where(w => w.VAHEDTYPE_ID == vahedTypeId);
        }

        if (filter.State is { } state)
        {
            query = query.Where(w => w.STATE == state);
        }

        // Date bounds. string.Compare(...) is used rather than the > / < operators because EF
        // Core translates it to a plain SQL comparison, while the C# operators are not defined on
        // string at all. The columns hold zero-padded YYYYMMDD Jalali text, so a lexicographic
        // comparison IS a chronological one — see GetWhiteAndBlackListsQuery's XML doc. A row
        // whose column is NULL compares as "less than" any bound and is therefore excluded by a
        // lower bound and kept by none of the upper bounds, which is the intended behaviour: a
        // row with no authorized-from date does not fall inside an authorized-from range.
        if (!string.IsNullOrEmpty(filter.FromAuthorizedDate))
        {
            var bound = filter.FromAuthorizedDate;
            query = query.Where(w => string.Compare(w.FROMAUTHORIZEDDATE, bound) >= 0);
        }

        if (!string.IsNullOrEmpty(filter.ToAuthorizedDate))
        {
            var bound = filter.ToAuthorizedDate;
            query = query.Where(w => w.TOAUTHORIZEDDATE != null && string.Compare(w.TOAUTHORIZEDDATE, bound) <= 0);
        }

        if (!string.IsNullOrEmpty(filter.FromLimitationDate))
        {
            var bound = filter.FromLimitationDate;
            query = query.Where(w => string.Compare(w.FROMLIMITATIONDATE, bound) >= 0);
        }

        if (!string.IsNullOrEmpty(filter.ToLimitationDate))
        {
            var bound = filter.ToLimitationDate;
            query = query.Where(w => w.TOLIMITATIONDATE != null && string.Compare(w.TOLIMITATIONDATE, bound) <= 0);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // CREATEDDATE is non-nullable on this table, so this is a clean chronological sort.
            // ID is a pure tie-breaker so paging stays stable across identical CreatedDate values.
            .OrderBy(w => w.CREATEDDATE)
            .ThenBy(w => w.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<WhiteAndBlackListDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<WhiteAndBlackListDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on WhiteAndBlackListDto.IsDeleted.
        return _dbContext.TB_WHITEANDBLACKLISTs
            .AsNoTracking()
            .Where(w => w.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WhiteAndBlackListKey>> GetExistingKeysAsync(
        IReadOnlyCollection<Guid> accountCodeIds,
        CancellationToken cancellationToken = default)
    {
        if (accountCodeIds.Count == 0)
        {
            return [];
        }

        // Projecting the four UNIQUE-key columns only — this runs once per bulk create and the
        // row count is bounded by (selected accounts × unit types), so pulling whole entities
        // here would be pure waste.
        var keys = await _dbContext.TB_WHITEANDBLACKLISTs
            .AsNoTracking()
            .Where(w => w.ISDELETED != true && accountCodeIds.Contains(w.ACCOUNTCODE_ID))
            .Select(w => new WhiteAndBlackListKey(
                w.ACCOUNTCODE_ID,
                w.VAHEDTYPE_ID,
                w.FROMAUTHORIZEDDATE,
                w.TOAUTHORIZEDDATE))
            .ToListAsync(cancellationToken);

        return keys;
    }
}
