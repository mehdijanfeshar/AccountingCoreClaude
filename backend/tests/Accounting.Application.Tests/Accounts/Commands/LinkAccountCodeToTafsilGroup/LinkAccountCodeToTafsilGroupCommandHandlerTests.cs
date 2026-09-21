using Accounting.Application.Accounts.Commands.Common;
using Accounting.Application.Accounts.Commands.LinkAccountCodeToTafsilGroup;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Tests.Accounts.Commands.Common;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Accounts.Commands.LinkAccountCodeToTafsilGroup;

public sealed class LinkAccountCodeToTafsilGroupCommandHandlerTests
{
    private static LinkAccountCodeToTafsilGroupCommand ValidCommand() => new(
        AccountCodeId: Guid.NewGuid(),
        LevelId: Guid.NewGuid(),
        TafsilGroupId: Guid.NewGuid());

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IAccountCodeRepository>().WithNoExistingLevelLinks();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ACCOUNT_LINK_TAFSILGROUP? staged = null;
        repository
            .Setup(r => r.AddTafsilGroupLinkAsync(It.IsAny<TB_ACCOUNT_LINK_TAFSILGROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT_LINK_TAFSILGROUP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new LinkAccountCodeToTafsilGroupCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new AccountLevelLinkSynchronizer(repository.Object, currentUser.Object));
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.AccountCodeId, staged!.ACCOUNT_ID);
        Assert.Equal(command.LevelId, staged.LEVEL_ID);
        Assert.Equal(command.TafsilGroupId, staged.TAFSILGROUP_ID);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_IsDeletedFalse_AndNonDefaultCreatedDate()
    {
        var repository = new Mock<IAccountCodeRepository>().WithNoExistingLevelLinks();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_ACCOUNT_LINK_TAFSILGROUP? staged = null;
        repository
            .Setup(r => r.AddTafsilGroupLinkAsync(It.IsAny<TB_ACCOUNT_LINK_TAFSILGROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT_LINK_TAFSILGROUP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new LinkAccountCodeToTafsilGroupCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new AccountLevelLinkSynchronizer(repository.Object, currentUser.Object));

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        Assert.False(staged.ISDELETED);
        Assert.NotEqual(default, staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndCallsSaveChangesOnce()
    {
        var repository = new Mock<IAccountCodeRepository>().WithNoExistingLevelLinks();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ACCOUNT_LINK_TAFSILGROUP? staged = null;
        repository
            .Setup(r => r.AddTafsilGroupLinkAsync(It.IsAny<TB_ACCOUNT_LINK_TAFSILGROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT_LINK_TAFSILGROUP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new LinkAccountCodeToTafsilGroupCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new AccountLevelLinkSynchronizer(repository.Object, currentUser.Object));

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(staged!.ID, result);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IAccountCodeRepository>().WithNoExistingLevelLinks();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new LinkAccountCodeToTafsilGroupCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new AccountLevelLinkSynchronizer(repository.Object, currentUser.Object));

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddTafsilGroupLinkAsync(It.IsAny<TB_ACCOUNT_LINK_TAFSILGROUP>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
