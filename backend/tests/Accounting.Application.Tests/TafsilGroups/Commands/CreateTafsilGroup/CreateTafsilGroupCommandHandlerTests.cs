using Accounting.Application.TafsilGroups.Commands.CreateTafsilGroup;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.TafsilGroups.Commands.CreateTafsilGroup;

public sealed class CreateTafsilGroupCommandHandlerTests
{
    private static CreateTafsilGroupCommand ValidCommand() => new(
        TafsilGroupCode: "001",
        TafsilGroupName: "گروه تفصیلی یک",
        PersonType: true);

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<ITafsilGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_TAFSIL_GROUP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSIL_GROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSIL_GROUP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.TafsilGroupCode, staged!.TAFSILGROUP_CODE);
        Assert.Equal(command.TafsilGroupName, staged.TAFSILGROUP_NAME);
        Assert.Equal(command.PersonType, staged.PERSONTYPE);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<ITafsilGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_TAFSIL_GROUP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSIL_GROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSIL_GROUP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<ITafsilGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_TAFSIL_GROUP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSIL_GROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSIL_GROUP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotEqual(default, staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<ITafsilGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_TAFSIL_GROUP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSIL_GROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSIL_GROUP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<ITafsilGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_TAFSIL_GROUP>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<ITafsilGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSIL_GROUP>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<ITafsilGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateTafsilGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_TAFSIL_GROUP>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
