using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PayReciveHeads.Commands.UpdatePayReciveHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.PayReciveHeads.Commands.UpdatePayReciveHead;

public sealed class UpdatePayReciveHeadCommandHandlerTests
{
    private static readonly Guid ExistingId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static UpdatePayReciveHeadCommand ValidCommand() => new(
        Id: ExistingId,
        PayReciveCode: "00999",
        PayReciveDate: "14041231",
        PayReciveDescription: "شرح به‌روزشده",
        PayReciveType: false,
        VahedCode: "0002",
        Year: "1405",
        VoucherHeadId: Guid.Parse("22222222-2222-2222-2222-222222222222"));

    /// <summary>
    /// A pre-existing row whose audit/creation columns carry recognizable values, so that
    /// <see cref="Handle_LeavesImmutableColumnsUntouched"/> can prove they survive the update
    /// byte-for-byte.
    /// </summary>
    private static TB_PAYRECIVHEAD ExistingEntity() => new()
    {
        ID = ExistingId,
        PAYRECIVCODE = "00001",
        PAYRECIVDATE = "14040101",
        PAYRECIVDESCRIPTION = "شرح اولیه",
        PAYRECIVTYPE = true,
        VAHEDCODE = "0001",
        YEAR = "1404",
        VOUCHERSHEAD_ID = null,
        ADDUSERID = "creator01",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = false,
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "editor01")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    private static (UpdatePayReciveHeadCommandHandler Handler, Mock<IPayReciveHeadRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork) Build(TB_PAYRECIVHEAD? existing, string userId = "editor01")
    {
        var repository = new Mock<IPayReciveHeadRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new UpdatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock(userId).Object);
        return (handler, repository, unitOfWork);
    }

    [Fact]
    public async Task Handle_OverwritesEveryWritableField_FieldByField()
    {
        var entity = ExistingEntity();
        var (handler, _, _) = Build(entity);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.PayReciveCode, entity.PAYRECIVCODE);
        Assert.Equal(command.PayReciveDate, entity.PAYRECIVDATE);
        Assert.Equal(command.PayReciveDescription, entity.PAYRECIVDESCRIPTION);
        Assert.Equal(command.PayReciveType, entity.PAYRECIVTYPE);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.Year, entity.YEAR);
        Assert.Equal(command.VoucherHeadId, entity.VOUCHERSHEAD_ID);
    }

    [Fact]
    public async Task Handle_StampsChangeUserIdFromCurrentUser_AndUpdatedDate()
    {
        var entity = ExistingEntity();
        var (handler, _, _) = Build(entity, "editor99");

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal("editor99", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
    }

    /// <summary>
    /// The creation audit trail and the soft-delete flag must survive an update untouched —
    /// this is what makes Update incapable of doubling as a back-door delete/undelete.
    /// </summary>
    [Fact]
    public async Task Handle_LeavesImmutableColumnsUntouched()
    {
        var entity = ExistingEntity();
        var originalCreatedDate = entity.CREATEDDATE;
        var (handler, _, _) = Build(entity);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ExistingId, entity.ID);
        Assert.Equal("creator01", entity.ADDUSERID);
        Assert.Equal(originalCreatedDate, entity.CREATEDDATE);
        Assert.False(entity.ISDELETED);
    }

    /// <summary>
    /// Structural companion to the test above: the immutable columns are not merely left alone
    /// by the current handler, they are unreachable from the command's public surface.
    /// </summary>
    [Fact]
    public void UpdateCommand_ExposesNoImmutableColumns()
    {
        var propertyNames = typeof(UpdatePayReciveHeadCommand)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.DoesNotContain("AddUserId", propertyNames);
        Assert.DoesNotContain("ChangeUserId", propertyNames);
        Assert.DoesNotContain("CreatedDate", propertyNames);
        Assert.DoesNotContain("UpdatedDate", propertyNames);
        Assert.DoesNotContain("IsDeleted", propertyNames);
    }

    [Fact]
    public async Task Handle_RowDoesNotExist_ThrowsNotFound_AndNeverSaves()
    {
        var (handler, _, unitOfWork) = Build(existing: null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(ValidCommand(), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// A soft-deleted row is "logically absent", exactly matching the read side's
    /// <c>ISDELETED == false</c> filter. On this table <c>ISDELETED</c> is non-nullable, so
    /// <see langword="true"/> is the only deleted state there is.
    /// </summary>
    [Fact]
    public async Task Handle_SoftDeletedRow_ThrowsNotFound_AndNeverSaves()
    {
        var entity = ExistingEntity();
        entity.ISDELETED = true;
        var (handler, _, unitOfWork) = Build(entity);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(ValidCommand(), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        // The soft-deleted row must not be mutated on the way out.
        Assert.Equal("00001", entity.PAYRECIVCODE);
        Assert.Null(entity.CHANGEUSERID);
    }

    [Fact]
    public async Task Handle_CallsSaveChangesExactlyOnce_AndLooksUpTheRouteId()
    {
        var (handler, repository, unitOfWork) = Build(ExistingEntity());

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(r => r.GetForUpdateAsync(ExistingId, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var (handler, repository, unitOfWork) = Build(ExistingEntity());
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.GetForUpdateAsync(ExistingId, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    /// <summary>
    /// PUT semantics: an omitted optional field is written as NULL, it does not retain the
    /// previous value. This is the documented trade-off of choosing PUT over PATCH on Legacy
    /// tables (CLAUDE.md phase 8).
    /// </summary>
    [Fact]
    public async Task Handle_NullOptionalFields_OverwritePreviousValuesWithNull()
    {
        var entity = ExistingEntity();
        entity.PAYRECIVTYPE = true;
        entity.VOUCHERSHEAD_ID = Guid.NewGuid();
        var (handler, _, _) = Build(entity);

        await handler.Handle(
            ValidCommand() with { PayReciveType = null, VoucherHeadId = null },
            CancellationToken.None);

        Assert.Null(entity.PAYRECIVTYPE);
        Assert.Null(entity.VOUCHERSHEAD_ID);
    }
}
