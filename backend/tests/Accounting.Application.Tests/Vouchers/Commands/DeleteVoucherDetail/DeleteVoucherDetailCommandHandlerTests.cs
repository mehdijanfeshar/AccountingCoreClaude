using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.DeleteVoucherDetail;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.DeleteVoucherDetail;

public sealed class DeleteVoucherDetailCommandHandlerTests
{
    private static TB_VOUCHERSDETAIL ExistingEntity(Guid id, bool? isDeleted = false) => new()
    {
        ID = id,
        VOUCHERSHEAD_ID = Guid.NewGuid(),
        DESCRIPTION = "ردیف",
        ADDUSERID = "creator1",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CHANGEUSERID = null,
        UPDATEDDATE = null,
        ISDELETED = isDeleted,
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "deleter1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_ExistingNonDeletedRecord_SetsIsDeletedTrue_StampsAuditFields_AndSavesOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repository
            .Setup(r => r.SoftDeleteTafsiliLinksAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter9");

        var handler = new DeleteVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherDetailCommand(id), CancellationToken.None);

        Assert.True(entity.ISDELETED);
        Assert.Equal("deleter9", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingNonDeletedRecord_CascadesSoftDeleteToTafsiliLinks_WithSameUserAndTimestampAsRow()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repository
            .Setup(r => r.SoftDeleteTafsiliLinksAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter9");

        var handler = new DeleteVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherDetailCommand(id), CancellationToken.None);

        repository.Verify(
            r => r.SoftDeleteTafsiliLinksAsync(id, "deleter9", entity.UPDATEDDATE!.Value, It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsSoftDeleteTafsiliLinksAsyncBeforeSaveChangesAsync()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.SoftDeleteTafsiliLinksAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SoftDeleteTafsiliLinksAsync"))
            .ReturnsAsync(0);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new DeleteVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherDetailCommand(id), CancellationToken.None);

        Assert.Equal(new[] { "SoftDeleteTafsiliLinksAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_NeverIssuesPhysicalDelete_RepositoryInterfaceHasNoDeleteMethod()
    {
        var repositoryMethodNames = typeof(IVoucherDetailRepository)
            .GetMethods()
            .Select(m => m.Name)
            .ToArray();

        Assert.DoesNotContain(repositoryMethodNames, name =>
            name.Contains("Remove", StringComparison.OrdinalIgnoreCase) ||
            (name.Contains("Delete", StringComparison.OrdinalIgnoreCase) &&
             !name.StartsWith("SoftDelete", StringComparison.OrdinalIgnoreCase)));

        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new DeleteVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherDetailCommand(id), CancellationToken.None);

        Assert.Same(entity, await repository.Object.GetForUpdateAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_VOUCHERSDETAIL?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new DeleteVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteVoucherDetailCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordAlreadySoftDeleted_IsIdempotent_NoExceptionAndNoSaveChanges_AndAuditFieldsUnchanged_AndCascadeNotInvoked()
    {
        var id = Guid.NewGuid();
        var updatedAt = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);
        var entity = ExistingEntity(id, isDeleted: true);
        entity.CHANGEUSERID = "previousEditor";
        entity.UPDATEDDATE = updatedAt;

        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("newDeleter");

        var handler = new DeleteVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new DeleteVoucherDetailCommand(id), CancellationToken.None));

        Assert.Null(exception);
        Assert.True(entity.ISDELETED);
        Assert.Equal("previousEditor", entity.CHANGEUSERID);
        Assert.Equal(updatedAt, entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(
            r => r.SoftDeleteTafsiliLinksAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RecordWithNullIsDeleted_IsSoftDeleted_NotTreatedAsAlreadyDeleted()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: null);
        var repository = new Mock<IVoucherDetailRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repository
            .Setup(r => r.SoftDeleteTafsiliLinksAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter5");

        var handler = new DeleteVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherDetailCommand(id), CancellationToken.None);

        Assert.True(entity.ISDELETED);
        Assert.Equal("deleter5", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);
        repository
            .Setup(r => r.SoftDeleteTafsiliLinksAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), token))
            .ReturnsAsync(0);

        var handler = new DeleteVoucherDetailCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherDetailCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        repository.Verify(r => r.SoftDeleteTafsiliLinksAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
