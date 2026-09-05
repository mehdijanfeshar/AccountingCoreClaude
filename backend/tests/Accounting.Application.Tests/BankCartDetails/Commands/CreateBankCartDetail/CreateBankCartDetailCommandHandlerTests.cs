using Accounting.Application.BankCartDetails.Commands.CreateBankCartDetail;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.BankCartDetails.Commands.CreateBankCartDetail;

public sealed class CreateBankCartDetailCommandHandlerTests
{
    private static CreateBankCartDetailCommand ValidCommand() => new(
        ReceipId: Guid.NewGuid(),
        CheckId: Guid.NewGuid(),
        BankId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        AccountNumber: "1234567890123",
        Month: "01",
        Cheqno: "12345678",
        RecivDate: "14020101",
        CheckReceiptType: true,
        Debtor: 1000m,
        Creditor: 0m,
        VahedCode: "0001",
        Year: "1402",
        CheckIncorrentId: Guid.NewGuid());

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IBankCartDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_BANKCARTDETAIL? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BANKCARTDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BANKCARTDETAIL, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.ReceipId, staged!.RECEIP_ID);
        Assert.Equal(command.CheckId, staged.CHECK_ID);
        Assert.Equal(command.BankId, staged.BANK_ID);
        Assert.Equal(command.BranchId, staged.BRANCH_ID);
        Assert.Equal(command.AccountNumber, staged.ACCOUNTNUMBER);
        Assert.Equal(command.Month, staged.MONTH);
        Assert.Equal(command.Cheqno, staged.CHEQNO);
        Assert.Equal(command.RecivDate, staged.RECIVDATE);
        Assert.Equal(command.CheckReceiptType, staged.CHECKRECEIPTTYPE);
        Assert.Equal(command.Debtor, staged.DEBTOR);
        Assert.Equal(command.Creditor, staged.CREDITOR);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.Year, staged.YEAR);
        Assert.Equal(command.CheckIncorrentId, staged.CHECK_INCORRENT_ID);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IBankCartDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_BANKCARTDETAIL? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BANKCARTDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BANKCARTDETAIL, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<IBankCartDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_BANKCARTDETAIL? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BANKCARTDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BANKCARTDETAIL, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotNull(staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IBankCartDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_BANKCARTDETAIL? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BANKCARTDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BANKCARTDETAIL, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IBankCartDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_BANKCARTDETAIL>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IBankCartDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_BANKCARTDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IBankCartDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_BANKCARTDETAIL>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
