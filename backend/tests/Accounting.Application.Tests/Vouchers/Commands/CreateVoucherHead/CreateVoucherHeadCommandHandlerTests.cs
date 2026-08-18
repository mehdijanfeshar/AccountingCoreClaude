using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.CreateVoucherHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.CreateVoucherHead;

public sealed class CreateVoucherHeadCommandHandlerTests
{
    private static CreateVoucherHeadCommand ValidCommand() => new(
        DocNum: "000001",
        DateDoc: "14050101",
        DocLife: true,
        HeadDesc: "سند افتتاحیه",
        Apendix: null,
        SystemTypeId: null,
        FlagState: null,
        VahedCode: "0001",
        Year: "1405",
        IsAutomatic: false,
        SndVahedCode: null,
        ParentHeadId: null,
        AttachFileName: null,
        AtfNum: null,
        AddUserId: "user1");

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TB_VOUCHERSHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, unitOfWork.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.DocNum, staged!.DOC_NUM);
        Assert.Equal(command.DateDoc, staged.DATE_DOC);
        Assert.Equal(command.DocLife, staged.DOCLIFE);
        Assert.Equal(command.HeadDesc, staged.HEAD_DESC);
        Assert.Equal(command.Apendix, staged.APENDIX);
        Assert.Equal(command.SystemTypeId, staged.SYSTEM_TYPE);
        Assert.Equal(command.FlagState, staged.FLAG_STATE);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.Year, staged.YEAR);
        Assert.Equal(command.IsAutomatic, staged.ISAUTOMATIC);
        Assert.Equal(command.SndVahedCode, staged.SNDVAHEDCODE);
        Assert.Equal(command.ParentHeadId, staged.PARENTHEAD_ID);
        Assert.Equal(command.AttachFileName, staged.ATTACHFILE_NAME);
        Assert.Equal(command.AtfNum, staged.ATF_NUM);
        Assert.Equal(command.AddUserId, staged.ADDUSERID);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TB_VOUCHERSHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotNull(staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TB_VOUCHERSHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
