using Accounting.Application.BankAccounts.Commands.DeleteBankAccount;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.BankAccounts.Commands.DeleteBankAccount;

public sealed class DeleteBankAccountCommandHandlerTests
{
    private static TB_ACCOUNT ExistingEntity(Guid id, bool? isDeleted = false) => new()
    {
        ID = id,
        ACCOUNTNUMBER = "1234567890",
        ACCOUNTHOLDER = "علی رضایی",
        CARDNUMBER = "6037991234567890",
        SHEBANUMBER = "IR120170000000123456789012",
        FIRSTAMOUNT = 1_000_000m,
        BANK_ID = Guid.NewGuid(),
        BRANCH_ID = Guid.NewGuid(),
        ACCOUNTTYPE_ID = Guid.NewGuid(),
        ACCOUNTCODE_ID = Guid.NewGuid(),
        CHECKFILE = null,
        VAHEDCODE = "0001",
        ACCOUNTOPENINGDATE = "13990101",
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
        var repository = new Mock<IBankAccountRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter9");

        var handler = new DeleteBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteBankAccountCommand(id), CancellationToken.None);

        Assert.True(entity.ISDELETED);
        Assert.Equal("deleter9", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NeverIssuesPhysicalDelete_RepositoryInterfaceHasNoDeleteMethod()
    {
        var repositoryMethodNames = typeof(IBankAccountRepository)
            .GetMethods()
            .Select(m => m.Name)
            .ToArray();

        Assert.DoesNotContain(repositoryMethodNames, name =>
            name.Contains("Remove", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Delete", StringComparison.OrdinalIgnoreCase));

        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IBankAccountRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new DeleteBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteBankAccountCommand(id), CancellationToken.None);

        Assert.Same(entity, await repository.Object.GetForUpdateAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IBankAccountRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_ACCOUNT?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new DeleteBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteBankAccountCommand(id), CancellationToken.None));

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

        var repository = new Mock<IBankAccountRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("newDeleter");

        var handler = new DeleteBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new DeleteBankAccountCommand(id), CancellationToken.None));

        Assert.Null(exception);
        Assert.True(entity.ISDELETED);
        Assert.Equal("previousEditor", entity.CHANGEUSERID);
        Assert.Equal(updatedAt, entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordWithNullIsDeleted_IsGenuinelySoftDeleted_NotTreatedAsAlreadyDeleted()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: null);
        var repository = new Mock<IBankAccountRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("deleter5");

        var handler = new DeleteBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteBankAccountCommand(id), CancellationToken.None);

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
        var repository = new Mock<IBankAccountRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new DeleteBankAccountCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(new DeleteBankAccountCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
