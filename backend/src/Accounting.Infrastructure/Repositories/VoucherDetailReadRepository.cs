using System.Linq.Expressions;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Queries;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVoucherDetailReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the general
/// "reports read from a Read Model" rule already applied by <c>VoucherHeadReadRepository</c>.
/// Every query below projects straight to <see cref="VoucherDetailDto"/> (see <see cref="ToDto"/>)
/// so no navigation property is ever loaded.
/// </summary>
public sealed class VoucherDetailReadRepository : IVoucherDetailReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate it
    /// into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_VOUCHERSDETAIL, VoucherDetailDto>> ToDto = d => new VoucherDetailDto(
        d.ID,
        d.VOUCHERSHEAD_ID,
        d.ACCOUNT_ID,
        d.RECEIP_ID,
        d.CHECK_ID,
        d.LOWLEVELCODE_ID,
        d.ETEBAR_ID,
        d.DESCRIPTION,
        d.RADIF,
        d.DEBTOR,
        d.CREDITOR,
        d.CREATEDDATE,
        d.UPDATEDDATE,
        d.ADDUSERID,
        d.CHANGEUSERID,
        d.VAHEDCODE,
        d.YEAR,
        d.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public VoucherDetailReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<VoucherDetailDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? voucherHeadId,
        string? year,
        string? vahedCode,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        var query = _dbContext.TB_VOUCHERSDETAILs
            .AsNoTracking()
            .Where(d => d.ISDELETED != true);

        if (voucherHeadId.HasValue)
        {
            query = query.Where(d => d.VOUCHERSHEAD_ID == voucherHeadId.Value);
        }

        if (!string.IsNullOrEmpty(year))
        {
            query = query.Where(d => d.YEAR == year);
        }

        if (!string.IsNullOrEmpty(vahedCode))
        {
            query = query.Where(d => d.VAHEDCODE == vahedCode);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // RADIF is int? — NULLs sort last under Oracle's default ASC NULLS LAST behaviour.
            // ID is a pure tie-breaker so paging stays stable across identical RADIF values
            // (including multiple NULLs).
            .OrderBy(d => d.RADIF)
            .ThenBy(d => d.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<VoucherDetailDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<VoucherDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on VoucherDetailDto.IsDeleted.
        return _dbContext.TB_VOUCHERSDETAILs
            .AsNoTracking()
            .Where(d => d.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
