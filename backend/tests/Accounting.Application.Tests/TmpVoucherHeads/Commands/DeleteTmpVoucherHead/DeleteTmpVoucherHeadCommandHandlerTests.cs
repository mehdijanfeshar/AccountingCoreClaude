using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.TmpVoucherHeads.Commands.DeleteTmpVoucherHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.TmpVoucherHeads.Commands.DeleteTmpVoucherHead;

public sealed class DeleteTmpVoucherHeadCommandHandlerTests
{
    private static readonly Guid ExistingId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static TB_TMP_VOUCHERHEAD ExistingEntity() => new()
    {
        ID = ExistingId,
        DATE_DOC = "14040101",
        HEAD_DESC = "شرح اولیه",
        VAHEDCODE = "0001",
        YEAR = "1404",
        ADDUSERID = "creator02",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = false,
    };

    private static (DeleteTmpVoucherHeadCommandHandler Handler, Mock<ITmpVoucherHeadRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork) Build(TB_TMP_VOUCHERHEAD? existing, string userId = "deleter02")
    {
        var repository = new Mock<ITmpVoucherHeadRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        var handler = new DeleteTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, currentUser.Object);
        return (handler, repository, unitOfWork);
    }

    [Fact]
    public async Task Handle_SoftDeletesRow_AndStampsAuditFromCurrentUser()
    {
        var entity = ExistingEntity();
        var (handler, _, unitOfWork) = Build(entity, "deleter88");

        await handler.Handle(new DeleteTmpVoucherHeadCommand(ExistingId), CancellationToken.None);

        Assert.Equal(true, entity.ISDELETED);
        Assert.Equal("deleter88", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LeavesBusinessDataAndCreationAuditIntact()
    {
        var entity = ExistingEntity();
        var (handler, _, _) = Build(entity);

        await handler.Handle(new DeleteTmpVoucherHeadCommand(ExistingId), CancellationToken.None);

        Assert.Equal("14040101", entity.DATE_DOC);
        Assert.Equal("creator02", entity.ADDUSERID);
        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), entity.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_RowDoesNotExist_ThrowsNotFound_AndNeverSaves()
    {
        var (handler, _, unitOfWork) = Build(existing: null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteTmpVoucherHeadCommand(ExistingId), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadySoftDeletedRow_IsNoOp_AndDoesNotReStampAudit()
    {
        var entity = ExistingEntity();
        entity.ISDELETED = true;
        entity.CHANGEUSERID = "original02";
        entity.UPDATEDDATE = new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var (handler, _, unitOfWork) = Build(entity, "someoneElse");

        await handler.Handle(new DeleteTmpVoucherHeadCommand(ExistingId), CancellationToken.None);

        Assert.Equal(true, entity.ISDELETED);
        Assert.Equal("original02", entity.CHANGEUSERID);
        Assert.Equal(new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// The trickiest boundary case on this table, and the one that differs from
    /// <c>TB_PAYRECIVHEAD</c>: <c>ISDELETED</c> is <c>bool?</c>, and a NULL means "not deleted".
    /// Such a row must be genuinely soft-deleted (and audited), NOT treated as an idempotent
    /// no-op — otherwise every legacy row that predates the flag would be undeletable.
    /// </summary>
    [Fact]
    public async Task Handle_NullIsDeleted_IsGenuinelySoftDeleted_NotTreatedAsAlreadyDeleted()
    {
        var entity = ExistingEntity();
        entity.ISDELETED = null;
        var (handler, _, unitOfWork) = Build(entity, "deleter55");

        await handler.Handle(new DeleteTmpVoucherHeadCommand(ExistingId), CancellationToken.None);

        Assert.Equal(true, entity.ISDELETED);
        Assert.Equal("deleter55", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var (handler, repository, unitOfWork) = Build(ExistingEntity());
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await handler.Handle(new DeleteTmpVoucherHeadCommand(ExistingId), token);

        repository.Verify(r => r.GetForUpdateAsync(ExistingId, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
