using Accounting.Application.Accounts.Commands.UnlinkAccountCodeFromTafsilGroup;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Accounts.Commands.UnlinkAccountCodeFromTafsilGroup;

public sealed class UnlinkAccountCodeFromTafsilGroupCommandHandlerTests
{
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

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "deleter1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_ExistingLink_SetsIsDeletedTrue_StampsAuditFields_AndSavesOnce()
    {
        var accountCodeId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var link = ExistingLink(accountCodeId, linkId);
        var repository = new Mock<IAccountCodeRepository>();
        repository
            .Setup(r => r.GetTafsilGroupLinkForUpdateAsync(accountCodeId, linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter9");

        var handler = new UnlinkAccountCodeFromTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new UnlinkAccountCodeFromTafsilGroupCommand(accountCodeId, linkId), CancellationToken.None);

        Assert.True(link.ISDELETED);
        Assert.Equal("deleter9", link.CHANGEUSERID);
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

        var handler = new UnlinkAccountCodeFromTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new UnlinkAccountCodeFromTafsilGroupCommand(accountCodeId, linkId), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LinkAlreadySoftDeleted_IsIdempotent_NoExceptionAndNoSaveChanges_AndAuditFieldsUnchanged()
    {
        var accountCodeId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var updatedAt = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);
        var link = ExistingLink(accountCodeId, linkId, isDeleted: true);
        link.CHANGEUSERID = "previousEditor";
        link.UPDATEDDATE = updatedAt;

        var repository = new Mock<IAccountCodeRepository>();
        repository
            .Setup(r => r.GetTafsilGroupLinkForUpdateAsync(accountCodeId, linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("newDeleter");

        var handler = new UnlinkAccountCodeFromTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new UnlinkAccountCodeFromTafsilGroupCommand(accountCodeId, linkId), CancellationToken.None));

        Assert.Null(exception);
        Assert.True(link.ISDELETED);
        Assert.Equal("previousEditor", link.CHANGEUSERID);
        Assert.Equal(updatedAt, link.UPDATEDDATE);
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

        var handler = new UnlinkAccountCodeFromTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new UnlinkAccountCodeFromTafsilGroupCommand(accountCodeId, linkId), token);

        repository.Verify(r => r.GetTafsilGroupLinkForUpdateAsync(accountCodeId, linkId, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
