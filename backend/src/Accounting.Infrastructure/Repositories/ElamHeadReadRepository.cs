using System.Linq.Expressions;
using Accounting.Application.ElamHeads.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IElamHeadReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class ElamHeadReadRepository : IElamHeadReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_ELAMHEAD, ElamHeadDto>> ToDto = e => new ElamHeadDto(
        e.ID,
        e.VOUCHERSHEAD_ID,
        e.ELAMH_SERIALNO,
        e.ELAMH_CODE,
        e.ELAMH_DABIRNO,
        e.ELAMH_DABIRDATE,
        e.ELAMH_PRINTNO,
        e.ELAMH_CASE,
        e.SERIALNO_INPUT,
        e.WEB_STAT,
        e.ELAMH_DATE,
        e.ELAMH_DESC,
        e.WORKSHOP_ID,
        e.ELAMH_RCVNO,
        e.ELAMH_RCVDT,
        e.ELAMH_LSTMON,
        e.PAY_NO,
        e.ELAMHDRAMAD_TYPE,
        e.PEIMAN_NO,
        e.ELAMH_WORKSHOPCODE,
        e.ELAMH_WORKSHOPNAME,
        e.ELAMH_SENDRCVVAHED,
        e.ELAMH_YEAR,
        e.VAHEDCODE,
        e.YEAR,
        e.ELAMSENDERID,
        e.CREATEDDATE,
        e.UPDATEDDATE,
        e.ADDUSERID,
        e.CHANGEUSERID,
        e.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public ElamHeadReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ElamHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        var query = _dbContext.TB_ELAMHEADs
            .AsNoTracking()
            .Where(e => e.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // ELAMH_SERIALNO is nullable (Oracle sorts NULLs last) and not unique on its own
            // (AK_AK_ELAMHEAD_ELAMHEAD is composite over SERIALNO+CODE+VAHEDCODE), so ID is a
            // pure tie-breaker to keep paging stable.
            .OrderBy(e => e.ELAMH_SERIALNO)
            .ThenBy(e => e.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ElamHeadDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<ElamHeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on ElamHeadDto.IsDeleted.
        return _dbContext.TB_ELAMHEADs
            .AsNoTracking()
            .Where(e => e.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
