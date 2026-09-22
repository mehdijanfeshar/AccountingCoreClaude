using Accounting.Application.Accounts.Commands.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real EF Core tests for the two reads <c>AccountLevelLinkSynchronizer</c> depends on, plus the
/// synchronizer running on top of them.
///
/// <b>These exist because the assumptions involved cannot be tested with a mocked repository.</b>
/// The Application-level tests prove the reconcile logic given a set of rows; what they cannot
/// prove is that the rows the real repository hands it are the right ones. Two EF behaviours are
/// load-bearing and both are silent when wrong:
/// <list type="number">
/// <item>A row staged with <c>AddTafsilGroupLinkAsync</c> is <b>not</b> returned by a subsequent
/// query — EF sends SQL to the server and does not replay it against pending inserts. If
/// <c>GetTafsilGroupLinksForSyncAsync</c> did not merge pending inserts, creating the first link
/// for a level would compute "no links here" and retire the level it had just enabled.</item>
/// <item>A row already mutated by the caller comes back from a later query as that same instance
/// (EF's identity map), so the update and unlink paths see their own change. Were it a fresh
/// instance, an unlink would still read <c>ISDELETED = false</c> and leave the level required.</item>
/// </list>
///
/// Provider choice (SQLite in-memory, never live Oracle) and the hand-written <c>CREATE TABLE</c>
/// workaround follow <see cref="TafsiliLookupReadRepositoryTests"/> — see that file for the
/// rationale.
/// </summary>
public sealed class AccountCodeRepositoryLevelSyncTests : IDisposable
{
    private const string CreateAccountLinkLevelTableSql = """
        CREATE TABLE TB_ACCOUNT_LINK_LEVEL (
            ID TEXT PRIMARY KEY,
            ACCOUNT_ID TEXT NOT NULL,
            LEVEL_ID TEXT NOT NULL,
            CREATEDDATE TEXT NOT NULL,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT NOT NULL,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER NOT NULL
        );
        """;

    private const string CreateAccountLinkTafsilGroupTableSql = """
        CREATE TABLE TB_ACCOUNT_LINK_TAFSILGROUP (
            ID TEXT PRIMARY KEY,
            ACCOUNT_ID TEXT NOT NULL,
            TAFSILGROUP_ID TEXT NOT NULL,
            LEVEL_ID TEXT NOT NULL,
            CREATEDDATE TEXT NOT NULL,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT NOT NULL,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER NOT NULL
        );
        """;

    private static readonly DateTime SeedDate = new(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherAccountId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid LevelOne = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LevelFour = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly SqliteConnection _connection;

    public AccountCodeRepositoryLevelSyncTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateAccountLinkLevelTableSql);
        setupContext.Database.ExecuteSqlRaw(CreateAccountLinkTafsilGroupTableSql);
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LegacyDbContext(options);
    }

    private static AccountLevelLinkSynchronizer Synchronizer(IAccountCodeRepository repository, string userId = "tester")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return new AccountLevelLinkSynchronizer(repository, currentUser.Object);
    }

    private static TB_ACCOUNT_LINK_LEVEL LevelLink(Guid accountId, Guid levelId, bool isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNT_ID = accountId,
        LEVEL_ID = levelId,
        CREATEDDATE = SeedDate,
        ADDUSERID = "seed-user",
        ISDELETED = isDeleted,
    };

    private static TB_ACCOUNT_LINK_TAFSILGROUP GroupLink(Guid accountId, Guid levelId, bool isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNT_ID = accountId,
        LEVEL_ID = levelId,
        TAFSILGROUP_ID = Guid.NewGuid(),
        CREATEDDATE = SeedDate,
        ADDUSERID = "seed-user",
        ISDELETED = isDeleted,
    };

    /// <summary>
    /// The pending-insert merge. Without it this test finds no link for the new level and the
    /// synchronizer inserts nothing.
    /// </summary>
    [Fact]
    public async Task Sync_SeesALinkStagedInTheSameUnitOfWork()
    {
        await using var context = CreateContext();
        var repository = new AccountCodeRepository(context);

        await repository.AddTafsilGroupLinkAsync(GroupLink(AccountId, LevelOne));
        await Synchronizer(repository).SyncAsync(AccountId);
        await context.SaveChangesAsync();

        await using var verifyContext = CreateContext();
        var levelRows = await verifyContext.TB_ACCOUNT_LINK_LEVELs
            .Where(l => l.ACCOUNT_ID == AccountId && !l.ISDELETED)
            .ToListAsync();

        Assert.Equal(LevelOne, Assert.Single(levelRows).LEVEL_ID);
    }

    /// <summary>
    /// The identity-map half: the unlink path soft-deletes the row it loaded, then the
    /// synchronizer queries again and must see that same, already-mutated instance.
    /// </summary>
    [Fact]
    public async Task Sync_SeesAnUnlinkMadeInTheSameUnitOfWork()
    {
        var groupLink = GroupLink(AccountId, LevelOne);
        await using (var seedContext = CreateContext())
        {
            seedContext.TB_ACCOUNT_LINK_TAFSILGROUPs.Add(groupLink);
            seedContext.TB_ACCOUNT_LINK_LEVELs.Add(LevelLink(AccountId, LevelOne));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new AccountCodeRepository(context);

        var loaded = await repository.GetTafsilGroupLinkForUpdateAsync(AccountId, groupLink.ID);
        loaded!.ISDELETED = true;

        await Synchronizer(repository, "srvusr01").SyncAsync(AccountId);
        await context.SaveChangesAsync();

        await using var verifyContext = CreateContext();
        var levelRow = await verifyContext.TB_ACCOUNT_LINK_LEVELs
            .SingleAsync(l => l.ACCOUNT_ID == AccountId);

        Assert.True(levelRow.ISDELETED);
        Assert.Equal("srvusr01", levelRow.CHANGEUSERID);
    }

    /// <summary>
    /// The 005000 repair, end to end against a real database: a level that is active with no
    /// گروه تفصیلی behind it is retired the next time this معین's links are saved, and the level
    /// that does have a group — and was never asked for — becomes required.
    /// </summary>
    [Fact]
    public async Task Sync_RepairsAnAccountWhoseTwoTablesDisagree()
    {
        await using (var seedContext = CreateContext())
        {
            seedContext.TB_ACCOUNT_LINK_LEVELs.Add(LevelLink(AccountId, LevelFour));
            seedContext.TB_ACCOUNT_LINK_TAFSILGROUPs.Add(GroupLink(AccountId, LevelOne));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        await Synchronizer(new AccountCodeRepository(context)).SyncAsync(AccountId);
        await context.SaveChangesAsync();

        await using var verifyContext = CreateContext();
        var rows = await verifyContext.TB_ACCOUNT_LINK_LEVELs
            .Where(l => l.ACCOUNT_ID == AccountId)
            .ToListAsync();

        Assert.True(rows.Single(r => r.LEVEL_ID == LevelFour).ISDELETED);
        Assert.False(rows.Single(r => r.LEVEL_ID == LevelOne).ISDELETED);
    }

    /// <summary>
    /// Both reads are scoped to one معین. A سطح another معین requires is none of this account's
    /// business, and retiring it would be a cross-account write from a save that never mentioned it.
    /// </summary>
    [Fact]
    public async Task Sync_LeavesOtherAccountsAlone()
    {
        await using (var seedContext = CreateContext())
        {
            seedContext.TB_ACCOUNT_LINK_LEVELs.Add(LevelLink(OtherAccountId, LevelFour));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        await Synchronizer(new AccountCodeRepository(context)).SyncAsync(AccountId);
        await context.SaveChangesAsync();

        await using var verifyContext = CreateContext();
        var otherRow = await verifyContext.TB_ACCOUNT_LINK_LEVELs
            .SingleAsync(l => l.ACCOUNT_ID == OtherAccountId);

        Assert.False(otherRow.ISDELETED);
    }

    /// <summary>
    /// Running twice must not produce a second row for the same level — this table has no unique
    /// constraint on (ACCOUNT_ID, LEVEL_ID), so nothing but this behaviour prevents duplicates.
    /// </summary>
    [Fact]
    public async Task Sync_IsIdempotentAcrossRepeatedRuns()
    {
        await using (var seedContext = CreateContext())
        {
            seedContext.TB_ACCOUNT_LINK_TAFSILGROUPs.Add(GroupLink(AccountId, LevelOne));
            await seedContext.SaveChangesAsync();
        }

        for (var run = 0; run < 2; run++)
        {
            await using var context = CreateContext();
            await Synchronizer(new AccountCodeRepository(context)).SyncAsync(AccountId);
            await context.SaveChangesAsync();
        }

        await using var verifyContext = CreateContext();
        var rows = await verifyContext.TB_ACCOUNT_LINK_LEVELs
            .Where(l => l.ACCOUNT_ID == AccountId)
            .ToListAsync();

        Assert.Single(rows);
        Assert.False(rows[0].ISDELETED);
    }

    /// <summary>
    /// Re-linking a level that was previously retired revives the original row rather than adding
    /// a second one beside it.
    /// </summary>
    [Fact]
    public async Task Sync_RevivesTheRetiredRowWhenALevelComesBack()
    {
        var retired = LevelLink(AccountId, LevelOne, isDeleted: true);
        await using (var seedContext = CreateContext())
        {
            seedContext.TB_ACCOUNT_LINK_LEVELs.Add(retired);
            seedContext.TB_ACCOUNT_LINK_TAFSILGROUPs.Add(GroupLink(AccountId, LevelOne));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext();
        await Synchronizer(new AccountCodeRepository(context)).SyncAsync(AccountId);
        await context.SaveChangesAsync();

        await using var verifyContext = CreateContext();
        var rows = await verifyContext.TB_ACCOUNT_LINK_LEVELs
            .Where(l => l.ACCOUNT_ID == AccountId)
            .ToListAsync();

        var row = Assert.Single(rows);
        Assert.Equal(retired.ID, row.ID);
        Assert.False(row.ISDELETED);
    }
}
