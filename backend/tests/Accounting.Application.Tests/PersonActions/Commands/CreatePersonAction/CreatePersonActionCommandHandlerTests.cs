using Accounting.Application.Common.Interfaces;
using Accounting.Application.PersonActions.Commands.CreatePersonAction;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.PersonActions.Commands.CreatePersonAction;

public sealed class CreatePersonActionCommandHandlerTests
{
    private static CreatePersonActionCommand ValidCommand() => new(
        UserName: "John Doe",
        UserId: "jdoe",
        FromDate: "14030101",
        ToDate: "14031231",
        Status: true,
        OperatorRole: true,
        VahedCode: "0100");

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IPersonActionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_PERSON_ACTION? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PERSON_ACTION>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PERSON_ACTION, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreatePersonActionCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.UserName, staged!.USERNAME);
        Assert.Equal(command.UserId, staged.USERID);
        Assert.Equal(command.FromDate, staged.FROMDATE);
        Assert.Equal(command.ToDate, staged.TODATE);
        Assert.Equal(command.Status, staged.STATUS);
        Assert.Equal(command.OperatorRole, staged.OPERATORROLE);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IPersonActionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_PERSON_ACTION? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PERSON_ACTION>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PERSON_ACTION, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreatePersonActionCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<IPersonActionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_PERSON_ACTION? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PERSON_ACTION>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PERSON_ACTION, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreatePersonActionCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotEqual(default, staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IPersonActionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_PERSON_ACTION? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PERSON_ACTION>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PERSON_ACTION, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreatePersonActionCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IPersonActionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreatePersonActionCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_PERSON_ACTION>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IPersonActionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PERSON_ACTION>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreatePersonActionCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IPersonActionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreatePersonActionCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_PERSON_ACTION>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
