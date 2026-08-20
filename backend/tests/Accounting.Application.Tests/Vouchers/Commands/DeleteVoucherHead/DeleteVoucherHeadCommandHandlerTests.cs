using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.DeleteVoucherHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.DeleteVoucherHead;

public sealed class DeleteVoucherHeadCommandHandlerTests
{
    private static TB_VOUCHERSHEAD ExistingEntity(Guid id, bool? isDeleted = false) => new()
    {
        ID = id,
        DOC_NUM = "000001",
        DATE_DOC = "14030101",
        VAHEDCODE = "0001",
        YEAR = "1403",
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
        var repository = new Mock<IVoucherHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repository
            .Setup(r => r.SoftDeleteDetailTreeAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter9");

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None);

        Assert.True(entity.ISDELETED);
        Assert.Equal("deleter9", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingNonDeletedRecord_CascadesSoftDeleteToDetailLines_WithSameUserAndTimestampAsHead()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVoucherHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repository
            .Setup(r => r.SoftDeleteDetailTreeAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter9");

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None);

        repository.Verify(
            r => r.SoftDeleteDetailTreeAsync(id, "deleter9", entity.UPDATEDDATE!.Value, It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsSoftDeleteDetailTreeAsyncBeforeSaveChangesAsync()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVoucherHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.SoftDeleteDetailTreeAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SoftDeleteDetailTreeAsync"))
            .ReturnsAsync(0);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None);

        Assert.Equal(new[] { "SoftDeleteDetailTreeAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_NeverIssuesPhysicalDelete_RepositoryInterfaceHasNoDeleteMethod()
    {
        // Structural guarantee: IVoucherHeadRepository does not expose any Remove/physical-Delete
        // method, so it is impossible for this handler (or any future one) to issue a physical
        // DELETE through this abstraction — soft-delete via mutation (e.g. the "SoftDelete*"
        // prefixed methods, such as SoftDeleteDetailTreeAsync) is the only path. Any bare
        // "Delete"-named method that is NOT prefixed "SoftDelete" would be suspicious and should
        // fail this guard.
        var repositoryMethodNames = typeof(IVoucherHeadRepository)
            .GetMethods()
            .Select(m => m.Name)
            .ToArray();

        Assert.DoesNotContain(repositoryMethodNames, name =>
            name.Contains("Remove", StringComparison.OrdinalIgnoreCase) ||
            (name.Contains("Delete", StringComparison.OrdinalIgnoreCase) &&
             !name.StartsWith("SoftDelete", StringComparison.OrdinalIgnoreCase)));

        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVoucherHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None);

        Assert.Same(entity, await repository.Object.GetForUpdateAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IVoucherHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_VOUCHERSHEAD?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordAlreadySoftDeleted_IsIdempotent_NoExceptionAndNoSaveChanges_AndAuditFieldsUnchanged()
    {
        var id = Guid.NewGuid();
        var updatedAt = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);
        var entity = ExistingEntity(id, isDeleted: true);
        entity.CHANGEUSERID = "previousEditor";
        entity.UPDATEDDATE = updatedAt;

        var repository = new Mock<IVoucherHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("newDeleter");

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None));

        Assert.Null(exception);
        Assert.True(entity.ISDELETED);
        Assert.Equal("previousEditor", entity.CHANGEUSERID);
        Assert.Equal(updatedAt, entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(
            r => r.SoftDeleteDetailTreeAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RecordWithNullIsDeleted_IsSoftDeleted_NotTreatedAsAlreadyDeleted()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: null);
        var repository = new Mock<IVoucherHeadRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter5");

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None);

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
        var repository = new Mock<IVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);
        repository
            .Setup(r => r.SoftDeleteDetailTreeAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), token))
            .ReturnsAsync(0);

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteVoucherHeadCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        repository.Verify(r => r.SoftDeleteDetailTreeAsync(id, It.IsAny<string?>(), It.IsAny<DateTime>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
