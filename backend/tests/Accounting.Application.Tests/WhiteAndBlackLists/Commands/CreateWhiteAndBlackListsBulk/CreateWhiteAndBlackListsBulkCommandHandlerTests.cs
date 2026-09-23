using Accounting.Application.Common.Interfaces;
using Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackListsBulk;
using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Commands.CreateWhiteAndBlackListsBulk;

public sealed class CreateWhiteAndBlackListsBulkCommandHandlerTests
{
    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    private sealed record Harness(
        CreateWhiteAndBlackListsBulkCommandHandler Handler,
        List<TB_WHITEANDBLACKLIST> Staged,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IWhiteAndBlackListReadRepository> ReadRepository);

    private static Harness BuildHandler(
        IReadOnlyCollection<WhiteAndBlackListKey>? existingKeys = null,
        string userId = "user1")
    {
        var repository = new Mock<IWhiteAndBlackListRepository>();
        var readRepository = new Mock<IWhiteAndBlackListReadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var staged = new List<TB_WHITEANDBLACKLIST>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_WHITEANDBLACKLIST>(), It.IsAny<CancellationToken>()))
            .Callback<TB_WHITEANDBLACKLIST, CancellationToken>((entity, _) => staged.Add(entity))
            .Returns(Task.CompletedTask);

        readRepository
            .Setup(r => r.GetExistingKeysAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingKeys ?? []);

        var handler = new CreateWhiteAndBlackListsBulkCommandHandler(
            repository.Object,
            readRepository.Object,
            unitOfWork.Object,
            CurrentUserMock(userId).Object);

        return new Harness(handler, staged, unitOfWork, readRepository);
    }

    [Fact]
    public async Task Handle_StagesTheFullCartesianProduct()
    {
        var accounts = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var vahedTypes = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var harness = BuildHandler();

        var result = await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(accounts, vahedTypes, "14040101", "14041229", WhiteBlackListState.Allowed),
            CancellationToken.None);

        Assert.Equal(6, result.Created);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(6, harness.Staged.Count);

        var stagedPairs = harness.Staged
            .Select(e => (e.ACCOUNTCODE_ID, e.VAHEDTYPE_ID))
            .ToHashSet();
        foreach (var account in accounts)
        {
            foreach (var vahedType in vahedTypes)
            {
                Assert.Contains((account, (Guid?)vahedType), stagedPairs);
            }
        }
    }

    /// <summary>
    /// The whole grant must be one transaction — otherwise a failure halfway leaves a partial
    /// permission matrix, which is exactly the non-atomic-save problem recorded as risk #21 for
    /// the voucher form.
    /// </summary>
    [Fact]
    public async Task Handle_SavesExactlyOnceForTheWholeGrant()
    {
        var harness = BuildHandler();

        await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                [Guid.NewGuid(), Guid.NewGuid()],
                [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
                "14040101",
                "14041229",
                WhiteBlackListState.Allowed),
            CancellationToken.None);

        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStateIsAllowed_WritesTheRangeToTheAuthorizedColumnsOnly()
    {
        var harness = BuildHandler();

        await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                [Guid.NewGuid()], [Guid.NewGuid()], "14040101", "14041229", WhiteBlackListState.Allowed),
            CancellationToken.None);

        var entity = Assert.Single(harness.Staged);
        Assert.Equal("14040101", entity.FROMAUTHORIZEDDATE);
        Assert.Equal("14041229", entity.TOAUTHORIZEDDATE);
        Assert.Null(entity.FROMLIMITATIONDATE);
        Assert.Null(entity.TOLIMITATIONDATE);
        Assert.Equal(WhiteBlackListState.Allowed, entity.STATE);
    }

    [Fact]
    public async Task Handle_WhenStateIsSystemOnly_WritesTheRangeToTheLimitationColumnsOnly()
    {
        var harness = BuildHandler();

        await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                [Guid.NewGuid()], [Guid.NewGuid()], "14040101", "14041229", WhiteBlackListState.SystemOnly),
            CancellationToken.None);

        var entity = Assert.Single(harness.Staged);
        Assert.Null(entity.FROMAUTHORIZEDDATE);
        Assert.Null(entity.TOAUTHORIZEDDATE);
        Assert.Equal("14040101", entity.FROMLIMITATIONDATE);
        Assert.Equal("14041229", entity.TOLIMITATIONDATE);
        Assert.Equal(WhiteBlackListState.SystemOnly, entity.STATE);
    }

    /// <summary>
    /// Re-granting something already granted is a normal user action (a second click, a stale
    /// grid). It must be reported as skipped, not staged — staging it would hit
    /// <c>UK_WHITEANDBLACKLIST</c> and roll the whole grant back.
    /// </summary>
    [Fact]
    public async Task Handle_SkipsCombinationsThatAlreadyExistInTheDatabase()
    {
        var account = Guid.NewGuid();
        var alreadyGranted = Guid.NewGuid();
        var newVahedType = Guid.NewGuid();
        var harness = BuildHandler(
            existingKeys: [new WhiteAndBlackListKey(account, alreadyGranted, "14040101", "14041229")]);

        var result = await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                [account], [alreadyGranted, newVahedType], "14040101", "14041229", WhiteBlackListState.Allowed),
            CancellationToken.None);

        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Skipped);
        var entity = Assert.Single(harness.Staged);
        Assert.Equal(newVahedType, entity.VAHEDTYPE_ID);
    }

    /// <summary>
    /// An existing row with a DIFFERENT date range is not the same row as far as
    /// <c>UK_WHITEANDBLACKLIST</c> is concerned — the range is part of the key — so it must not
    /// suppress the new grant. Live data confirms this really happens: 291 (account, unit type)
    /// pairs already carry more than one row.
    /// </summary>
    [Fact]
    public async Task Handle_DoesNotSkipWhenOnlyTheDateRangeDiffers()
    {
        var account = Guid.NewGuid();
        var vahedType = Guid.NewGuid();
        var harness = BuildHandler(
            existingKeys: [new WhiteAndBlackListKey(account, vahedType, "14030101", "14031229")]);

        var result = await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                [account], [vahedType], "14040101", "14041229", WhiteBlackListState.Allowed),
            CancellationToken.None);

        Assert.Equal(1, result.Created);
        Assert.Equal(0, result.Skipped);
    }

    [Fact]
    public async Task Handle_CollapsesDuplicateIdsWithinTheRequest()
    {
        var account = Guid.NewGuid();
        var vahedType = Guid.NewGuid();
        var harness = BuildHandler();

        var result = await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                [account, account], [vahedType, vahedType], "14040101", "14041229", WhiteBlackListState.Allowed),
            CancellationToken.None);

        Assert.Equal(1, result.Created);
        Assert.Single(harness.Staged);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var harness = BuildHandler(userId: "srvusr01");

        await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                [Guid.NewGuid()], [Guid.NewGuid()], "14040101", "14041229", WhiteBlackListState.Allowed),
            CancellationToken.None);

        var entity = Assert.Single(harness.Staged);
        Assert.Equal("srvusr01", entity.ADDUSERID);
    }

    /// <summary>
    /// IDs must be generated application-side: the Oracle <c>sys_guid()</c> column default yields
    /// a non-dashed value that <c>GuidToChar36Converter</c> rejects on read (recorded risk #11).
    /// </summary>
    [Fact]
    public async Task Handle_GeneratesDistinctNonEmptyIds_AndReturnsThem()
    {
        var harness = BuildHandler();

        var result = await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                [Guid.NewGuid(), Guid.NewGuid()], [Guid.NewGuid()], "14040101", "14041229", WhiteBlackListState.Allowed),
            CancellationToken.None);

        Assert.Equal(2, result.CreatedIds.Count);
        Assert.DoesNotContain(Guid.Empty, result.CreatedIds);
        Assert.Equal(result.CreatedIds.Count, result.CreatedIds.Distinct().Count());
        Assert.Equal(harness.Staged.Select(e => e.ID), result.CreatedIds);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var harness = BuildHandler();

        await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                [Guid.NewGuid()], [Guid.NewGuid()], "14040101", "14041229", WhiteBlackListState.Allowed),
            CancellationToken.None);

        var entity = Assert.Single(harness.Staged);
        Assert.False(entity.ISDELETED);
        Assert.NotEqual(default, entity.CREATEDDATE);
        Assert.Null(entity.UPDATEDDATE);
        Assert.Null(entity.CHANGEUSERID);
    }

    /// <summary>
    /// The existence pre-check must be asked about the accounts being granted. If the handler
    /// passed an empty list (or the wrong one), every combination would look new and the skip
    /// logic would silently stop working.
    /// </summary>
    [Fact]
    public async Task Handle_AsksTheReadRepositoryAboutTheRequestedAccounts()
    {
        var accounts = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var harness = BuildHandler();

        await harness.Handler.Handle(
            new CreateWhiteAndBlackListsBulkCommand(
                accounts, [Guid.NewGuid()], "14040101", "14041229", WhiteBlackListState.Allowed),
            CancellationToken.None);

        harness.ReadRepository.Verify(
            r => r.GetExistingKeysAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.OrderBy(i => i).SequenceEqual(accounts.OrderBy(i => i))),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
