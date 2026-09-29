using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PaymentRequestEventReadRepository : IPaymentRequestEventReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public PaymentRequestEventReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PaymentRequestEventDto>> GetByPaymentRequestIdAsync(
        Guid paymentRequestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TR_PAYMENT_REQUEST_EVENTs
            .AsNoTracking()
            .Where(e => e.PAYMENT_REQUEST_ID == paymentRequestId)
            .OrderBy(e => e.CREATEDDATE)
            .ThenBy(e => e.ID)
            .Select(e => new PaymentRequestEventDto(
                e.ID, e.ACTION, e.FROM_STATE, e.TO_STATE, e.NOTE, e.ADDUSERID, e.CREATEDDATE, e.CLIENT_IP))
            .ToListAsync(cancellationToken);
    }
}
