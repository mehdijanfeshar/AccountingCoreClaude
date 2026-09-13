using Accounting.Application.Accounts.Commands.UpdateAccountTafsilGroupLink;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Accounts.Commands.UpdateAccountTafsilGroupLink;

public sealed class UpdateAccountTafsilGroupLinkCommandHandlerTests
{
    private static UpdateAccountTafsilGroupLinkCommand ValidCommand(Guid accountCodeId, Guid linkId) => new(
        AccountCodeId: accountCodeId,
        LinkId: linkId,
        LevelId: Guid.NewGuid(),
        TafsilGroupId: Guid.NewGuid());

    private static TB_ACCOUNT_LINK_TAFSILGROUP ExistingLink(Guid accountCodeId, Guid linkId, bool isDeleted = false) => new()
    {
        ID = linkId,
        ACCOUNT_ID = accountCodeId,
        LEVEL_ID = Guid.NewGuid(),
        TAFSILGROUP_ID = Guid.NewGuid(),
        ADDUSERID = "creator1",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = isDeleted,
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "editor1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_ExistingLink_WritesLevelIdAndTafsilGroupIdFromCommand()
    {
        var accountCodeId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var link = ExistingLink(accountCodeId, linkId);
        var repository = new Mock<IAccountCodeRepository>();
        repository
            .Setup(r => r.GetTafsilGroupLinkForUpdateAsync(accountCodeId, linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateAccountTafsilGroupLinkCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(accountCodeId, linkId);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.LevelId, link.LEVEL_ID);
        Assert.Equal(command.TafsilGroupId, link.TAFSILGROUP_ID);
    }

    [Fact]
    public async Task Handle_SetsChangeUserIdFromCurrentUser_AndStampsUpdatedDate()
    {
        var accountCodeId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var link = ExistingLink(accountCodeId, linkId);
        var repository = new Mock<IAccountCodeRepository>();
        repository
            .Setup(r => r.GetTafsilGroupLinkForUpdateAsync(accountCodeId, linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr02");

        var handler = new UpdateAccountTafsilGroupLinkCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(accountCodeId, linkId), CancellationToken.None);

        Assert.Equal("srvusr02", link.CHANGEUSERID);
        Assert.NotNull(link.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LinkDoesNotExistUnderThatAccount_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var accountCodeId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var repository = new Mock<IAccountCodeRepository>();
        repository
            .Setup(r => r.GetTafsilGroupLinkForUpdateAsync(accountCodeId, linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TB_ACCOUNT_LINK_TAFSILGROUP?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateAccountTafsilGroupLinkCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(ValidCommand(accountCodeId, linkId), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LinkIsSoftDeleted_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var accountCodeId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var link = ExistingLink(accountCodeId, linkId, isDeleted: true);
        var repository = new Mock<IAccountCodeRepository>();
        repository
            .Setup(r => r.GetTafsilGroupLinkForUpdateAsync(accountCodeId, linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateAccountTafsilGroupLinkCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(ValidCommand(accountCodeId, linkId), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var accountCodeId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var link = ExistingLink(accountCodeId, linkId);
        var repository = new Mock<IAccountCodeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetTafsilGroupLinkForUpdateAsync(accountCodeId, linkId, token)).ReturnsAsync(link);

        var handler = new UpdateAccountTafsilGroupLinkCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(accountCodeId, linkId), token);

        repository.Verify(r => r.GetTafsilGroupLinkForUpdateAsync(accountCodeId, linkId, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
