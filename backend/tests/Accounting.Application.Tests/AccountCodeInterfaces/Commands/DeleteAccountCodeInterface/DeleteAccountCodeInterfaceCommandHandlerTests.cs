using Accounting.Application.AccountCodeInterfaces.Commands.DeleteAccountCodeInterface;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.AccountCodeInterfaces.Commands.DeleteAccountCodeInterface;

public sealed class DeleteAccountCodeInterfaceCommandHandlerTests
{
    private static TB_ACCOUNTCODE_INTERFACE ExistingEntity(Guid id, bool isDeleted = false) => new()
    {
        ID = id,
        TYPE = true,
        ACCOUNTCODEID = Guid.NewGuid(),
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
        var repository = new Mock<IAccountCodeInterfaceRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter9");

        var handler = new DeleteAccountCodeInterfaceCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteAccountCodeInterfaceCommand(id), CancellationToken.None);

        Assert.True(entity.ISDELETED);
        Assert.Equal("deleter9", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NeverIssuesPhysicalDelete_RepositoryInterfaceHasNoDeleteMethod()
    {
        var repositoryMethodNames = typeof(IAccountCodeInterfaceRepository)
            .GetMethods()
            .Select(m => m.Name)
            .ToArray();

        Assert.DoesNotContain(repositoryMethodNames, name =>
            name.Contains("Remove", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Delete", StringComparison.OrdinalIgnoreCase));

        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IAccountCodeInterfaceRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new DeleteAccountCodeInterfaceCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteAccountCodeInterfaceCommand(id), CancellationToken.None);

        Assert.Same(entity, await repository.Object.GetForUpdateAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IAccountCodeInterfaceRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_ACCOUNTCODE_INTERFACE?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new DeleteAccountCodeInterfaceCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteAccountCodeInterfaceCommand(id), CancellationToken.None));

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

        var repository = new Mock<IAccountCodeInterfaceRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("newDeleter");

        var handler = new DeleteAccountCodeInterfaceCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new DeleteAccountCodeInterfaceCommand(id), CancellationToken.None));

        Assert.Null(exception);
        Assert.True(entity.ISDELETED);
        Assert.Equal("previousEditor", entity.CHANGEUSERID);
        Assert.Equal(updatedAt, entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IAccountCodeInterfaceRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new DeleteAccountCodeInterfaceCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteAccountCodeInterfaceCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
