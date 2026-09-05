using System.Linq.Expressions;
using Accounting.Application.Receipts.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IReceiptReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class ReceiptReadRepository : IReceiptReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_RECEIP, ReceiptDto>> ToDto = r => new ReceiptDto(
        r.ID,
        r.RECEIPT_KIND,
        r.RECEIPT_DATE,
        r.RECEIPT_NO,
        r.DATE_RSID,
        r.VAHEDCODE,
        r.YEAR,
        r.CREATEDDATE,
        r.UPDATEDDATE,
        r.ADDUSERID,
        r.CHANGEUSERID,
        r.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public ReceiptReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ReceiptDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is a non-nullable bool on this table, so the simple
        // == false form is used (unlike the bool? tables elsewhere in this project).
        var query = _dbContext.TB_RECEIPs
            .AsNoTracking()
            .Where(r => r.ISDELETED == false);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // RECEIPT_NO is required but not unique, so ID is a pure tie-breaker for stable
            // paging.
            .OrderBy(r => r.RECEIPT_NO)
            .ThenBy(r => r.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReceiptDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<ReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on ReceiptDto.IsDeleted.
        return _dbContext.TB_RECEIPs
            .AsNoTracking()
            .Where(r => r.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
