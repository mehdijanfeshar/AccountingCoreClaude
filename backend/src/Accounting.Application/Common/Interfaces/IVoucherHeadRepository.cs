using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_VOUCHERSHEAD"/>. Only stages the entity for
/// insertion — it does NOT call SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public interface IVoucherHeadRepository
{
    Task AddAsync(TB_VOUCHERSHEAD voucherHead, CancellationToken cancellationToken = default);
}
