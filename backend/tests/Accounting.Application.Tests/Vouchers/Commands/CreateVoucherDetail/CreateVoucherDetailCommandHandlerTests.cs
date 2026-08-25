using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.CreateVoucherDetail;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.CreateVoucherDetail;

public sealed class CreateVoucherDetailCommandHandlerTests
{
    private static CreateVoucherDetailCommand ValidCommand(Guid voucherHeadId) => new(
        VoucherHeadId: voucherHeadId,
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف اول",
        Radif: 1,
        Debtor: 1000m,
        Creditor: null,
        VahedCode: "0001",
        Year: "1405");

    private static TB_VOUCHERSHEAD ExistingHead(Guid id, bool? isDeleted = false) => new()
    {
        ID = id,
        DOC_NUM = "000001",
        DATE_DOC = "14050101",
        VAHEDCODE = "0001",
        YEAR = "1405",
        ADDUSERID = "creator1",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = isDeleted,
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_ExistingHead_MapsCommandFieldsOntoStagedEntity_AndReturnsGeneratedId()
    {
        var headId = Guid.NewGuid();
        var headRepository = new Mock<IVoucherHeadRepository>();
        headRepository.Setup(r => r.GetForUpdateAsync(headId, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingHead(headId));
        var detailRepository = new Mock<IVoucherDetailRepository>();
        TB_VOUCHERSDETAIL? staged = null;
        detailRepository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateVoucherDetailCommandHandler(headRepository.Object, detailRepository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(headId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(result, staged!.ID);
        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(headId, staged.VOUCHERSHEAD_ID);
        Assert.Equal(command.AccountId, staged.ACCOUNT_ID);
        Assert.Equal(command.ReceiptId, staged.RECEIP_ID);
        Assert.Equal(command.CheckId, staged.CHECK_ID);
        Assert.Equal(command.LowLevelCodeId, staged.LOWLEVELCODE_ID);
        Assert.Equal(command.EtebarId, staged.ETEBAR_ID);
        Assert.Equal(command.Description, staged.DESCRIPTION);
        Assert.Equal(command.Radif, staged.RADIF);
        Assert.Equal(command.Debtor, staged.DEBTOR);
        Assert.Equal(command.Creditor, staged.CREDITOR);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.Year, staged.YEAR);
        Assert.False(staged.ISDELETED);
        Assert.NotNull(staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ParentHeadMissing_ThrowsNotFoundException_AndNeverStagesOrSaves()
    {
        var headId = Guid.NewGuid();
        var headRepository = new Mock<IVoucherHeadRepository>();
        headRepository.Setup(r => r.GetForUpdateAsync(headId, It.IsAny<CancellationToken>())).ReturnsAsync((TB_VOUCHERSHEAD?)null);
        var detailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateVoucherDetailCommandHandler(headRepository.Object, detailRepository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(ValidCommand(headId), CancellationToken.None));

        detailRepository.Verify(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ParentHeadSoftDeleted_ThrowsNotFoundException_AndNeverStagesOrSaves()
    {
        var headId = Guid.NewGuid();
        var headRepository = new Mock<IVoucherHeadRepository>();
        headRepository.Setup(r => r.GetForUpdateAsync(headId, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingHead(headId, isDeleted: true));
        var detailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateVoucherDetailCommandHandler(headRepository.Object, detailRepository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(ValidCommand(headId), CancellationToken.None));

        detailRepository.Verify(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var headId = Guid.NewGuid();
        var headRepository = new Mock<IVoucherHeadRepository>();
        headRepository.Setup(r => r.GetForUpdateAsync(headId, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingHead(headId));
        var detailRepository = new Mock<IVoucherDetailRepository>();
        TB_VOUCHERSDETAIL? staged = null;
        detailRepository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");

        var handler = new CreateVoucherDetailCommandHandler(headRepository.Object, detailRepository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(headId), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce_AddAsyncBeforeSaveChanges()
    {
        var headId = Guid.NewGuid();
        var headRepository = new Mock<IVoucherHeadRepository>();
        headRepository.Setup(r => r.GetForUpdateAsync(headId, It.IsAny<CancellationToken>())).ReturnsAsync(ExistingHead(headId));
        var detailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        detailRepository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateVoucherDetailCommandHandler(headRepository.Object, detailRepository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(headId), CancellationToken.None);

        detailRepository.Verify(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToAllDependencies()
    {
        var headId = Guid.NewGuid();
        var headRepository = new Mock<IVoucherHeadRepository>();
        var detailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        headRepository.Setup(r => r.GetForUpdateAsync(headId, token)).ReturnsAsync(ExistingHead(headId));

        var handler = new CreateVoucherDetailCommandHandler(headRepository.Object, detailRepository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(headId), token);

        headRepository.Verify(r => r.GetForUpdateAsync(headId, token), Times.Once);
        detailRepository.Verify(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
