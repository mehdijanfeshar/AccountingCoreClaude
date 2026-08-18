using System.Linq.Expressions;
using Accounting.Application.Accounts.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAccountCodeReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View. This is a deliberate, narrow exception to the general
/// "reports read from a Read Model" rule in CLAUDE.md — that rule targets heavy/analytical
/// reporting, not a simple GetAll/GetById on a single Legacy entity. Every query below
/// projects straight to <see cref="AccountCodeDto"/> (see <see cref="ToDto"/>) so no
/// navigation property is ever loaded, even though <see cref="TB_ACCOUNTCODE"/> declares
/// several collection navigations.
/// </summary>
public sealed class AccountCodeReadRepository : IAccountCodeReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_ACCOUNTCODE, AccountCodeDto>> ToDto = a => new AccountCodeDto(
        a.ID,
        a.TYPECODE,
        a.PARENTID,
        a.ACCCODE,
        a.ACCCODENAME,
        a.TYPEACTIVITY,
        a.SOURCEANDCONSUME_ID,
        a.IDENTYGROUPS_ID,
        a.TYPEACCCODE,
        a.CREATEDDATE,
        a.UPDATEDDATE,
        a.ADDUSERID,
        a.CHANGEUSERID,
        a.ISDELETED,
        a.MOINFORCLOSE,
        a.TYPEACTION);

    private readonly LegacyDbContext _dbContext;

    public AccountCodeReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AccountCodeDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        var query = _dbContext.TB_ACCOUNTCODEs
            .AsNoTracking()
            .Where(a => a.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // NOTE: ACCCODE is a string column, so this is an alphabetical sort, not a
            // numeric one (e.g. "10" sorts before "2"). This mirrors how the data is actually
            // stored in Legacy and is intentionally left as-is here. ID is a pure tie-breaker
            // so paging stays stable across identical ACCCODE values.
            .OrderBy(a => a.ACCCODE)
            .ThenBy(a => a.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<AccountCodeDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<AccountCodeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on AccountCodeDto.IsDeleted.
        //
        // NOTE: this query reads PARENTID, a risk-flagged column per CLAUDE.md —
        // GuidToChar36Converter parses it strictly (ParseExact) and will throw rather than
        // silently coerce a malformed Oracle value.
        return _dbContext.TB_ACCOUNTCODEs
            .AsNoTracking()
            .Where(a => a.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
