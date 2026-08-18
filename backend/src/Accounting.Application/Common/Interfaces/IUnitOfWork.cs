namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Narrow, entity-agnostic unit of work. It exposes ONLY change-persistence and explicit
/// transaction control — it intentionally does NOT expose per-entity repository properties
/// (e.g. no <c>IAccountCodeRepository AccountCodes { get; }</c>). Typed repositories are
/// injected directly into MediatR command handlers via constructor DI.
///
/// Adding a new Legacy entity to the write model must require only a new repository
/// interface + implementation + command; it must NEVER require an edit to this interface
/// or its implementation.
///
/// Transaction boundary ownership: repositories only stage changes (e.g.
/// <c>DbSet.AddAsync</c>) and must NEVER call <see cref="SaveChangesAsync"/> themselves.
/// The command handler owns the transaction boundary and calls
/// <see cref="SaveChangesAsync"/> exactly once per use case. For multi-step / multi-entity
/// use cases (e.g. voucher posting, number generation, period closing, batch operations)
/// the handler should additionally wrap the work in
/// <see cref="BeginTransactionAsync"/> / <see cref="CommitTransactionAsync"/> /
/// <see cref="RollbackTransactionAsync"/>.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all staged changes tracked by the underlying DbContext in a single
    /// round-trip. Must be called exactly once per use case, by the handler.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins an explicit database transaction for use cases that need a boundary wider
    /// than a single <see cref="SaveChangesAsync"/> call.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the transaction started by <see cref="BeginTransactionAsync"/>.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction started by <see cref="BeginTransactionAsync"/>, if any.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
