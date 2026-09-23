using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.WhiteAndBlackLists.Commands.ReactivateWhiteAndBlackList;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Commands.ReactivateWhiteAndBlackList;

public sealed class ReactivateWhiteAndBlackListCommandHandlerTests
{
    private static TB_WHITEANDBLACKLIST BlacklistedRow(bool? isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNTCODE_ID = Guid.NewGuid(),
        VAHEDTYPE_ID = Guid.NewGuid(),
        ADDUSERID = "creator",
        CREATEDDATE = DateTime.UtcNow.AddDays(-5),
        ISDELETED = isDeleted,
        STATE = WhiteBlackListState.Blacklisted,
    };

    private static (ReactivateWhiteAndBlackListCommandHandler Handler, Mock<IUnitOfWork> UnitOfWork) BuildHandler(
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

        return (new ReactivateWhiteAndBlackListCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object),
            unitOfWork);
    }

    [Fact]
    public async Task Handle_ToAllowed_WritesTheRangeToTheAuthorizedColumnsOnly()
    {
        var entity = BlacklistedRow();
        var (handler, _) = BuildHandler(entity);

        await handler.Handle(
            new ReactivateWhiteAndBlackListCommand(entity.ID, WhiteBlackListState.Allowed, "14040101", "14041229"),
            CancellationToken.None);

        Assert.Equal(WhiteBlackListState.Allowed, entity.STATE);
        Assert.Equal("14040101", entity.FROMAUTHORIZEDDATE);
        Assert.Equal("14041229", entity.TOAUTHORIZEDDATE);
        Assert.Null(entity.FROMLIMITATIONDATE);
        Assert.Null(entity.TOLIMITATIONDATE);
    }

    [Fact]
    public async Task Handle_ToSystemOnly_WritesTheRangeToTheLimitationColumnsOnly()
    {
        var entity = BlacklistedRow();
        var (handler, _) = BuildHandler(entity);

        await handler.Handle(
            new ReactivateWhiteAndBlackListCommand(entity.ID, WhiteBlackListState.SystemOnly, "14040101", "14041229"),
            CancellationToken.None);

        Assert.Equal(WhiteBlackListState.SystemOnly, entity.STATE);
        Assert.Null(entity.FROMAUTHORIZEDDATE);
        Assert.Null(entity.TOAUTHORIZEDDATE);
        Assert.Equal("14040101", entity.FROMLIMITATIONDATE);
        Assert.Equal("14041229", entity.TOLIMITATIONDATE);
    }

    /// <summary>
    /// The pair the caller did NOT choose must be cleared, not left alone. A row that used to be
    /// «مجاز» and comes back as «فقط سیستمی» would otherwise keep its old authorized window next
    /// to the new restriction window, and the grid would render both.
    /// </summary>
    [Fact]
    public async Task Handle_ClearsTheDatePairThatDoesNotMatchTheNewState()
    {
        var entity = BlacklistedRow();
        entity.FROMAUTHORIZEDDATE = "14030101";
        entity.TOAUTHORIZEDDATE = "14031229";
        var (handler, _) = BuildHandler(entity);

        await handler.Handle(
            new ReactivateWhiteAndBlackListCommand(entity.ID, WhiteBlackListState.SystemOnly, "14040101", "14041229"),
            CancellationToken.None);

        Assert.Null(entity.FROMAUTHORIZEDDATE);
        Assert.Null(entity.TOAUTHORIZEDDATE);
    }

    [Fact]
    public async Task Handle_StampsChangeUserIdFromCurrentUser_AndUpdatedDate()
    {
        var entity = BlacklistedRow();
        var (handler, _) = BuildHandler(entity, userId: "srvusr01");

        await handler.Handle(
            new ReactivateWhiteAndBlackListCommand(entity.ID, WhiteBlackListState.Allowed, "14040101", "14041229"),
            CancellationToken.None);

        Assert.Equal("srvusr01", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
    }

    [Fact]
    public async Task Handle_SavesExactlyOnce()
    {
        var entity = BlacklistedRow();
        var (handler, unitOfWork) = BuildHandler(entity);

        await handler.Handle(
            new ReactivateWhiteAndBlackListCommand(entity.ID, WhiteBlackListState.Allowed, "14040101", "14041229"),
            CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingRow_ThrowsNotFound()
    {
        var (handler, unitOfWork) = BuildHandler(existing: null);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new ReactivateWhiteAndBlackListCommand(Guid.NewGuid(), WhiteBlackListState.Allowed, "14040101", "14041229"),
            CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SoftDeletedRow_ThrowsNotFound()
    {
        var entity = BlacklistedRow(isDeleted: true);
        var (handler, _) = BuildHandler(entity);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new ReactivateWhiteAndBlackListCommand(entity.ID, WhiteBlackListState.Allowed, "14040101", "14041229"),
            CancellationToken.None));
    }
}
