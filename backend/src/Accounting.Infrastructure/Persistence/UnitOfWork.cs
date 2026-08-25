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
    /// Oracle error number for ORA-00001, "unique constraint violated".
    /// </summary>
    private const int OracleUniqueConstraintViolated = 1;

    /// <summary>
    /// Oracle error number for ORA-02291, "integrity constraint violated - parent key not found".
    /// Its sibling ORA-02292 ("child record found") is deliberately not handled — see
    /// <see cref="ForeignKeyViolationException"/> XML doc: this project performs no hard deletes,
    /// so that error is unreachable and mapping it would be speculation.
    /// </summary>
    private const int OracleParentKeyNotFound = 2291;

    /// <summary>
    /// Persists staged changes. Oracle-specific constraint violations are detected here — this is
    /// the only place in the solution allowed to know about <see cref="OracleException"/> — and
    /// translated into Application-level exceptions so that <c>Accounting.Api</c> never has to
    /// reference Oracle types:
    /// <list type="bullet">
    /// <item><description>ORA-00001 (unique constraint, e.g. <c>UK_ACCOUNTCODE</c> or
    /// <c>UK_VOUCHERHEAD_NUMBER</c>) → <see cref="DuplicateKeyException"/> → 409.</description></item>
    /// <item><description>ORA-02291 (parent key not found, e.g. <c>FK_VOUCHERDETAIL_ACCOUNCODE</c>
    /// when <c>ACCOUNT_ID</c> references a non-existent <c>TB_ACCOUNTCODE</c> row) →
    /// <see cref="ForeignKeyViolationException"/> → 400.</description></item>
    /// </list>
    /// Any other <see cref="DbUpdateException"/> is rethrown as-is.
    ///
    /// Because this translation lives in the single central <see cref="IUnitOfWork"/>
    /// implementation rather than in any individual handler, EVERY write path gets it uniformly —
    /// create/update on <c>TB_ACCOUNTCODE</c>, <c>TB_VOUCHERSHEAD</c> and <c>TB_VOUCHERSDETAIL</c>
    /// alike, including the composite head+lines create and the embedded tafsili-link writes —
    /// with no per-command opt-in to forget.
    ///
    /// The generic messages below are intentional: the raw Oracle text names the violated
    /// constraint, table and column, which must never reach an HTTP response body. The original
    /// exception is preserved as <c>InnerException</c> for logging only.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is OracleException { Number: OracleUniqueConstraintViolated })
        {
            throw new DuplicateKeyException(
                "A row with the same unique key already exists.", ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is OracleException { Number: OracleParentKeyNotFound })
        {
            throw new ForeignKeyViolationException(
                "One or more referenced records do not exist.", ex);
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
