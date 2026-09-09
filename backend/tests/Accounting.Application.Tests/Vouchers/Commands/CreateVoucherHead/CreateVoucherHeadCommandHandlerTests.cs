using Accounting.Application.Common.Behaviors;
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
        Year: "1405",
        IsAutomatic: false,
        SndVahedCode: null,
        ParentHeadId: null,
        AttachFileName: null,
        AtfNum: null)
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
        var repository = new Mock<IVoucherHeadRepository>();
        var voucherDetailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_VOUCHERSHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, voucherDetailRepository.Object, unitOfWork.Object, currentUser.Object);
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
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var voucherDetailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_VOUCHERSHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, voucherDetailRepository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var voucherDetailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_VOUCHERSHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, voucherDetailRepository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotNull(staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var voucherDetailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_VOUCHERSHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, voucherDetailRepository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var voucherDetailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, voucherDetailRepository.Object, unitOfWork.Object, currentUser.Object);

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
        var voucherDetailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, voucherDetailRepository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var voucherDetailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, voucherDetailRepository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsVahedCodeFromCommandAtFaceValue()
    {
        // The handler itself just maps request.VahedCode onto the entity — it does not read
        // ICurrentUser.VahedCode directly. Forgery prevention is VahedScopeBehavior's job (see
        // the dedicated pipeline test below); this test only proves the mapping is faithful.
        var repository = new Mock<IVoucherHeadRepository>();
        var voucherDetailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_VOUCHERSHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, voucherDetailRepository.Object, unitOfWork.Object, currentUser.Object);
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
        // unconditionally with ICurrentUser.VahedCode before the handler ever sees it. This is
        // the heaviest write path in the project (composite create), so the assertion also
        // covers the head's VAHEDCODE, not just the command's own property.
        var repository = new Mock<IVoucherHeadRepository>();
        var voucherDetailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");
        TB_VOUCHERSHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVoucherHeadCommandHandler(repository.Object, voucherDetailRepository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<CreateVoucherHeadCommand, Guid>(currentUser.Object);
        var forgedCommand = ValidCommand() with { VahedCode = "9999" };

        await behavior.Handle(forgedCommand, ct => handler.Handle(forgedCommand, ct), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("0009", staged!.VAHEDCODE);
        Assert.Equal("0009", forgedCommand.VahedCode);
    }
}
