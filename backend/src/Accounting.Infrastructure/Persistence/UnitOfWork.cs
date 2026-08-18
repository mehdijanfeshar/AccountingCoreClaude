using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Oracle.ManagedDataAccess.Client;

namespace Accounting.Infrastructure.Persistence;

/// <summary>
/// Thin wrapper around <see cref="LegacyDbContext"/> implementing <see cref="IUnitOfWork"/>.
/// Intentionally exposes no per-entity members — see <see cref="IUnitOfWork"/> XML docs for
/// the transaction-boundary contract that repositories and handlers must follow.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly LegacyDbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Persists staged changes. Oracle-specific unique-constraint violations (ORA-00001, e.g.
    /// <c>UK_ACCOUNTCODE</c> or <c>UK_VOUCHERHEAD_NUMBER</c>) are detected here — the only
    /// place in the solution allowed to know about <see cref="OracleException"/> — and
    /// translated into an Application-level <see cref="DuplicateKeyException"/> so that
    /// <c>Accounting.Api</c> never has to reference Oracle types. Any other
    /// <see cref="DbUpdateException"/> is rethrown as-is.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is OracleException { Number: 1 })
        {
            throw new DuplicateKeyException(
                "A row with the same unique key already exists.", ex);
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            throw new InvalidOperationException(
                "No active transaction to commit. Call BeginTransactionAsync first.");
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
        }
    }
}
