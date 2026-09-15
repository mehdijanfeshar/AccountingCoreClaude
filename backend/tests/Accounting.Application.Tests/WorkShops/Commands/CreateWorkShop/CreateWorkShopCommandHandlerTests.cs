using Accounting.Application.WorkShops.Commands.CreateWorkShop;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.WorkShops.Commands.CreateWorkShop;

public sealed class CreateWorkShopCommandHandlerTests
{
    private static CreateWorkShopCommand ValidCommand() => new(
        AccountCodeId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        WorkShopName: "کارگاه شماره یک",
        WorkShopCode: "WS001",
        IsActive: true,
        CheckFile: null)
    {
        VahedCode = "0001",
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_WORKSHOP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_WORKSHOP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.AccountCodeId, staged!.ACCOUNTCODE_ID);
        Assert.Equal(command.BranchId, staged.BRANCH_ID);
        Assert.Equal(command.WorkShopName, staged.WORKSHOPNAME);
        Assert.Equal(command.WorkShopCode, staged.WORKSHOPCODE);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.IsActive, staged.ISACTIVE);
        Assert.Equal(command.CheckFile, staged.CHECKFILE);
    }

    [Fact]
    public async Task Handle_MapsCheckFileBytes_WhenProvided()
    {
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_WORKSHOP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_WORKSHOP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var bytes = new byte[] { 1, 2, 3, 4 };
        var command = ValidCommand() with { CheckFile = bytes };

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(bytes, staged!.CHECKFILE);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_WORKSHOP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_WORKSHOP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_WORKSHOP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_WORKSHOP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotNull(staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_WORKSHOP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_WORKSHOP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsVahedCodeFromCommandAtFaceValue()
    {
        // The handler itself just maps request.VahedCode onto the entity — it does not read
        // ICurrentUser.VahedCode directly. Forgery prevention is VahedScopeBehavior's job (see
        // the dedicated pipeline test below); this test only proves the mapping is faithful.
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_WORKSHOP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_WORKSHOP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand() with { VahedCode = "0009" };

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("0009", staged!.VAHEDCODE);
    }

    [Fact]
    public async Task Handle_ThroughVahedScopeBehavior_ClientSuppliedVahedCodeIsDiscarded_ServerValueIsWritten()
    {
        // End-to-end forgery-prevention proof: even when a "client" manages to populate
        // command.VahedCode with a forged value before dispatch, running the command through
        // VahedScopeBehavior — exactly as the real MediatR pipeline does — overwrites it
        // unconditionally with ICurrentUser.VahedCode before the handler ever sees it.
        var repository = new Mock<IWorkShopRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");
        TB_WORKSHOP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_WORKSHOP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_WORKSHOP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateWorkShopCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<CreateWorkShopCommand, Guid>(currentUser.Object);
        var forgedCommand = ValidCommand() with { VahedCode = "9999" };

        await behavior.Handle(forgedCommand, ct => handler.Handle(forgedCommand, ct), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("0009", staged!.VAHEDCODE);
        Assert.Equal("0009", forgedCommand.VahedCode);
    }
}
