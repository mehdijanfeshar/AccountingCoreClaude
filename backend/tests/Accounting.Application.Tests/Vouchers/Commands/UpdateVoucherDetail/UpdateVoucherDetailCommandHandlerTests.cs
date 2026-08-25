using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.UpdateVoucherDetail;

public sealed class UpdateVoucherDetailCommandHandlerTests
{
    private static UpdateVoucherDetailCommand ValidCommand(Guid id) => new(
        Id: id,
        AccountId: Guid.NewGuid(),
        ReceiptId: Guid.NewGuid(),
        CheckId: Guid.NewGuid(),
        LowLevelCodeId: Guid.NewGuid(),
        EtebarId: Guid.NewGuid(),
        Description: "ردیف اصلاحی",
        Radif: 2,
        Debtor: null,
        Creditor: 2500m,
        VahedCode: "0002",
        Year: "1404");

    private static TB_VOUCHERSDETAIL ExistingEntity(Guid id, Guid voucherHeadId, bool? isDeleted = false) => new()
    {
        ID = id,
        VOUCHERSHEAD_ID = voucherHeadId,
        ACCOUNT_ID = Guid.NewGuid(),
        DESCRIPTION = "ردیف اولیه",
        RADIF = 1,
        DEBTOR = 1000m,
        CREDITOR = null,
        VAHEDCODE = "0001",
        YEAR = "1403",
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
        var voucherHeadId = Guid.NewGuid();
        var entity = ExistingEntity(id, voucherHeadId);
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.AccountId, entity.ACCOUNT_ID);
        Assert.Equal(command.ReceiptId, entity.RECEIP_ID);
        Assert.Equal(command.CheckId, entity.CHECK_ID);
        Assert.Equal(command.LowLevelCodeId, entity.LOWLEVELCODE_ID);
        Assert.Equal(command.EtebarId, entity.ETEBAR_ID);
        Assert.Equal(command.Description, entity.DESCRIPTION);
        Assert.Equal(command.Radif, entity.RADIF);
        Assert.Equal(command.Debtor, entity.DEBTOR);
        Assert.Equal(command.Creditor, entity.CREDITOR);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.Year, entity.YEAR);
    }

    [Fact]
    public async Task Handle_NeverTouchesVoucherHeadIdIdAddUserIdCreatedDateOrIsDeleted()
    {
        var id = Guid.NewGuid();
        var voucherHeadId = Guid.NewGuid();
        var entity = ExistingEntity(id, voucherHeadId);
        var originalVoucherHeadId = entity.VOUCHERSHEAD_ID;
        var originalId = entity.ID;
        var originalAddUserId = entity.ADDUSERID;
        var originalCreatedDate = entity.CREATEDDATE;
        var originalIsDeleted = entity.ISDELETED;

        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        // Explicit lock: UpdateVoucherDetailCommand structurally has no VoucherHeadId property,
        // so reparenting is provably impossible, not merely "not exercised by this test".
        Assert.Equal(originalVoucherHeadId, entity.VOUCHERSHEAD_ID);
        Assert.Equal(originalId, entity.ID);
        Assert.Equal(originalAddUserId, entity.ADDUSERID);
        Assert.Equal(originalCreatedDate, entity.CREATEDDATE);
        Assert.Equal(originalIsDeleted, entity.ISDELETED);
    }

    [Fact]
    public void Command_HasNoVoucherHeadIdProperty_ReparentingIsStructurallyImpossible()
    {
        var properties = typeof(UpdateVoucherDetailCommand).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain(properties, name =>
            name.Contains("VoucherHead", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("VOUCHERSHEAD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_SetsChangeUserIdFromCurrentUser_AndStampsUpdatedDate()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, Guid.NewGuid());
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr02");

        var handler = new UpdateVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal("srvusr02", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_VOUCHERSDETAIL?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordIsSoftDeleted_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, Guid.NewGuid(), isDeleted: true);
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordWithNullIsDeleted_IsTreatedAsNotDeleted_AndUpdateSucceeds()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, Guid.NewGuid(), isDeleted: null);
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal("ردیف اصلاحی", entity.DESCRIPTION);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingRecord_CallsSaveChangesExactlyOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, Guid.NewGuid());
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, Guid.NewGuid());
        var repository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new UpdateVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
