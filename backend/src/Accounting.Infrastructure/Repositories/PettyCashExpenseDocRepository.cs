using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PettyCashExpenseDocRepository : IPettyCashExpenseDocRepository
{
    private static readonly PettyCashDocState[] InFlightStates =
    {
        PettyCashDocState.New,
        PettyCashDocState.PendingReview,
        PettyCashDocState.Returned,
    };

    private readonly LegacyDbContext _dbContext;

    public PettyCashExpenseDocRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_EXPENSE_DOC expenseDoc, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_EXPENSE_DOCs.AddAsync(expenseDoc, cancellationToken);
    }

    public async Task<TB_PC_EXPENSE_DOC?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PC_EXPENSE_DOCs
            .FirstOrDefaultAsync(d => d.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PettyCashExpenseDoc");

        return entity;
    }

    public async Task<bool> ExistsActiveDuplicateAsync(
        string vendorNationalId,
        string invoiceNo,
        string vahedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_PC_EXPENSE_DOCs
            .AsNoTracking()
            .Where(d => d.VAHEDCODE == vahedCode
                && !d.ISDELETED
                && d.DOC_STATE != PettyCashDocState.Rejected
                && d.VENDOR_NATIONAL_ID == vendorNationalId
                && d.INVOICE_NO == invoiceNo);

        if (excludeId is { } id)
        {
            query = query.Where(d => d.ID != id);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<PettyCashFundExposure> GetFundExposureAsync(
        Guid revolvingFundId,
        string vahedCode,
        Guid? excludeDocId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_PC_EXPENSE_DOCs
            .AsNoTracking()
            .Where(d => d.REVOLVINGFUND_ID == revolvingFundId && d.VAHEDCODE == vahedCode && !d.ISDELETED);

        if (excludeDocId is { } id)
        {
            query = query.Where(d => d.ID != id);
        }

        // Materialised client-side (bounded by one fund's live document count, never a hot
        // aggregate over the whole table) so the two-column sum is a plain C# Sum rather than
        // asking the provider to translate "SUM(a ?? 0 + b ?? 0)", which is where EF Core /
        // Oracle null-coalescing-inside-aggregate translations tend to get fragile.
        var approvedRows = await query
            .Where(d => d.DOC_STATE == PettyCashDocState.Approved)
            .Select(d => new { d.AMOUNT_BEFORE_TAX, d.VAT_AMOUNT })
            .ToListAsync(cancellationToken);

        var inFlightRows = await query
            .Where(d => InFlightStates.Contains(d.DOC_STATE))
            .Select(d => new { d.AMOUNT_BEFORE_TAX, d.VAT_AMOUNT })
            .ToListAsync(cancellationToken);

        var approvedAmount = approvedRows.Sum(r => (r.AMOUNT_BEFORE_TAX ?? 0m) + (r.VAT_AMOUNT ?? 0m));
        var inFlightAmount = inFlightRows.Sum(r => (r.AMOUNT_BEFORE_TAX ?? 0m) + (r.VAT_AMOUNT ?? 0m));

        return new PettyCashFundExposure(approvedAmount, approvedRows.Count, inFlightAmount, inFlightRows.Count);
    }
}
