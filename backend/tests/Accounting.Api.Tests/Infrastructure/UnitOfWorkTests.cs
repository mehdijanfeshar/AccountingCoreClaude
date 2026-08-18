using System.Reflection;
using Accounting.Application.Common.Exceptions;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Accounting.Api.Tests.Infrastructure;

/// <summary>
/// Exercises <see cref="UnitOfWork.SaveChangesAsync"/>'s ORA-00001 → <see cref="DuplicateKeyException"/>
/// translation. There is no Oracle test environment available (see CLAUDE.md / task brief), so
/// these tests never touch a real database: <see cref="LegacyDbContext.SaveChangesAsync"/> is
/// overridden on a throwaway subclass to synchronously throw a pre-built exception, and
/// <see cref="UnitOfWork.SaveChangesAsync"/> is exercised for real against that.
///
/// <see cref="OracleException"/> deliberately has no public constructor. <see cref="CreateOracleException"/>
/// invokes the driver's own internal <c>(int, string, string, string, int)</c> constructor via
/// reflection to obtain a genuine instance of the exact type
/// <c>UnitOfWork.SaveChangesAsync</c> pattern-matches on (verified against the actual resolved
/// package version, Oracle.ManagedDataAccess.Core 23.26.0, before writing these tests) — this is
/// not a stub/fake standing in for the real type.
/// </summary>
public sealed class UnitOfWorkTests
{
    private static OracleException CreateOracleException(int number, string message)
    {
        var ctor = typeof(OracleException).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(int), typeof(string), typeof(string), typeof(string), typeof(int) },
            modifiers: null);

        if (ctor is null)
        {
            throw new InvalidOperationException(
                "Oracle.ManagedDataAccess.Client.OracleException no longer exposes the internal " +
                "(int, string, string, string, int) constructor this test relies on to build a " +
                "real instance without a live database. Update this helper to match whatever " +
                "internal constructor the current package version exposes.");
        }

        return (OracleException)ctor.Invoke(new object[] { number, message, "proc", "source", number });
    }

    /// <summary>
    /// Stands in for a real Oracle connection failure: overrides the one virtual seam
    /// <see cref="UnitOfWork"/> calls into (<see cref="DbContext.SaveChangesAsync(CancellationToken)"/>)
    /// to throw synchronously, without ever touching a database. The in-memory provider backing
    /// <see cref="DbContextOptions{TContext}"/> is never actually used — the override throws
    /// before <c>base.SaveChangesAsync</c> (and therefore the EF model) is ever reached.
    /// </summary>
    private sealed class ThrowingLegacyDbContext : LegacyDbContext
    {
        private readonly Exception _exceptionToThrow;

        public ThrowingLegacyDbContext(DbContextOptions<LegacyDbContext> options, Exception exceptionToThrow)
            : base(options)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw _exceptionToThrow;
    }

    private static DbContextOptions<LegacyDbContext> InMemoryOptions()
        => new DbContextOptionsBuilder<LegacyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task SaveChangesAsync_DbUpdateExceptionWrappingOra00001_IsTranslatedToDuplicateKeyException()
    {
        var oracleException = CreateOracleException(1, "ORA-00001: unique constraint (X.UK_ACCOUNTCODE) violated");
        var dbUpdateException = new DbUpdateException("Saving changes failed.", oracleException);

        await using var context = new ThrowingLegacyDbContext(InMemoryOptions(), dbUpdateException);
        await using var unitOfWork = new UnitOfWork(context);

        var thrown = await Assert.ThrowsAsync<DuplicateKeyException>(() => unitOfWork.SaveChangesAsync());

        Assert.Same(dbUpdateException, thrown.InnerException);
    }

    [Fact]
    public async Task SaveChangesAsync_DbUpdateExceptionWrappingNonUniqueOracleError_IsRethrownUnchanged()
    {
        // ORA-00904 (invalid identifier) is a real, distinct Oracle error number — proves the
        // translation is keyed specifically on Number == 1, not "any OracleException".
        var oracleException = CreateOracleException(904, "ORA-00904: invalid identifier");
        var dbUpdateException = new DbUpdateException("Saving changes failed.", oracleException);

        await using var context = new ThrowingLegacyDbContext(InMemoryOptions(), dbUpdateException);
        await using var unitOfWork = new UnitOfWork(context);

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());

        Assert.Same(dbUpdateException, thrown);
    }

    [Fact]
    public async Task SaveChangesAsync_DbUpdateExceptionWrappingNonOracleException_IsRethrownUnchanged()
    {
        var dbUpdateException = new DbUpdateException(
            "Saving changes failed.", new InvalidOperationException("not an Oracle error"));

        await using var context = new ThrowingLegacyDbContext(InMemoryOptions(), dbUpdateException);
        await using var unitOfWork = new UnitOfWork(context);

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());

        Assert.Same(dbUpdateException, thrown);
    }

    [Fact]
    public async Task SaveChangesAsync_ExceptionThatIsNotADbUpdateException_PropagatesUnchanged()
    {
        var exception = new InvalidOperationException("unrelated failure, not even a DbUpdateException");

        await using var context = new ThrowingLegacyDbContext(InMemoryOptions(), exception);
        await using var unitOfWork = new UnitOfWork(context);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.SaveChangesAsync());

        Assert.Same(exception, thrown);
    }
}
