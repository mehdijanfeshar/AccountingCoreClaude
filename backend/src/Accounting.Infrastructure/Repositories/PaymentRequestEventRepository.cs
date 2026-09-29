using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PaymentRequestEventRepository : IPaymentRequestEventRepository
{
    private readonly LegacyDbContext _dbContext;

    public PaymentRequestEventRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TR_PAYMENT_REQUEST_EVENT paymentRequestEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_PAYMENT_REQUEST_EVENTs.AddAsync(paymentRequestEvent, cancellationToken);
    }

    public Task<TB_TR_PAYMENT_REQUEST_EVENT?> GetLastApproveEventAsync(
        Guid paymentRequestId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_TR_PAYMENT_REQUEST_EVENTs
            .AsNoTracking()
            .Where(e => e.PAYMENT_REQUEST_ID == paymentRequestId && e.ACTION == PaymentRequestEventAction.Approve)
            .OrderByDescending(e => e.CREATEDDATE)
            .ThenByDescending(e => e.ID)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
