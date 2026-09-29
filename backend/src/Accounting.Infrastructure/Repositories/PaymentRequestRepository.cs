using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PaymentRequestRepository : IPaymentRequestRepository
{
    private readonly LegacyDbContext _dbContext;

    public PaymentRequestRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TR_PAYMENT_REQUEST paymentRequest, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_PAYMENT_REQUESTs.AddAsync(paymentRequest, cancellationToken);
    }

    public async Task<TB_TR_PAYMENT_REQUEST?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_TR_PAYMENT_REQUESTs.FirstOrDefaultAsync(p => p.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PaymentRequest");

        return entity;
    }

    public async Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default)
    {
        // Same client-side-parsed-MAX approach as ChargeAndCostRepository.GetNextCodeAsync — CODE
        // is "PAY-" + a zero-padded number, a varchar column a lexicographic MAX would not sort
        // numerically for. Deleted rows are counted on purpose — a soft-deleted request's code
        // must never be handed out again.
        var existingCodes = await _dbContext.TB_TR_PAYMENT_REQUESTs
            .AsNoTracking()
            .Where(p => p.VAHEDCODE == vahedCode && p.YEAR == year)
            .Select(p => p.CODE)
            .ToListAsync(cancellationToken);

        var highest = 0;

        foreach (var code in existingCodes)
        {
            var numericPart = code.StartsWith("PAY-", StringComparison.Ordinal) ? code["PAY-".Length..] : code;

            if (int.TryParse(numericPart, out var parsed) && parsed > highest)
            {
                highest = parsed;
            }
        }

        return highest + 1;
    }

    public async Task<bool> ExistsDuplicateAsync(
        string beneficiaryNationalId,
        string invoiceRef,
        string vahedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_TR_PAYMENT_REQUESTs
            .AsNoTracking()
            .Where(p => p.VAHEDCODE == vahedCode
                        && !p.ISDELETED
                        && p.REQUEST_STATE != PaymentRequestState.Rejected
                        && p.BENEFICIARY_NATIONAL_ID == beneficiaryNationalId
                        && p.INVOICE_REF == invoiceRef);

        if (excludeId is { } id)
        {
            query = query.Where(p => p.ID != id);
        }

        var count = await query.CountAsync(cancellationToken);

        return count > 0;
    }

    public async Task AddCostCenterTafsiliLinkAsync(
        TB_TR_PAYMENT_REQUEST_LINK_TAFSILI link, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_PAYMENT_REQUEST_LINK_TAFSILIs.AddAsync(link, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_TR_PAYMENT_REQUEST_LINK_TAFSILI>> GetActiveCostCenterTafsiliLinksAsync(
        Guid paymentRequestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TR_PAYMENT_REQUEST_LINK_TAFSILIs
            .Where(l => l.PAYMENT_REQUEST_ID == paymentRequestId && !l.ISDELETED)
            .ToListAsync(cancellationToken);
    }
}
