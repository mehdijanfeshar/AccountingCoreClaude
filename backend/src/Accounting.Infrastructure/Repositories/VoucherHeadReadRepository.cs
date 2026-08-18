using System.Linq.Expressions;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Queries;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVoucherHeadReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View. This is a deliberate, narrow exception to the general
/// "reports read from a Read Model" rule in CLAUDE.md — that rule targets heavy/analytical
/// reporting, not a simple GetAll/GetById on a single Legacy entity. Every query below
/// projects straight to <see cref="VoucherHeadDto"/> (see <see cref="ToDto"/>) so no
/// navigation property is ever loaded, and the <c>ATTACHFILE</c> BLOB column is never selected.
/// </summary>
public sealed class VoucherHeadReadRepository : IVoucherHeadReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity — in
    /// particular, the <c>ATTACHFILE</c> BLOB column is never touched.
    /// </summary>
    private static readonly Expression<Func<TB_VOUCHERSHEAD, VoucherHeadDto>> ToDto = v => new VoucherHeadDto(
        v.ID,
        v.DOC_NUM,
        v.DATE_DOC,
        v.DOCLIFE,
        v.HEAD_DESC,
        v.APENDIX,
        v.SYSTEM_TYPE,
        v.FLAG_STATE,
        v.CREATEDDATE,
        v.UPDATEDDATE,
        v.ADDUSERID,
        v.CHANGEUSERID,
        v.VAHEDCODE,
        v.YEAR,
        v.ISDELETED,
        v.ATTACHFILE_NAME,
        v.ATF_NUM,
        v.ISAUTOMATIC,
        v.SNDVAHEDCODE,
        v.PARENTHEAD_ID,
        v.GLOBALNUMBER);

    private readonly LegacyDbContext _dbContext;

    public VoucherHeadReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<VoucherHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? year,
        string? vahedCode,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        var query = _dbContext.TB_VOUCHERSHEADs
            .AsNoTracking()
            .Where(v => v.ISDELETED != true);

        if (!string.IsNullOrEmpty(year))
        {
            query = query.Where(v => v.YEAR == year);
        }

        if (!string.IsNullOrEmpty(vahedCode))
        {
            query = query.Where(v => v.VAHEDCODE == vahedCode);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // NOTE: DOC_NUM and YEAR are string columns, so this is an alphabetical sort, not
            // a numeric one (e.g. "10" sorts before "2"). This mirrors how the data is stored
            // in Legacy and is intentionally left as-is here. ID is a pure tie-breaker so
            // paging stays stable across identical Year/VahedCode/DocNum values.
            .OrderBy(v => v.YEAR)
            .ThenBy(v => v.VAHEDCODE)
            .ThenBy(v => v.DOC_NUM)
            .ThenBy(v => v.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<VoucherHeadDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<VoucherHeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on VoucherHeadDto.IsDeleted.
        return _dbContext.TB_VOUCHERSHEADs
            .AsNoTracking()
            .Where(v => v.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
