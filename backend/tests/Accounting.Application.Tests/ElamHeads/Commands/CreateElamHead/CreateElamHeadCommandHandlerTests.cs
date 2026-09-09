using Accounting.Application.ElamHeads.Commands.CreateElamHead;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.ElamHeads.Commands.CreateElamHead;

public sealed class CreateElamHeadCommandHandlerTests
{
    /// <summary>
    /// Every field carries a distinct, recognizable value so that a copy/paste mismap between
    /// two similarly-named <c>ELAMH_*</c> columns in the handler is caught by
    /// <see cref="Handle_MapsEveryWritableFieldOntoStagedEntity_FieldByField"/> below.
    /// </summary>
    private static CreateElamHeadCommand ValidCommand() => new(
        VoucherHeadId: Guid.NewGuid(),
        SerialNo: "SER-001",
        Code: "COD-01",
        DabirNo: "DABIR-001",
        DabirDate: "14040101",
        PrintNo: 7,
        Case: true,
        SerialNoInput: "INP-01",
        WebStat: 2,
        Date: "14040102",
        Desc: "شرح اعلاميه تستی",
        WorkShopId: Guid.NewGuid(),
        RcvNo: "RCV-001",
        RcvDt: "14040103",
        LstMon: "07",
        PayNo: "PAY-00001",
        DramadType: false,
        PeimanNo: "PEIMAN-01",
        WorkShopCode: "WS-001",
        WorkShopName: "کارگاه تستی",
        SendRcvVahed: "0009",
        ElamYear: "04",
        Year: "1404",
        ElamSenderId: Guid.NewGuid())
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
    public async Task Handle_MapsEveryWritableFieldOntoStagedEntity_FieldByField()
    {
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ELAMHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.VoucherHeadId, staged!.VOUCHERSHEAD_ID);
        Assert.Equal(command.SerialNo, staged.ELAMH_SERIALNO);
        Assert.Equal(command.Code, staged.ELAMH_CODE);
        Assert.Equal(command.DabirNo, staged.ELAMH_DABIRNO);
        Assert.Equal(command.DabirDate, staged.ELAMH_DABIRDATE);
        Assert.Equal(command.PrintNo, staged.ELAMH_PRINTNO);
        Assert.Equal(command.Case, staged.ELAMH_CASE);
        Assert.Equal(command.SerialNoInput, staged.SERIALNO_INPUT);
        Assert.Equal(command.WebStat, staged.WEB_STAT);
        Assert.Equal(command.Date, staged.ELAMH_DATE);
        Assert.Equal(command.Desc, staged.ELAMH_DESC);
        Assert.Equal(command.WorkShopId, staged.WORKSHOP_ID);
        Assert.Equal(command.RcvNo, staged.ELAMH_RCVNO);
        Assert.Equal(command.RcvDt, staged.ELAMH_RCVDT);
        Assert.Equal(command.LstMon, staged.ELAMH_LSTMON);
        Assert.Equal(command.PayNo, staged.PAY_NO);
        Assert.Equal(command.DramadType, staged.ELAMHDRAMAD_TYPE);
        Assert.Equal(command.PeimanNo, staged.PEIMAN_NO);
        Assert.Equal(command.WorkShopCode, staged.ELAMH_WORKSHOPCODE);
        Assert.Equal(command.WorkShopName, staged.ELAMH_WORKSHOPNAME);
        Assert.Equal(command.SendRcvVahed, staged.ELAMH_SENDRCVVAHED);
        Assert.Equal(command.ElamYear, staged.ELAMH_YEAR);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.Year, staged.YEAR);
        Assert.Equal(command.ElamSenderId, staged.ELAMSENDERID);
    }

    [Fact]
    public async Task Handle_ElamYearAndYear_AreNotSwapped()
    {
        // Regression guard for the specific copy/paste risk the task called out: ELAMH_YEAR
        // and YEAR are both 4-char-ish year-like strings that a careless handler could swap.
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ELAMHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand() with { ElamYear = "AA", Year = "BBBB" };

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("AA", staged!.ELAMH_YEAR);
        Assert.Equal("BBBB", staged.YEAR);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_ELAMHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ELAMHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotNull(staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ELAMHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_AllFieldsNull_Passes_EveryColumnIsNullable()
    {
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ELAMHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = new CreateElamHeadCommand(
            VoucherHeadId: null,
            SerialNo: null,
            Code: null,
            DabirNo: null,
            DabirDate: null,
            PrintNo: null,
            Case: null,
            SerialNoInput: null,
            WebStat: null,
            Date: null,
            Desc: null,
            WorkShopId: null,
            RcvNo: null,
            RcvDt: null,
            LstMon: null,
            PayNo: null,
            DramadType: null,
            PeimanNo: null,
            WorkShopCode: null,
            WorkShopName: null,
            SendRcvVahed: null,
            ElamYear: null,
            Year: null,
            ElamSenderId: null)
        {
            VahedCode = "0001",
        };

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Null(staged!.ELAMH_SERIALNO);
        Assert.Null(staged.ELAMH_CASE);
        Assert.Null(staged.ELAMHDRAMAD_TYPE);
    }

    [Fact]
    public async Task Handle_MapsVahedCodeFromCommandAtFaceValue()
    {
        // The handler itself just maps request.VahedCode onto the entity — it does not read
        // ICurrentUser.VahedCode directly. Forgery prevention is VahedScopeBehavior's job (see
        // the dedicated pipeline test below); this test only proves the mapping is faithful.
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_ELAMHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
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
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");
        TB_ELAMHEAD? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<CreateElamHeadCommand, Guid>(currentUser.Object);
        var forgedCommand = ValidCommand() with { VahedCode = "9999" };

        await behavior.Handle(forgedCommand, ct => handler.Handle(forgedCommand, ct), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("0009", staged!.VAHEDCODE);
        Assert.Equal("0009", forgedCommand.VahedCode);
    }
}
