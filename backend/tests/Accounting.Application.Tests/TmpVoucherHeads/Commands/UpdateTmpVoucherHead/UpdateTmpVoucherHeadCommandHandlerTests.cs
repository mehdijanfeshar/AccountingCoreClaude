using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;
using Accounting.Domain.Entity;
using MediatR;
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
        Year: "1405",
        SysType: "X",
        SourceId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))
    {
        VahedCode = "0002",
    };

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
    /// PUT semantics on a table where almost every column is nullable: an all-null body wipes
    /// every business field it carries. That is the documented trade-off of PUT over PATCH here,
    /// and it is more consequential on this table than anywhere else in the project. <c>VahedCode</c>
    /// is excluded from "every writable field": it is no longer a positional/nullable parameter
    /// (it is always server-assigned — see <see cref="UpdateTmpVoucherHeadCommand.VahedCode"/> XML
    /// doc), so an "unset" command still carries its default <see cref="string.Empty"/> there, not
    /// <see langword="null"/>.
    /// </summary>
    [Fact]
    public async Task Handle_AllNullBody_NullsOutEveryWritableField()
    {
        var entity = ExistingEntity();
        entity.VOUCHERSHEAD_ID = Guid.NewGuid();
        entity.SOURCEID = Guid.NewGuid();
        var (handler, _, _) = Build(entity);

        await handler.Handle(
            new UpdateTmpVoucherHeadCommand(ExistingId, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.Null(entity.VOUCHERSHEAD_ID);
        Assert.Null(entity.DATE_DOC);
        Assert.Null(entity.HEAD_DESC);
        Assert.Equal(string.Empty, entity.VAHEDCODE);
        Assert.Null(entity.YEAR);
        Assert.Null(entity.SYS_TYPE);
        Assert.Null(entity.SOURCEID);
        // The audit trail survives even a full wipe of the business fields.
        Assert.Equal("creator02", entity.ADDUSERID);
        Assert.Equal("editor02", entity.CHANGEUSERID);
    }

    [Fact]
    public async Task Handle_ThroughVahedScopeBehavior_ClientSuppliedVahedCodeIsDiscarded_ServerValueIsWritten()
    {
        // End-to-end forgery-prevention proof: even when a "client" manages to populate
        // command.VahedCode with a forged value before dispatch, running the command through
        // VahedScopeBehavior — exactly as the real MediatR pipeline does — overwrites it
        // unconditionally with ICurrentUser.VahedCode before the handler ever sees it. The
        // behavior's ICurrentUser is a separate mock from Build()'s internal one, since Build()
        // only wires ICurrentUser.UserId (for CHANGEUSERID), not VahedCode.
        var entity = ExistingEntity();
        var (handler, _, _) = Build(entity);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");

        var behavior = new VahedScopeBehavior<UpdateTmpVoucherHeadCommand, Unit>(currentUser.Object);
        var forgedCommand = ValidCommand() with { VahedCode = "9999" };

        await behavior.Handle(
            forgedCommand,
            async ct =>
            {
                await handler.Handle(forgedCommand, ct);
                return Unit.Value;
            },
            CancellationToken.None);

        Assert.Equal("0009", entity.VAHEDCODE);
        Assert.Equal("0009", forgedCommand.VahedCode);
    }
}
