using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.TmpVoucherHeads.Commands.CreateTmpVoucherHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.TmpVoucherHeads.Commands.CreateTmpVoucherHead;

public sealed class CreateTmpVoucherHeadCommandHandlerTests
{
    /// <summary>
    /// Every field carries a distinct, recognizable value so that a copy/paste mismap between
    /// the two <see cref="Guid"/> columns (<c>VOUCHERSHEAD_ID</c> and <c>SOURCEID</c>) or the two
    /// short code columns (<c>VAHEDCODE</c> and <c>YEAR</c>) is caught field-by-field below.
    /// </summary>
    private static CreateTmpVoucherHeadCommand ValidCommand() => new(
        VoucherHeadId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        DateDoc: "14040101",
        HeadDesc: "شرح سند موقت تستی",
        Year: "1404",
        SysType: "K",
        SourceId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))
    {
        VahedCode = "0001",
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    private static Mock<ITmpVoucherHeadRepository> CapturingRepository(out Func<TB_TMP_VOUCHERHEAD?> staged)
    {
        var repository = new Mock<ITmpVoucherHeadRepository>();
        TB_TMP_VOUCHERHEAD? captured = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TMP_VOUCHERHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TMP_VOUCHERHEAD, CancellationToken>((entity, _) => captured = entity)
            .Returns(Task.CompletedTask);
        staged = () => captured;
        return repository;
    }

    [Fact]
    public async Task Handle_MapsEveryWritableFieldOntoStagedEntity_FieldByField()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal(command.VoucherHeadId, entity!.VOUCHERSHEAD_ID);
        Assert.Equal(command.DateDoc, entity.DATE_DOC);
        Assert.Equal(command.HeadDesc, entity.HEAD_DESC);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.Year, entity.YEAR);
        Assert.Equal(command.SysType, entity.SYS_TYPE);
        Assert.Equal(command.SourceId, entity.SOURCEID);
    }

    /// <summary>
    /// Regression guard for this table's sharpest copy/paste risk: <c>VOUCHERSHEAD_ID</c> and
    /// <c>SOURCEID</c> are both nullable GUIDs, but only the former has an FK. Swapping them
    /// would silently write a bogus source reference AND lose the real voucher link, and the
    /// database would not complain about either half.
    /// </summary>
    [Fact]
    public async Task Handle_VoucherHeadIdAndSourceId_AreNotSwapped()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), entity!.VOUCHERSHEAD_ID);
        Assert.Equal(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), entity.SOURCEID);
    }

    [Fact]
    public async Task Handle_VahedCodeAndYear_AreNotSwapped()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(ValidCommand() with { VahedCode = "AAAA", Year = "BBBB" }, CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal("AAAA", entity!.VAHEDCODE);
        Assert.Equal("BBBB", entity.YEAR);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr02");
        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal("srvusr02", entity!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    /// <summary>
    /// <c>ADDUSERID</c> is nullable on this table, which makes it especially important that the
    /// handler still always stamps it server-side: a nullable audit column is exactly the kind
    /// that quietly ends up empty. The command carries no caller-supplied identity at all.
    /// </summary>
    [Fact]
    public void CreateCommand_ExposesNoCallerSuppliedUserIdProperty()
    {
        var propertyNames = typeof(CreateTmpVoucherHeadCommand)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.DoesNotContain("AddUserId", propertyNames);
        Assert.DoesNotContain("ChangeUserId", propertyNames);
        Assert.DoesNotContain("CreatedDate", propertyNames);
        Assert.DoesNotContain("IsDeleted", propertyNames);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        // ISDELETED is bool? here; a newly created row is stamped with an explicit false rather
        // than being left NULL, even though the read filter treats both the same.
        Assert.Equal(false, entity!.ISDELETED);
        Assert.NotNull(entity.CREATEDDATE);
        Assert.Null(entity.UPDATEDDATE);
        Assert.Null(entity.CHANGEUSERID);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal(entity!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<ITmpVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_TMP_VOUCHERHEAD>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<ITmpVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TMP_VOUCHERHEAD>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<ITmpVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_TMP_VOUCHERHEAD>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    /// <summary>
    /// Every column on <c>TB_TMP_VOUCHERHEAD</c> reachable through a positional command
    /// parameter is nullable, so an all-null command must be accepted and must stage genuine
    /// NULLs — nothing may be coerced to a default. <c>VahedCode</c> is the one exception: it is
    /// no longer a positional/nullable parameter at all (it is always server-assigned — see
    /// <see cref="CreateTmpVoucherHeadCommand.VahedCode"/> XML doc), so an "unset" command still
    /// carries its default <see cref="string.Empty"/> there, not <see langword="null"/>.
    /// </summary>
    [Fact]
    public async Task Handle_AllFieldsNull_Passes_EveryColumnIsNullable()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(
            new CreateTmpVoucherHeadCommand(null, null, null, null, null, null),
            CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Null(entity!.VOUCHERSHEAD_ID);
        Assert.Null(entity.DATE_DOC);
        Assert.Null(entity.HEAD_DESC);
        Assert.Equal(string.Empty, entity.VAHEDCODE);
        Assert.Null(entity.YEAR);
        Assert.Null(entity.SYS_TYPE);
        Assert.Null(entity.SOURCEID);
        // ...but the server-controlled columns are still populated.
        Assert.NotEqual(Guid.Empty, entity.ID);
        Assert.NotNull(entity.ADDUSERID);
        Assert.NotNull(entity.CREATEDDATE);
    }

    /// <summary>
    /// Documents the deliberate Head-only scope as a behavioural fact rather than only a comment:
    /// creating a temporary voucher stages exactly one entity and never touches
    /// <c>TB_TMP_VOUCHERSDETAIL</c>, so the resulting document has no lines. The reference
    /// project creates head + details as one graph; closing that gap needs an explicit
    /// aggregate-boundary decision (see docs/open-decisions.md).
    /// </summary>
    [Fact]
    public void WriteRepository_HasNoMethodReachingTmpVoucherDetail()
    {
        var methodNames = typeof(ITmpVoucherHeadRepository)
            .GetMethods()
            .Select(m => m.Name)
            .ToList();

        Assert.Equal(new[] { "AddAsync", "GetForUpdateAsync" }.OrderBy(n => n), methodNames.OrderBy(n => n));
    }

    /// <summary>
    /// The command exposes no way to supply detail lines either — so the Head-only limitation is
    /// structural, not just a missing repository method.
    /// </summary>
    [Fact]
    public void CreateCommand_ExposesNoDetailCollection()
    {
        var hasCollectionProperty = typeof(CreateTmpVoucherHeadCommand)
            .GetProperties()
            .Any(p => p.PropertyType != typeof(string)
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType));

        Assert.False(hasCollectionProperty);
    }

    [Fact]
    public async Task Handle_ThroughVahedScopeBehavior_ClientSuppliedVahedCodeIsDiscarded_ServerValueIsWritten()
    {
        // End-to-end forgery-prevention proof: even when a "client" manages to populate
        // command.VahedCode with a forged value before dispatch, running the command through
        // VahedScopeBehavior — exactly as the real MediatR pipeline does — overwrites it
        // unconditionally with ICurrentUser.VahedCode before the handler ever sees it.
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");

        var handler = new CreateTmpVoucherHeadCommandHandler(
            repository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<CreateTmpVoucherHeadCommand, Guid>(currentUser.Object);
        var forgedCommand = ValidCommand() with { VahedCode = "9999" };

        await behavior.Handle(forgedCommand, ct => handler.Handle(forgedCommand, ct), CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal("0009", entity!.VAHEDCODE);
        Assert.Equal("0009", forgedCommand.VahedCode);
    }
}
