using Accounting.Application.ElamHeads.Commands.UpdateElamHead;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;
using Moq;

namespace Accounting.Application.Tests.ElamHeads.Commands.UpdateElamHead;

public sealed class UpdateElamHeadCommandHandlerTests
{
    private static UpdateElamHeadCommand ValidCommand(Guid id) => new(
        Id: id,
        VoucherHeadId: Guid.NewGuid(),
        SerialNo: "SER-002",
        Code: "COD-02",
        DabirNo: "DABIR-002",
        DabirDate: "14040201",
        PrintNo: 9,
        Case: false,
        SerialNoInput: "INP-02",
        WebStat: 1,
        Date: "14040202",
        Desc: "شرح به‌روزشده",
        WorkShopId: Guid.NewGuid(),
        RcvNo: "RCV-002",
        RcvDt: "14040203",
        LstMon: "08",
        PayNo: "PAY-00002",
        DramadType: true,
        PeimanNo: "PEIMAN-02",
        WorkShopCode: "WS-002",
        WorkShopName: "کارگاه به‌روزشده",
        SendRcvVahed: "0010",
        ElamYear: "05",
        Year: "1405",
        ElamSenderId: Guid.NewGuid())
    {
        VahedCode = "0002",
    };

    private static TB_ELAMHEAD ExistingEntity(Guid id, bool? isDeleted = false) => new()
    {
        ID = id,
        VOUCHERSHEAD_ID = Guid.NewGuid(),
        ELAMH_SERIALNO = "SER-001",
        ELAMH_CODE = "COD-01",
        ELAMH_DABIRNO = "DABIR-001",
        ELAMH_DABIRDATE = "14040101",
        ELAMH_PRINTNO = 7,
        ELAMH_CASE = true,
        SERIALNO_INPUT = "INP-01",
        WEB_STAT = 2,
        ELAMH_DATE = "14040102",
        ELAMH_DESC = "شرح قدیمی",
        WORKSHOP_ID = Guid.NewGuid(),
        ELAMH_RCVNO = "RCV-001",
        ELAMH_RCVDT = "14040103",
        ELAMH_LSTMON = "07",
        PAY_NO = "PAY-00001",
        ELAMHDRAMAD_TYPE = false,
        PEIMAN_NO = "PEIMAN-01",
        ELAMH_WORKSHOPCODE = "WS-001",
        ELAMH_WORKSHOPNAME = "کارگاه قدیمی",
        ELAMH_SENDRCVVAHED = "0009",
        ELAMH_YEAR = "04",
        VAHEDCODE = "0001",
        YEAR = "1404",
        ELAMSENDERID = Guid.NewGuid(),
        ADDUSERID = "creator1",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CHANGEUSERID = null,
        UPDATEDDATE = null,
        ISDELETED = isDeleted,
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "editor1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_ExistingNonDeletedRecord_WritesEveryWritableFieldFromCommand_FieldByField()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IElamHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.VoucherHeadId, entity.VOUCHERSHEAD_ID);
        Assert.Equal(command.SerialNo, entity.ELAMH_SERIALNO);
        Assert.Equal(command.Code, entity.ELAMH_CODE);
        Assert.Equal(command.DabirNo, entity.ELAMH_DABIRNO);
        Assert.Equal(command.DabirDate, entity.ELAMH_DABIRDATE);
        Assert.Equal(command.PrintNo, entity.ELAMH_PRINTNO);
        Assert.Equal(command.Case, entity.ELAMH_CASE);
        Assert.Equal(command.SerialNoInput, entity.SERIALNO_INPUT);
        Assert.Equal(command.WebStat, entity.WEB_STAT);
        Assert.Equal(command.Date, entity.ELAMH_DATE);
        Assert.Equal(command.Desc, entity.ELAMH_DESC);
        Assert.Equal(command.WorkShopId, entity.WORKSHOP_ID);
        Assert.Equal(command.RcvNo, entity.ELAMH_RCVNO);
        Assert.Equal(command.RcvDt, entity.ELAMH_RCVDT);
        Assert.Equal(command.LstMon, entity.ELAMH_LSTMON);
        Assert.Equal(command.PayNo, entity.PAY_NO);
        Assert.Equal(command.DramadType, entity.ELAMHDRAMAD_TYPE);
        Assert.Equal(command.PeimanNo, entity.PEIMAN_NO);
        Assert.Equal(command.WorkShopCode, entity.ELAMH_WORKSHOPCODE);
        Assert.Equal(command.WorkShopName, entity.ELAMH_WORKSHOPNAME);
        Assert.Equal(command.SendRcvVahed, entity.ELAMH_SENDRCVVAHED);
        Assert.Equal(command.ElamYear, entity.ELAMH_YEAR);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.Year, entity.YEAR);
        Assert.Equal(command.ElamSenderId, entity.ELAMSENDERID);
    }

    [Fact]
    public async Task Handle_SetsChangeUserIdFromCurrentUser_AndStampsUpdatedDate()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IElamHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr02");

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal("srvusr02", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_NeverMutatesIdentityOrCreationAuditOrIsDeleted()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var originalId = entity.ID;
        var originalAddUserId = entity.ADDUSERID;
        var originalCreatedDate = entity.CREATEDDATE;
        var originalIsDeleted = entity.ISDELETED;

        var repository = new Mock<IElamHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal(originalId, entity.ID);
        Assert.Equal(originalAddUserId, entity.ADDUSERID);
        Assert.Equal(originalCreatedDate, entity.CREATEDDATE);
        Assert.Equal(originalIsDeleted, entity.ISDELETED);
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IElamHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_ELAMHEAD?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordIsSoftDeleted_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: true);
        var repository = new Mock<IElamHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordWithNullIsDeleted_IsTreatedAsNotDeleted_AndUpdateSucceeds()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: null);
        var repository = new Mock<IElamHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var command = ValidCommand(id);

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.Code, entity.ELAMH_CODE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingRecord_CallsSaveChangesExactlyOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IElamHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IElamHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsVahedCodeFromCommandAtFaceValue()
    {
        // The handler itself just maps request.VahedCode onto the entity — it does not read
        // ICurrentUser.VahedCode directly. Forgery prevention is VahedScopeBehavior's job (see
        // the dedicated pipeline test below); this test only proves the mapping is faithful.
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IElamHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id) with { VahedCode = "0009" };

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal("0009", entity.VAHEDCODE);
    }

    [Fact]
    public async Task Handle_ThroughVahedScopeBehavior_ClientSuppliedVahedCodeIsDiscarded_ServerValueIsWritten()
    {
        // End-to-end forgery-prevention proof: even when a "client" manages to populate
        // command.VahedCode with a forged value before dispatch, running the command through
        // VahedScopeBehavior — exactly as the real MediatR pipeline does — overwrites it
        // unconditionally with ICurrentUser.VahedCode before the handler ever sees it.
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IElamHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");

        var handler = new UpdateElamHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<UpdateElamHeadCommand, Unit>(currentUser.Object);
        var forgedCommand = ValidCommand(id) with { VahedCode = "9999" };

        await behavior.Handle(
            forgedCommand,
            async ct =>
            {
                await handler.Handle(forgedCommand, ct);
                return Unit.Value;
            },
            CancellationToken.None);

        Assert.Equal("0009", entity.VAHEDCODE);
        Assert.Equal("0009", forgedCommand.VahedCode);
    }
}
