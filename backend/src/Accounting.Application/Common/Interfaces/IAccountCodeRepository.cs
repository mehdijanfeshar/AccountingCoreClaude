using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_ACCOUNTCODE"/>. Only stages the entity for
/// insertion — it does NOT call SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public interface IAccountCodeRepository
{
    Task AddAsync(TB_ACCOUNTCODE accountCode, CancellationToken cancellationToken = default);
}
