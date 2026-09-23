using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.WhiteAndBlackLists.Commands.BlacklistWhiteAndBlackList;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Commands.BlacklistWhiteAndBlackList;

public sealed class BlacklistWhiteAndBlackListCommandHandlerTests
{
    private static TB_WHITEANDBLACKLIST ExistingRow(bool? isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNTCODE_ID = Guid.NewGuid(),
        VAHEDTYPE_ID = Guid.NewGuid(),
        ADDUSERID = "creator",
        CREATEDDATE = DateTime.UtcNow.AddDays(-5),
        ISDELETED = isDeleted,
        FROMAUTHORIZEDDATE = "14040101",
        TOAUTHORIZEDDATE = "14041229",
        FROMLIMITATIONDATE = "14050101",
        TOLIMITATIONDATE = "14051229",
        STATE = WhiteBlackListState.Allowed,
    };

    private static (BlacklistWhiteAndBlackListCommandHandler Handler, Mock<IUnitOfWork> UnitOfWork) BuildHandler(
        TB_WHITEANDBLACKLIST? existing,
        string userId = "user1")
    {
        var repository = new Mock<IWhiteAndBlackListRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);

        return (new BlacklistWhiteAndBlackListCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object),
            unitOfWork);
    }

    [Fact]
    public async Task Handle_SetsStateToBlacklisted()
    {
        var entity = ExistingRow();
        var (handler, _) = BuildHandler(entity);

        await handler.Handle(new BlacklistWhiteAndBlackListCommand(entity.ID), CancellationToken.None);

        Assert.Equal(WhiteBlackListState.Blacklisted, entity.STATE);
    }

    /// <summary>
    /// All four date columns, not just the pair in use. A blacklisted row has neither an
    /// authorized window nor a restriction window; leaving either populated would produce a row
    /// whose dates contradict its state. Ported from the reference project's
    /// <c>ChangeStateToBlackListed</c>.
    /// </summary>
    [Fact]
    public async Task Handle_ClearsAllFourDateColumns()
    {
        var entity = ExistingRow();
        var (handler, _) = BuildHandler(entity);

        await handler.Handle(new BlacklistWhiteAndBlackListCommand(entity.ID), CancellationToken.None);

        Assert.Null(entity.FROMAUTHORIZEDDATE);
        Assert.Null(entity.TOAUTHORIZEDDATE);
        Assert.Null(entity.FROMLIMITATIONDATE);
        Assert.Null(entity.TOLIMITATIONDATE);
    }

    [Fact]
    public async Task Handle_StampsChangeUserIdFromCurrentUser_AndUpdatedDate()
    {
        var entity = ExistingRow();
        var (handler, _) = BuildHandler(entity, userId: "srvusr01");

        await handler.Handle(new BlacklistWhiteAndBlackListCommand(entity.ID), CancellationToken.None);

        Assert.Equal("srvusr01", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
    }

    [Fact]
    public async Task Handle_SavesExactlyOnce()
    {
        var entity = ExistingRow();
        var (handler, unitOfWork) = BuildHandler(entity);

        await handler.Handle(new BlacklistWhiteAndBlackListCommand(entity.ID), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingRow_ThrowsNotFound()
    {
        var (handler, unitOfWork) = BuildHandler(existing: null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new BlacklistWhiteAndBlackListCommand(Guid.NewGuid()), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SoftDeletedRow_ThrowsNotFound()
    {
        var entity = ExistingRow(isDeleted: true);
        var (handler, _) = BuildHandler(entity);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new BlacklistWhiteAndBlackListCommand(entity.ID), CancellationToken.None));
    }

    /// <summary>
    /// ISDELETED is <c>bool?</c> on this table, so NULL must count as "not deleted" — the same
    /// rule the read side's <c>ISDELETED != true</c> filter applies. Treating NULL as deleted here
    /// would make legacy rows permanently un-blacklistable.
    /// </summary>
    [Fact]
    public async Task Handle_NullIsDeleted_IsTreatedAsNotDeleted()
    {
        var entity = ExistingRow(isDeleted: null);
        var (handler, _) = BuildHandler(entity);

        await handler.Handle(new BlacklistWhiteAndBlackListCommand(entity.ID), CancellationToken.None);

        Assert.Equal(WhiteBlackListState.Blacklisted, entity.STATE);
    }

    /// <summary>
    /// Idempotent by design: a second click on a stale grid must not fail for a state the user
    /// already wanted.
    /// </summary>
    [Fact]
    public async Task Handle_AlreadyBlacklistedRow_Succeeds()
    {
        var entity = ExistingRow();
        entity.STATE = WhiteBlackListState.Blacklisted;
        var (handler, _) = BuildHandler(entity);

        await handler.Handle(new BlacklistWhiteAndBlackListCommand(entity.ID), CancellationToken.None);

        Assert.Equal(WhiteBlackListState.Blacklisted, entity.STATE);
    }
}
