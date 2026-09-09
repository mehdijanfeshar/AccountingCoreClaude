using Accounting.Application.BankCartDetails.Commands.UpdateBankCartDetail;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;
using Moq;

namespace Accounting.Application.Tests.BankCartDetails.Commands.UpdateBankCartDetail;

public sealed class UpdateBankCartDetailCommandHandlerTests
{
    private static UpdateBankCartDetailCommand ValidCommand(Guid id) => new(
        Id: id,
        ReceipId: Guid.NewGuid(),
        CheckId: Guid.NewGuid(),
        BankId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        AccountNumber: "9876543210987",
        Month: "02",
        Cheqno: "87654321",
        RecivDate: "14020202",
        CheckReceiptType: false,
        Debtor: 0m,
        Creditor: 2000m,
        Year: "1403",
        CheckIncorrentId: Guid.NewGuid())
    {
        VahedCode = "0002",
    };

    private static TB_BANKCARTDETAIL ExistingEntity(Guid id, bool? isDeleted = false) => new()
    {
        ID = id,
        RECEIP_ID = Guid.NewGuid(),
        CHECK_ID = Guid.NewGuid(),
        BANK_ID = Guid.NewGuid(),
        BRANCH_ID = Guid.NewGuid(),
        ACCOUNTNUMBER = "1111111111111",
        MONTH = "01",
        CHEQNO = "11111111",
        RECIVDATE = "14010101",
        CHECKRECEIPTTYPE = true,
        DEBTOR = 500m,
        CREDITOR = 0m,
        VAHEDCODE = "0001",
        YEAR = "1401",
        CHECK_INCORRENT_ID = Guid.NewGuid(),
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
    public async Task Handle_ExistingNonDeletedRecord_WritesAllWritableFieldsFromCommand()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IBankCartDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.ReceipId, entity.RECEIP_ID);
        Assert.Equal(command.CheckId, entity.CHECK_ID);
        Assert.Equal(command.BankId, entity.BANK_ID);
        Assert.Equal(command.BranchId, entity.BRANCH_ID);
        Assert.Equal(command.AccountNumber, entity.ACCOUNTNUMBER);
        Assert.Equal(command.Month, entity.MONTH);
        Assert.Equal(command.Cheqno, entity.CHEQNO);
        Assert.Equal(command.RecivDate, entity.RECIVDATE);
        Assert.Equal(command.CheckReceiptType, entity.CHECKRECEIPTTYPE);
        Assert.Equal(command.Debtor, entity.DEBTOR);
        Assert.Equal(command.Creditor, entity.CREDITOR);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.Year, entity.YEAR);
        Assert.Equal(command.CheckIncorrentId, entity.CHECK_INCORRENT_ID);
    }

    [Fact]
    public async Task Handle_SetsChangeUserIdFromCurrentUser_AndStampsUpdatedDate()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IBankCartDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr02");

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

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

        var repository = new Mock<IBankCartDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

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
        var repository = new Mock<IBankCartDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_BANKCARTDETAIL?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordIsSoftDeleted_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: true);
        var repository = new Mock<IBankCartDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordWithNullIsDeleted_IsTreatedAsNotDeleted_AndUpdateSucceeds()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: null);
        var repository = new Mock<IBankCartDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var command = ValidCommand(id);

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.AccountNumber, entity.ACCOUNTNUMBER);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingRecord_CallsSaveChangesExactlyOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IBankCartDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IBankCartDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

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
        var repository = new Mock<IBankCartDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
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
        var repository = new Mock<IBankCartDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");

        var handler = new UpdateBankCartDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<UpdateBankCartDetailCommand, Unit>(currentUser.Object);
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
