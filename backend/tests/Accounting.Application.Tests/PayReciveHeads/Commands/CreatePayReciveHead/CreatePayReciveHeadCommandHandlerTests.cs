using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PayReciveHeads.Commands.CreatePayReciveHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.PayReciveHeads.Commands.CreatePayReciveHead;

public sealed class CreatePayReciveHeadCommandHandlerTests
{
    /// <summary>
    /// Every field carries a distinct, recognizable value so that a copy/paste mismap between
    /// two similarly-named <c>PAYRECIV*</c> columns in the handler is caught by
    /// <see cref="Handle_MapsEveryWritableFieldOntoStagedEntity_FieldByField"/> below.
    /// </summary>
    private static CreatePayReciveHeadCommand ValidCommand() => new(
        PayReciveCode: "00123",
        PayReciveDate: "14040101",
        PayReciveDescription: "شرح سند دریافت و پرداخت تستی",
        PayReciveType: true,
        Year: "1404",
        VoucherHeadId: Guid.NewGuid())
    {
        VahedCode = "0001",
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    private static (CreatePayReciveHeadCommandHandler Handler, Mock<IPayReciveHeadRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork, Mock<ICurrentUser> CurrentUser) Build(string userId = "user1")
    {
        var repository = new Mock<IPayReciveHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock(userId);
        var handler = new CreatePayReciveHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        return (handler, repository, unitOfWork, currentUser);
    }

    private static Mock<IPayReciveHeadRepository> CapturingRepository(out Func<TB_PAYRECIVHEAD?> staged)
    {
        var repository = new Mock<IPayReciveHeadRepository>();
        TB_PAYRECIVHEAD? captured = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PAYRECIVHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PAYRECIVHEAD, CancellationToken>((entity, _) => captured = entity)
            .Returns(Task.CompletedTask);
        staged = () => captured;
        return repository;
    }

    [Fact]
    public async Task Handle_MapsEveryWritableFieldOntoStagedEntity_FieldByField()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal(command.PayReciveCode, entity!.PAYRECIVCODE);
        Assert.Equal(command.PayReciveDate, entity.PAYRECIVDATE);
        Assert.Equal(command.PayReciveDescription, entity.PAYRECIVDESCRIPTION);
        Assert.Equal(command.PayReciveType, entity.PAYRECIVTYPE);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.Year, entity.YEAR);
        Assert.Equal(command.VoucherHeadId, entity.VOUCHERSHEAD_ID);
    }

    /// <summary>
    /// Regression guard for the specific copy/paste risk on this table: PAYRECIVCODE and
    /// PAYRECIVDATE are adjacent, both short numeric-looking strings that a careless handler
    /// could swap, and neither is protected by any UNIQUE constraint that would surface the
    /// mistake later.
    /// </summary>
    [Fact]
    public async Task Handle_PayReciveCodeAndPayReciveDate_AreNotSwapped()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(
            ValidCommand() with { PayReciveCode = "AAAAA", PayReciveDate = "BBBBBBBB" },
            CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal("AAAAA", entity!.PAYRECIVCODE);
        Assert.Equal("BBBBBBBB", entity.PAYRECIVDATE);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        var handler = new CreatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal("srvusr01", entity!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    /// <summary>
    /// Structural proof that <c>ADDUSERID</c> cannot be forged: the command exposes no
    /// caller-supplied user identifier at all, so re-introducing the phase-6 vulnerability would
    /// be a compile error rather than a silent logic regression.
    /// </summary>
    [Fact]
    public void CreateCommand_ExposesNoCallerSuppliedUserIdProperty()
    {
        var propertyNames = typeof(CreatePayReciveHeadCommand)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.DoesNotContain("AddUserId", propertyNames);
        Assert.DoesNotContain("ChangeUserId", propertyNames);
        Assert.DoesNotContain("CreatedDate", propertyNames);
        Assert.DoesNotContain("IsDeleted", propertyNames);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonDefaultCreatedDate()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        // ISDELETED is a non-nullable bool on this table, so "not deleted" is exactly false.
        Assert.False(entity!.ISDELETED);
        Assert.NotEqual(default, entity.CREATEDDATE);
        Assert.Null(entity.UPDATEDDATE);
        Assert.Null(entity.CHANGEUSERID);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreatePayReciveHeadCommandHandler(
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
        var (handler, repository, unitOfWork, _) = Build();

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_PAYRECIVHEAD>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IPayReciveHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_PAYRECIVHEAD>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var (handler, repository, unitOfWork, _) = Build();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_PAYRECIVHEAD>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    /// <summary>
    /// The two genuinely optional columns on this table (<c>PAYRECIVTYPE</c>,
    /// <c>VOUCHERSHEAD_ID</c>) must round-trip as NULL rather than being coerced.
    /// </summary>
    [Fact]
    public async Task Handle_NullOptionalFields_AreStagedAsNull()
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(
            ValidCommand() with { PayReciveType = null, VoucherHeadId = null },
            CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Null(entity!.PAYRECIVTYPE);
        Assert.Null(entity.VOUCHERSHEAD_ID);
    }

    /// <summary>
    /// Documents the CONFIRMED <c>bool?</c>-should-be-enum defect on <c>PAYRECIVTYPE</c>: the
    /// real Legacy domain is three-valued (1 = payment, 2 = receipt, 3 = both), and a
    /// <see cref="bool"/>? can only express two of those three states plus NULL. This test does
    /// not assert the bug is fixed — it pins the current, deliberately unfixed behaviour so that
    /// whoever re-types the column has to come here and make the change visible.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_PayReciveType_RoundTripsOnlyTwoOfItsThreeRealValues(bool payReciveType)
    {
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);

        await handler.Handle(ValidCommand() with { PayReciveType = payReciveType }, CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal(payReciveType, entity!.PAYRECIVTYPE);

        // Pins the current (wrong) CLR type. When the enum fix lands, this line fails and forces
        // the change to be acknowledged here rather than slipping through silently.
        Assert.Equal(
            typeof(bool?),
            typeof(CreatePayReciveHeadCommand)
                .GetProperty(nameof(CreatePayReciveHeadCommand.PayReciveType))!.PropertyType);
    }

    [Fact]
    public async Task Handle_MapsVahedCodeFromCommandAtFaceValue()
    {
        // The handler itself just maps request.VahedCode onto the entity — it does not read
        // ICurrentUser.VahedCode directly. Forgery prevention is VahedScopeBehavior's job (see
        // the dedicated pipeline test below); this test only proves the mapping is faithful.
        var repository = CapturingRepository(out var staged);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, CurrentUserMock().Object);
        var command = ValidCommand() with { VahedCode = "0009" };

        await handler.Handle(command, CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal("0009", entity!.VAHEDCODE);
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
        var handler = new CreatePayReciveHeadCommandHandler(
            repository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<CreatePayReciveHeadCommand, Guid>(currentUser.Object);
        var forgedCommand = ValidCommand() with { VahedCode = "9999" };

        await behavior.Handle(forgedCommand, ct => handler.Handle(forgedCommand, ct), CancellationToken.None);

        var entity = staged();
        Assert.NotNull(entity);
        Assert.Equal("0009", entity!.VAHEDCODE);
        Assert.Equal("0009", forgedCommand.VahedCode);
    }
}
