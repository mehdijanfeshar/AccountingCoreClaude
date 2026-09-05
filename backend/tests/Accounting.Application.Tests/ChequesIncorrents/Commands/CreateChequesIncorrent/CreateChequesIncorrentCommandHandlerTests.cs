using Accounting.Application.ChequesIncorrents.Commands.CreateChequesIncorrent;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.ChequesIncorrents.Commands.CreateChequesIncorrent;

public sealed class CreateChequesIncorrentCommandHandlerTests
{
    private static CreateChequesIncorrentCommand ValidCommand() => new(
        CheckId: Guid.NewGuid(),
        DocNum: "100001",
        DocDate: "13990101",
        CheqNo: "20000001",
        CheqDate: "13990102",
        PaperDesc: "بابت خرید کالا",
        PayTo: "شرکت الف",
        RecivDate: "13990110",
        AccountNumber: "1234567890123",
        Creditor: 500_000m,
        VahedCode: "0001",
        Year: "1399");

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IChequesIncorrentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_CHEQUES_INCORRENT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHEQUES_INCORRENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHEQUES_INCORRENT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateChequesIncorrentCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.CheckId, staged!.CHECK_ID);
        Assert.Equal(command.DocNum, staged.DOC_NUM);
        Assert.Equal(command.DocDate, staged.DOC_DATE);
        Assert.Equal(command.CheqNo, staged.CHEQ_NO);
        Assert.Equal(command.CheqDate, staged.CHEQ_DATE);
        Assert.Equal(command.PaperDesc, staged.PAPER_DESC);
        Assert.Equal(command.PayTo, staged.PAYTO);
        Assert.Equal(command.RecivDate, staged.RECIVDATE);
        Assert.Equal(command.AccountNumber, staged.ACCOUNTNUMBER);
        Assert.Equal(command.Creditor, staged.CREDITOR);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.Year, staged.YEAR);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IChequesIncorrentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_CHEQUES_INCORRENT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHEQUES_INCORRENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHEQUES_INCORRENT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateChequesIncorrentCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonDefaultCreatedDate()
    {
        var repository = new Mock<IChequesIncorrentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_CHEQUES_INCORRENT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHEQUES_INCORRENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHEQUES_INCORRENT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateChequesIncorrentCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotEqual(default, staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IChequesIncorrentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_CHEQUES_INCORRENT? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHEQUES_INCORRENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHEQUES_INCORRENT, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateChequesIncorrentCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IChequesIncorrentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateChequesIncorrentCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_CHEQUES_INCORRENT>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IChequesIncorrentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHEQUES_INCORRENT>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateChequesIncorrentCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IChequesIncorrentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateChequesIncorrentCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_CHEQUES_INCORRENT>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
