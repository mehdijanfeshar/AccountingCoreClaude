using Accounting.Application.BankAccounts.Commands.CreateBankAccount;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.BankAccounts.Commands.CreateBankAccount;

public sealed class CreateBankAccountCommandHandlerTests
{
    private static CreateBankAccountCommand ValidCommand() => new(
        AccountNumber: "1234567890",
        AccountHolder: "علی رضایی",
        CardNumber: "6037991234567890",
        ShebaNumber: "IR120170000000123456789012",
        FirstAmount: 1_000_000m,
        BankId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        AccountTypeId: Guid.NewGuid(),
        AccountCodeId: Guid.NewGuid(),
        CheckFile: null,
        AccountOpeningDate: "13990101")
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
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ACCOUNT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.AccountNumber, staged!.ACCOUNTNUMBER);
        Assert.Equal(command.AccountHolder, staged.ACCOUNTHOLDER);
        Assert.Equal(command.CardNumber, staged.CARDNUMBER);
        Assert.Equal(command.ShebaNumber, staged.SHEBANUMBER);
        Assert.Equal(command.FirstAmount, staged.FIRSTAMOUNT);
        Assert.Equal(command.BankId, staged.BANK_ID);
        Assert.Equal(command.BranchId, staged.BRANCH_ID);
        Assert.Equal(command.AccountTypeId, staged.ACCOUNTTYPE_ID);
        Assert.Equal(command.AccountCodeId, staged.ACCOUNTCODE_ID);
        Assert.Equal(command.CheckFile, staged.CHECKFILE);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.AccountOpeningDate, staged.ACCOUNTOPENINGDATE);
    }

    [Fact]
    public async Task Handle_MapsCheckFileBytes_WhenProvided()
    {
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ACCOUNT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var bytes = new byte[] { 1, 2, 3, 4 };
        var command = ValidCommand() with { CheckFile = bytes };

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(bytes, staged!.CHECKFILE);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_ACCOUNT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ACCOUNT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotNull(staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ACCOUNT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsVahedCodeFromCommandAtFaceValue()
    {
        // The handler itself just maps request.VahedCode onto the entity — it does not read
        // ICurrentUser.VahedCode directly. Forgery prevention is VahedScopeBehavior's job (see
        // the dedicated pipeline test below); this test only proves the mapping is faithful.
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ACCOUNT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
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
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");
        TB_ACCOUNT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<CreateBankAccountCommand, Guid>(currentUser.Object);
        var forgedCommand = ValidCommand() with { VahedCode = "9999" };

        await behavior.Handle(forgedCommand, ct => handler.Handle(forgedCommand, ct), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("0009", staged!.VAHEDCODE);
        Assert.Equal("0009", forgedCommand.VahedCode);
    }
}
