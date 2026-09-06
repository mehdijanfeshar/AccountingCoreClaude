using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PayReciveHeads.Commands.DeletePayReciveHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.PayReciveHeads.Commands.DeletePayReciveHead;

public sealed class DeletePayReciveHeadCommandHandlerTests
{
    private static readonly Guid ExistingId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static TB_PAYRECIVHEAD ExistingEntity() => new()
    {
        ID = ExistingId,
        PAYRECIVCODE = "00001",
        PAYRECIVDATE = "14040101",
        PAYRECIVDESCRIPTION = "شرح اولیه",
        VAHEDCODE = "0001",
        YEAR = "1404",
        ADDUSERID = "creator01",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = false,
    };

    private static (DeletePayReciveHeadCommandHandler Handler, Mock<IPayReciveHeadRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork) Build(TB_PAYRECIVHEAD? existing, string userId = "deleter01")
    {
        var repository = new Mock<IPayReciveHeadRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        var handler = new DeletePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, currentUser.Object);
        return (handler, repository, unitOfWork);
    }

    [Fact]
    public async Task Handle_SoftDeletesRow_AndStampsAuditFromCurrentUser()
    {
        var entity = ExistingEntity();
        var (handler, _, unitOfWork) = Build(entity, "deleter99");

        await handler.Handle(new DeletePayReciveHeadCommand(ExistingId), CancellationToken.None);

        Assert.True(entity.ISDELETED);
        Assert.Equal("deleter99", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Soft delete only — the row's business data and creation audit trail stay intact, and no
    /// physical delete path exists (the write repository interface has no remove method at all).
    /// </summary>
    [Fact]
    public async Task Handle_LeavesBusinessDataAndCreationAuditIntact()
    {
        var entity = ExistingEntity();
        var (handler, _, _) = Build(entity);

        await handler.Handle(new DeletePayReciveHeadCommand(ExistingId), CancellationToken.None);

        Assert.Equal("00001", entity.PAYRECIVCODE);
        Assert.Equal("creator01", entity.ADDUSERID);
        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), entity.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_RowDoesNotExist_ThrowsNotFound_AndNeverSaves()
    {
        var (handler, _, unitOfWork) = Build(existing: null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeletePayReciveHeadCommand(ExistingId), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// HTTP DELETE is idempotent: deleting an already-deleted row succeeds as a no-op. Crucially
    /// it must NOT re-stamp the audit columns — otherwise repeated calls would keep overwriting
    /// the record of who actually performed the deletion.
    /// </summary>
    [Fact]
    public async Task Handle_AlreadySoftDeletedRow_IsNoOp_AndDoesNotReStampAudit()
    {
        var entity = ExistingEntity();
        entity.ISDELETED = true;
        entity.CHANGEUSERID = "original01";
        entity.UPDATEDDATE = new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var (handler, _, unitOfWork) = Build(entity, "someoneElse");

        await handler.Handle(new DeletePayReciveHeadCommand(ExistingId), CancellationToken.None);

        Assert.True(entity.ISDELETED);
        Assert.Equal("original01", entity.CHANGEUSERID);
        Assert.Equal(new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), entity.UPDATEDDATE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var (handler, repository, unitOfWork) = Build(ExistingEntity());
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await handler.Handle(new DeletePayReciveHeadCommand(ExistingId), token);

        repository.Verify(r => r.GetForUpdateAsync(ExistingId, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    /// <summary>
    /// Documents an accepted gap rather than asserting a guarantee: this handler does NOT
    /// cascade to <c>TB_PAYRECIVDETAIL</c>, because the aggregate boundary for the pair is
    /// undecided and no repository can reach the child rows. If a cascade is ever added, the
    /// write repository will need a new method and this test should be replaced by a real
    /// cascade assertion (see docs/open-decisions.md).
    /// </summary>
    [Fact]
    public void WriteRepository_HasNoMethodReachingPayReciveDetail()
    {
        var methodNames = typeof(IPayReciveHeadRepository)
            .GetMethods()
            .Select(m => m.Name)
            .ToList();

        Assert.Equal(new[] { "AddAsync", "GetForUpdateAsync" }.OrderBy(n => n), methodNames.OrderBy(n => n));
    }
}
