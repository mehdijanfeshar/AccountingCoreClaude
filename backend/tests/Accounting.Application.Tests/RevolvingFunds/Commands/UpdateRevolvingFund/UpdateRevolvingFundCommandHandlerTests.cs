using Accounting.Application.RevolvingFunds.Commands.UpdateRevolvingFund;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;
using Moq;

namespace Accounting.Application.Tests.RevolvingFunds.Commands.UpdateRevolvingFund;

public sealed class UpdateRevolvingFundCommandHandlerTests
{
    private static UpdateRevolvingFundCommand ValidCommand(Guid id) => new(
        Id: id,
        Code: "02",
        Name: "تنخواه به‌روزشده",
        Description: "توضیحات جدید",
        DefaultAmount: 2000000m,
        AccountCodeId: Guid.NewGuid(),
        Year: "1405")
    {
        VahedCode = "0002",
    };

    private static TB_REVOLVING_FUND ExistingEntity(Guid id, bool? isDeleted = false) => new()
    {
        ID = id,
        CODE = "01",
        NAME = "تنخواه قدیمی",
        DESCRIPTION = "قدیمی",
        DEFAULTAMOUNT = 1000000m,
        ACCOUNTCODE_ID = Guid.NewGuid(),
        VAHEDCODE = "0001",
        YEAR = "1404",
        ADDUSERID = "creator1",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CHANGEUSERID = null,
        UPDATEDDATE = null,
        ISDELETED = isDeleted,
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "editor1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_ExistingNonDeletedRecord_WritesAllWritableFieldsFromCommand()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IRevolvingFundRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.Code, entity.CODE);
        Assert.Equal(command.Name, entity.NAME);
        Assert.Equal(command.Description, entity.DESCRIPTION);
        Assert.Equal(command.DefaultAmount, entity.DEFAULTAMOUNT);
        Assert.Equal(command.AccountCodeId, entity.ACCOUNTCODE_ID);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.Year, entity.YEAR);
    }

    [Fact]
    public async Task Handle_SetsChangeUserIdFromCurrentUser_AndStampsUpdatedDate()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IRevolvingFundRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr02");

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal("srvusr02", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_NeverMutatesIdentityOrCreationAuditOrIsDeleted()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var originalId = entity.ID;
        var originalAddUserId = entity.ADDUSERID;
        var originalCreatedDate = entity.CREATEDDATE;
        var originalIsDeleted = entity.ISDELETED;

        var repository = new Mock<IRevolvingFundRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal(originalId, entity.ID);
        Assert.Equal(originalAddUserId, entity.ADDUSERID);
        Assert.Equal(originalCreatedDate, entity.CREATEDDATE);
        Assert.Equal(originalIsDeleted, entity.ISDELETED);
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IRevolvingFundRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_REVOLVING_FUND?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordIsSoftDeleted_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: true);
        var repository = new Mock<IRevolvingFundRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordWithNullIsDeleted_IsTreatedAsNotDeleted_AndUpdateSucceeds()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: null);
        var repository = new Mock<IRevolvingFundRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var command = ValidCommand(id);

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.Code, entity.CODE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingRecord_CallsSaveChangesExactlyOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IRevolvingFundRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IRevolvingFundRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsVahedCodeFromCommandAtFaceValue()
    {
        // The handler itself just maps request.VahedCode onto the entity — it does not read
        // ICurrentUser.VahedCode directly. Forgery prevention is VahedScopeBehavior's job (see
        // the dedicated pipeline test below); this test only proves the mapping is faithful.
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IRevolvingFundRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id) with { VahedCode = "0009" };

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal("0009", entity.VAHEDCODE);
    }

    [Fact]
    public async Task Handle_ThroughVahedScopeBehavior_ClientSuppliedVahedCodeIsDiscarded_ServerValueIsWritten()
    {
        // End-to-end forgery-prevention proof: even when a "client" manages to populate
        // command.VahedCode with a forged value before dispatch, running the command through
        // VahedScopeBehavior — exactly as the real MediatR pipeline does — overwrites it
        // unconditionally with ICurrentUser.VahedCode before the handler ever sees it.
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IRevolvingFundRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");

        var handler = new UpdateRevolvingFundCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<UpdateRevolvingFundCommand, Unit>(currentUser.Object);
        var forgedCommand = ValidCommand(id) with { VahedCode = "9999" };

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
