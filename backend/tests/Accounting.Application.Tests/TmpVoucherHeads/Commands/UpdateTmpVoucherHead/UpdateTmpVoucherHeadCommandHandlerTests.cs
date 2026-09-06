using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;

public sealed class UpdateTmpVoucherHeadCommandHandlerTests
{
    private static readonly Guid ExistingId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static UpdateTmpVoucherHeadCommand ValidCommand() => new(
        Id: ExistingId,
        VoucherHeadId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        DateDoc: "14041231",
        HeadDesc: "شرح به‌روزشده",
        VahedCode: "0002",
        Year: "1405",
        SysType: "X",
        SourceId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static TB_TMP_VOUCHERHEAD ExistingEntity() => new()
    {
        ID = ExistingId,
        VOUCHERSHEAD_ID = null,
        DATE_DOC = "14040101",
        HEAD_DESC = "شرح اولیه",
        VAHEDCODE = "0001",
        YEAR = "1404",
        SYS_TYPE = "K",
        SOURCEID = null,
        ADDUSERID = "creator02",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = false,
    };

    private static (UpdateTmpVoucherHeadCommandHandler Handler, Mock<ITmpVoucherHeadRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork) Build(TB_TMP_VOUCHERHEAD? existing, string userId = "editor02")
    {
        var repository = new Mock<ITmpVoucherHeadRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        var handler = new UpdateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, currentUser.Object);
        return (handler, repository, unitOfWork);
    }

    [Fact]
    public async Task Handle_OverwritesEveryWritableField_FieldByField()
    {
        var entity = ExistingEntity();
        var (handler, _, _) = Build(entity);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.VoucherHeadId, entity.VOUCHERSHEAD_ID);
        Assert.Equal(command.DateDoc, entity.DATE_DOC);
        Assert.Equal(command.HeadDesc, entity.HEAD_DESC);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.Year, entity.YEAR);
        Assert.Equal(command.SysType, entity.SYS_TYPE);
        Assert.Equal(command.SourceId, entity.SOURCEID);
    }

    [Fact]
    public async Task Handle_StampsChangeUserIdFromCurrentUser_AndUpdatedDate()
    {
        var entity = ExistingEntity();
        var (handler, _, _) = Build(entity, "editor77");

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal("editor77", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
    }

    [Fact]
    public async Task Handle_LeavesImmutableColumnsUntouched()
    {
        var entity = ExistingEntity();
        var originalCreatedDate = entity.CREATEDDATE;
        var (handler, _, _) = Build(entity);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ExistingId, entity.ID);
        Assert.Equal("creator02", entity.ADDUSERID);
        Assert.Equal(originalCreatedDate, entity.CREATEDDATE);
        Assert.Equal(false, entity.ISDELETED);
    }

    [Fact]
    public void UpdateCommand_ExposesNoImmutableColumns()
    {
        var propertyNames = typeof(UpdateTmpVoucherHeadCommand)
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

    [Fact]
    public async Task Handle_SoftDeletedRow_ThrowsNotFound_AndNeverSaves()
    {
        var entity = ExistingEntity();
        entity.ISDELETED = true;
        var (handler, _, unitOfWork) = Build(entity);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(ValidCommand(), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("14040101", entity.DATE_DOC);
        Assert.Null(entity.CHANGEUSERID);
    }

    /// <summary>
    /// The subtle case that separates this table from <c>TB_PAYRECIVHEAD</c>: <c>ISDELETED</c> is
    /// <c>bool?</c> here, so a NULL means "not deleted" and the row must be updatable. Treating
    /// NULL as deleted would make an entire class of legacy rows silently un-editable.
    /// </summary>
    [Fact]
    public async Task Handle_NullIsDeleted_IsTreatedAsNotDeleted_AndUpdateSucceeds()
    {
        var entity = ExistingEntity();
        entity.ISDELETED = null;
        var (handler, _, unitOfWork) = Build(entity);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal("14041231", entity.DATE_DOC);
        Assert.NotNull(entity.CHANGEUSERID);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Update must not "repair" the NULL into false — that is not its job.
        Assert.Null(entity.ISDELETED);
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
    /// PUT semantics on a table where literally every column is nullable: an all-null body wipes
    /// every business field. That is the documented trade-off of PUT over PATCH here, and it is
    /// more consequential on this table than anywhere else in the project.
    /// </summary>
    [Fact]
    public async Task Handle_AllNullBody_NullsOutEveryWritableField()
    {
        var entity = ExistingEntity();
        entity.VOUCHERSHEAD_ID = Guid.NewGuid();
        entity.SOURCEID = Guid.NewGuid();
        var (handler, _, _) = Build(entity);

        await handler.Handle(
            new UpdateTmpVoucherHeadCommand(ExistingId, null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.Null(entity.VOUCHERSHEAD_ID);
        Assert.Null(entity.DATE_DOC);
        Assert.Null(entity.HEAD_DESC);
        Assert.Null(entity.VAHEDCODE);
        Assert.Null(entity.YEAR);
        Assert.Null(entity.SYS_TYPE);
        Assert.Null(entity.SOURCEID);
        // The audit trail survives even a full wipe of the business fields.
        Assert.Equal("creator02", entity.ADDUSERID);
        Assert.Equal("editor02", entity.CHANGEUSERID);
    }
}
