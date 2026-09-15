using Accounting.Application.BillLogs.Commands.CreateBillLog;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.BillLogs.Commands.CreateBillLog;

public sealed class CreateBillLogCommandHandlerTests
{
    private static CreateBillLogCommand ValidCommand() => new(
        LogDesc: "invoice processed",
        LogDate: "14030101",
        Year: "1403")
    {
        VahedCode = "0100",
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
        var repository = new Mock<IBillLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_BILL_LOG? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BILL_LOG>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BILL_LOG, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBillLogCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.LogDesc, staged!.LOG_DESC);
        Assert.Equal(command.LogDate, staged.LOG_DATE);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.Year, staged.YEAR);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IBillLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_BILL_LOG? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BILL_LOG>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BILL_LOG, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBillLogCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<IBillLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_BILL_LOG? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BILL_LOG>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BILL_LOG, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBillLogCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotEqual(default, staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IBillLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_BILL_LOG? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BILL_LOG>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BILL_LOG, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBillLogCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IBillLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateBillLogCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_BILL_LOG>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IBillLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BILL_LOG>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateBillLogCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IBillLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateBillLogCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_BILL_LOG>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsVahedCodeFromCommandAtFaceValue()
    {
        // The handler itself just maps request.VahedCode onto the entity — it does not read
        // ICurrentUser.VahedCode directly. Forgery prevention is VahedScopeBehavior's job (see
        // the dedicated pipeline test below); this test only proves the mapping is faithful.
        var repository = new Mock<IBillLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_BILL_LOG? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BILL_LOG>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BILL_LOG, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBillLogCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
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
        var repository = new Mock<IBillLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");
        TB_BILL_LOG? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BILL_LOG>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BILL_LOG, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBillLogCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<CreateBillLogCommand, Guid>(currentUser.Object);
        var forgedCommand = ValidCommand() with { VahedCode = "9999" };

        await behavior.Handle(forgedCommand, ct => handler.Handle(forgedCommand, ct), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("0009", staged!.VAHEDCODE);
        Assert.Equal("0009", forgedCommand.VahedCode);
    }
}
