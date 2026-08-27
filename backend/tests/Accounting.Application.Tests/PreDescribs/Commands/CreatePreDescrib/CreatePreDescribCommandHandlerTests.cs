using Accounting.Application.PreDescribs.Commands.CreatePreDescrib;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.PreDescribs.Commands.CreatePreDescrib;

public sealed class CreatePreDescribCommandHandlerTests
{
    private static CreatePreDescribCommand ValidCommand() => new(
        AccountId: null,
        Descrip: "توضیحات پیش‌فرض",
        VahedCode: "0001",
        FlagVoucher: false);

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IPreDescribRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_PREDESCRIB? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PREDESCRIB>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PREDESCRIB, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreatePreDescribCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.AccountId, staged!.ACCOUNTID);
        Assert.Equal(command.Descrip, staged.DESCRIP);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.FlagVoucher, staged.FLAGVOUCHER);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IPreDescribRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_PREDESCRIB? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PREDESCRIB>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PREDESCRIB, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreatePreDescribCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IPreDescribRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_PREDESCRIB? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PREDESCRIB>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PREDESCRIB, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreatePreDescribCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IPreDescribRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreatePreDescribCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_PREDESCRIB>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IPreDescribRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PREDESCRIB>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreatePreDescribCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IPreDescribRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreatePreDescribCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_PREDESCRIB>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
