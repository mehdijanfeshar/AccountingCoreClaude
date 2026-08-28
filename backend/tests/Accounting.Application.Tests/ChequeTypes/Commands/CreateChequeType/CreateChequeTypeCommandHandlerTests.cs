using Accounting.Application.ChequeTypes.Commands.CreateChequeType;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.ChequeTypes.Commands.CreateChequeType;

public sealed class CreateChequeTypeCommandHandlerTests
{
    private static CreateChequeTypeCommand ValidCommand() => new(
        ChequeTypeTitle: "Standard",
        ChequeWidth: 200,
        ChequeHeight: 90,
        ChequeImage: new byte[] { 1, 2, 3 },
        ChequeAdateFont: "Arial-10",
        ChequeAdateLeft: 10,
        ChequeAdateTop: 11,
        ChequeAdateWidth: 12,
        ChequeNdateFont: "Arial-11",
        ChequeNdateLeft: 20,
        ChequeNdateTop: 21,
        ChequeNdateWidth: 22,
        ChequeAamountFont: "Arial-12",
        ChequeAamountLeft: 30,
        ChequeAamountTop: 31,
        ChequeAamountWidth: 32,
        ChequeLamountFont: "Arial-13",
        ChequeLamountLeft: 40,
        ChequeLamountTop: 41,
        ChequeLamountWidth: 42,
        ChequeNamountFont: "Arial-14",
        ChequeNamountLeft: 50,
        ChequeNamountTop: 51,
        ChequeNamountWidth: 52,
        ChequeDescribe1Font: "Arial-15",
        ChequeDescribe1Left: 60,
        ChequeDescribe1Top: 61,
        ChequeDescribe1Width: 62,
        ChequeDescribe2Font: "Arial-16",
        ChequeDescribe2Left: 70,
        ChequeDescribe2Top: 71,
        ChequeDescribe2Width: 72,
        ChequeBreaklineFont: "Arial-17",
        ChequeBreaklineLeft: 80,
        ChequeBreaklineTop: 81,
        ChequeBreaklineWidth: 82,
        PrinterMargineTop: 5,
        PrinterMargineLeft: 6,
        PrinterType: "HP LaserJet",
        Year: "1403",
        VahedCode: "0100");

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsRepresentativeSpreadOfCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IChequeTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_CHECK_TYPE? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHECK_TYPE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHECK_TYPE, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.ChequeTypeTitle, staged!.CHEQUE_TYPE_TITLE);
        Assert.Equal(command.ChequeWidth, staged.CHEQUE_WIDTH);
        Assert.Equal(command.ChequeHeight, staged.CHEQUE_HEIGHT);
        Assert.Equal(command.ChequeImage, staged.CHEQUE_IMAGE);
        Assert.Equal(command.ChequeAdateFont, staged.CHEQUE_ADATE_FONT);
        Assert.Equal(command.ChequeAdateLeft, staged.CHEQUE_ADATE_LEFT);
        Assert.Equal(command.ChequeAdateTop, staged.CHEQUE_ADATE_TOP);
        Assert.Equal(command.ChequeAdateWidth, staged.CHEQUE_ADATE_WIDTH);
        Assert.Equal(command.ChequeNdateFont, staged.CHEQUE_NDATE_FONT);
        Assert.Equal(command.ChequeNdateLeft, staged.CHEQUE_NDATE_LEFT);
        Assert.Equal(command.ChequeNdateTop, staged.CHEQUE_NDATE_TOP);
        Assert.Equal(command.ChequeNdateWidth, staged.CHEQUE_NDATE_WIDTH);
        Assert.Equal(command.ChequeAamountFont, staged.CHEQUE_AAMOUNT_FONT);
        Assert.Equal(command.ChequeAamountLeft, staged.CHEQUE_AAMOUNT_LEFT);
        Assert.Equal(command.ChequeAamountTop, staged.CHEQUE_AAMOUNT_TOP);
        Assert.Equal(command.ChequeAamountWidth, staged.CHEQUE_AAMOUNT_WIDTH);
        Assert.Equal(command.ChequeLamountFont, staged.CHEQUE_LAMOUNT_FONT);
        Assert.Equal(command.ChequeLamountLeft, staged.CHEQUE_LAMOUNT_LEFT);
        Assert.Equal(command.ChequeLamountTop, staged.CHEQUE_LAMOUNT_TOP);
        Assert.Equal(command.ChequeLamountWidth, staged.CHEQUE_LAMOUNT_WIDTH);
        Assert.Equal(command.ChequeNamountFont, staged.CHEQUE_NAMOUNT_FONT);
        Assert.Equal(command.ChequeNamountLeft, staged.CHEQUE_NAMOUNT_LEFT);
        Assert.Equal(command.ChequeNamountTop, staged.CHEQUE_NAMOUNT_TOP);
        Assert.Equal(command.ChequeNamountWidth, staged.CHEQUE_NAMOUNT_WIDTH);
        Assert.Equal(command.ChequeDescribe1Font, staged.CHEQUE_DESCRIBE1_FONT);
        Assert.Equal(command.ChequeDescribe1Left, staged.CHEQUE_DESCRIBE1_LEFT);
        Assert.Equal(command.ChequeDescribe1Top, staged.CHEQUE_DESCRIBE1_TOP);
        Assert.Equal(command.ChequeDescribe1Width, staged.CHEQUE_DESCRIBE1_WIDTH);
        Assert.Equal(command.ChequeDescribe2Font, staged.CHEQUE_DESCRIBE2_FONT);
        Assert.Equal(command.ChequeDescribe2Left, staged.CHEQUE_DESCRIBE2_LEFT);
        Assert.Equal(command.ChequeDescribe2Top, staged.CHEQUE_DESCRIBE2_TOP);
        Assert.Equal(command.ChequeDescribe2Width, staged.CHEQUE_DESCRIBE2_WIDTH);
        Assert.Equal(command.ChequeBreaklineFont, staged.CHEQUE_BREAKLINE_FONT);
        Assert.Equal(command.ChequeBreaklineLeft, staged.CHEQUE_BREAKLINE_LEFT);
        Assert.Equal(command.ChequeBreaklineTop, staged.CHEQUE_BREAKLINE_TOP);
        Assert.Equal(command.ChequeBreaklineWidth, staged.CHEQUE_BREAKLINE_WIDTH);
        Assert.Equal(command.PrinterMargineTop, staged.PRINTER_MARGINE_TOP);
        Assert.Equal(command.PrinterMargineLeft, staged.PRINTER_MARGINE_LEFT);
        Assert.Equal(command.PrinterType, staged.PRINTER_TYPE);
        Assert.Equal(command.Year, staged.YEAR);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IChequeTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_CHECK_TYPE? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHECK_TYPE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHECK_TYPE, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonDefaultCreatedDate()
    {
        var repository = new Mock<IChequeTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_CHECK_TYPE? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHECK_TYPE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHECK_TYPE, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotEqual(default, staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IChequeTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_CHECK_TYPE? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHECK_TYPE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHECK_TYPE, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IChequeTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_CHECK_TYPE>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IChequeTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHECK_TYPE>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IChequeTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_CHECK_TYPE>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_AllFieldsNull_Passes_OnlyYearAndVahedCodeRequired()
    {
        var repository = new Mock<IChequeTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_CHECK_TYPE? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_CHECK_TYPE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHECK_TYPE, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = new CreateChequeTypeCommand(
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null,
            null,
            "1403",
            "0100");

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Null(staged!.CHEQUE_TYPE_TITLE);
        Assert.Null(staged.CHEQUE_IMAGE);
        Assert.Equal("1403", staged.YEAR);
        Assert.Equal("0100", staged.VAHEDCODE);
    }
}
