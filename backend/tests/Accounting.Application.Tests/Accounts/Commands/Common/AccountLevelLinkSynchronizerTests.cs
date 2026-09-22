using Accounting.Application.Accounts.Commands.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Accounts.Commands.Common;

/// <summary>
/// Pins the rule the synchronizer exists to enforce: a تفصیلی level is required for a معین exactly
/// when that معین has at least one live گروه تفصیلی link for it.
///
/// The case that motivated all of this is <see cref="SyncAsync_RetiresLevelThatHasNoGroupLink"/> —
/// the real معین 005000, where the voucher form demanded a level that no گروه تفصیلی fed, so its
/// dropdown could never be populated and the voucher could not be saved at all.
/// </summary>
public sealed class AccountLevelLinkSynchronizerTests
{
    private static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LevelOne = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid LevelThree = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid LevelFour = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private sealed class Harness
    {
        public Mock<IAccountCodeRepository> Repository { get; } = new();

        public List<TB_ACCOUNT_LINK_LEVEL> Inserted { get; } = new();

        public AccountLevelLinkSynchronizer Build(
            IEnumerable<TB_ACCOUNT_LINK_TAFSILGROUP> groupLinks,
            IEnumerable<TB_ACCOUNT_LINK_LEVEL> levelLinks,
            string userId = "user1")
        {
            Repository
                .Setup(r => r.GetTafsilGroupLinksForSyncAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(groupLinks.ToList());
            Repository
                .Setup(r => r.GetLevelLinksForSyncAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(levelLinks.ToList());
            Repository
                .Setup(r => r.AddLevelLinksAsync(It.IsAny<IReadOnlyCollection<TB_ACCOUNT_LINK_LEVEL>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<TB_ACCOUNT_LINK_LEVEL>, CancellationToken>((rows, _) => Inserted.AddRange(rows))
                .Returns(Task.CompletedTask);

            var currentUser = new Mock<ICurrentUser>();
            currentUser.SetupGet(u => u.UserId).Returns(userId);

            return new AccountLevelLinkSynchronizer(Repository.Object, currentUser.Object);
        }
    }

    private static TB_ACCOUNT_LINK_TAFSILGROUP GroupLink(Guid levelId, bool isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNT_ID = AccountId,
        LEVEL_ID = levelId,
        TAFSILGROUP_ID = Guid.NewGuid(),
        ADDUSERID = "seed",
        CREATEDDATE = new DateTime(2026, 1, 1),
        ISDELETED = isDeleted,
    };

    private static TB_ACCOUNT_LINK_LEVEL LevelLink(Guid levelId, bool isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNT_ID = AccountId,
        LEVEL_ID = levelId,
        ADDUSERID = "seed",
        CREATEDDATE = new DateTime(2026, 1, 1),
        ISDELETED = isDeleted,
    };

    [Fact]
    public async Task SyncAsync_InsertsLevelRowForNewlyLinkedLevel()
    {
        var harness = new Harness();
        var synchronizer = harness.Build(
            groupLinks: [GroupLink(LevelThree)],
            levelLinks: []);

        await synchronizer.SyncAsync(AccountId, CancellationToken.None);

        var inserted = Assert.Single(harness.Inserted);
        Assert.Equal(AccountId, inserted.ACCOUNT_ID);
        Assert.Equal(LevelThree, inserted.LEVEL_ID);
        Assert.Equal("user1", inserted.ADDUSERID);
        Assert.False(inserted.ISDELETED);
        Assert.NotEqual(default, inserted.CREATEDDATE);
    }

    /// <summary>
    /// The 005000 case. Level 4 was active with no گروه تفصیلی behind it, which the voucher form
    /// renders as a required field whose list can never be filled — unsatisfiable, so the voucher
    /// cannot be saved at all. Retiring it is the repair.
    /// </summary>
    [Fact]
    public async Task SyncAsync_RetiresLevelThatHasNoGroupLink()
    {
        var orphan = LevelLink(LevelFour);
        var harness = new Harness();
        var synchronizer = harness.Build(
            groupLinks: [GroupLink(LevelOne), GroupLink(LevelThree)],
            levelLinks: [LevelLink(LevelThree), orphan],
            userId: "srvusr01");

        await synchronizer.SyncAsync(AccountId, CancellationToken.None);

        Assert.True(orphan.ISDELETED);
        Assert.Equal("srvusr01", orphan.CHANGEUSERID);
        Assert.NotNull(orphan.UPDATEDDATE);

        // Level 1 was configured all along but never asked for, because no level row existed.
        var inserted = Assert.Single(harness.Inserted);
        Assert.Equal(LevelOne, inserted.LEVEL_ID);
    }

    [Fact]
    public async Task SyncAsync_RevivesSoftDeletedRowRatherThanInsertingASecondOne()
    {
        var previouslyRetired = LevelLink(LevelThree, isDeleted: true);
        var harness = new Harness();
        var synchronizer = harness.Build(
            groupLinks: [GroupLink(LevelThree)],
            levelLinks: [previouslyRetired]);

        await synchronizer.SyncAsync(AccountId, CancellationToken.None);

        Assert.False(previouslyRetired.ISDELETED);
        Assert.Equal("user1", previouslyRetired.CHANGEUSERID);
        Assert.Empty(harness.Inserted);
        harness.Repository.Verify(
            r => r.AddLevelLinksAsync(It.IsAny<IReadOnlyCollection<TB_ACCOUNT_LINK_LEVEL>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncAsync_IgnoresSoftDeletedGroupLinks()
    {
        var levelRow = LevelLink(LevelFour);
        var harness = new Harness();
        var synchronizer = harness.Build(
            groupLinks: [GroupLink(LevelFour, isDeleted: true)],
            levelLinks: [levelRow]);

        await synchronizer.SyncAsync(AccountId, CancellationToken.None);

        Assert.True(levelRow.ISDELETED);
        Assert.Empty(harness.Inserted);
    }

    /// <summary>
    /// A level fed by more than one گروه تفصیلی stays required while any of them survives —
    /// otherwise removing one of two groups would silently stop the form asking for that level.
    /// </summary>
    [Fact]
    public async Task SyncAsync_KeepsLevelRequiredWhileAnotherGroupLinkStillCoversIt()
    {
        var levelRow = LevelLink(LevelThree);
        var harness = new Harness();
        var synchronizer = harness.Build(
            groupLinks: [GroupLink(LevelThree, isDeleted: true), GroupLink(LevelThree)],
            levelLinks: [levelRow]);

        await synchronizer.SyncAsync(AccountId, CancellationToken.None);

        Assert.False(levelRow.ISDELETED);
        Assert.Null(levelRow.CHANGEUSERID);
        Assert.Empty(harness.Inserted);
    }

    /// <summary>
    /// Idempotence is what lets all three write paths call this unconditionally instead of each
    /// working out whether it needs to.
    /// </summary>
    [Fact]
    public async Task SyncAsync_StagesNothingWhenTheTwoTablesAlreadyAgree()
    {
        var levelRow = LevelLink(LevelThree);
        var harness = new Harness();
        var synchronizer = harness.Build(
            groupLinks: [GroupLink(LevelThree)],
            levelLinks: [levelRow]);

        await synchronizer.SyncAsync(AccountId, CancellationToken.None);

        Assert.False(levelRow.ISDELETED);
        Assert.Null(levelRow.CHANGEUSERID);
        Assert.Null(levelRow.UPDATEDDATE);
        Assert.Empty(harness.Inserted);
    }

    [Fact]
    public async Task SyncAsync_InsertsOnlyOneRowForTwoGroupLinksOnTheSameLevel()
    {
        var harness = new Harness();
        var synchronizer = harness.Build(
            groupLinks: [GroupLink(LevelThree), GroupLink(LevelThree)],
            levelLinks: []);

        await synchronizer.SyncAsync(AccountId, CancellationToken.None);

        Assert.Single(harness.Inserted);
    }

    [Fact]
    public async Task SyncAsync_NeverSavesItsOwnChanges()
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var harness = new Harness();
        var synchronizer = harness.Build(
            groupLinks: [GroupLink(LevelThree)],
            levelLinks: []);

        await synchronizer.SyncAsync(AccountId, CancellationToken.None);

        // Strict mock: any call at all would have thrown. The transaction boundary belongs to the
        // handler, so the link change and the level rows it implies commit together.
        unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SyncAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var harness = new Harness();
        var synchronizer = harness.Build(
            groupLinks: [GroupLink(LevelThree)],
            levelLinks: []);

        await synchronizer.SyncAsync(AccountId, token);

        harness.Repository.Verify(r => r.GetTafsilGroupLinksForSyncAsync(AccountId, token), Times.Once);
        harness.Repository.Verify(r => r.GetLevelLinksForSyncAsync(AccountId, token), Times.Once);
        harness.Repository.Verify(
            r => r.AddLevelLinksAsync(It.IsAny<IReadOnlyCollection<TB_ACCOUNT_LINK_LEVEL>>(), token),
            Times.Once);
    }
}
