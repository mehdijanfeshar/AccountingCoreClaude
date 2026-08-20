using Accounting.Application.Accounts.Commands.UpdateAccountCode;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Accounts.Commands.UpdateAccountCode;

public sealed class UpdateAccountCodeCommandHandlerTests
{
    private static UpdateAccountCodeCommand ValidCommand(Guid id) => new(
        Id: id,
        TypeCode: true,
        ParentId: null,
        AccCode: "100200",
        AccCodeName: "بانک ملت",
        TypeActivity: true,
        SourceAndConsumeId: null,
        IdentyGroupsId: null,
        TypeAccCode: true,
        MoInforClose: null,
        TypeAction: null);

    private static TB_ACCOUNTCODE ExistingEntity(Guid id, bool? isDeleted = false) => new()
    {
        ID = id,
        TYPECODE = false,
        PARENTID = null,
        ACCCODE = "100100",
        ACCCODENAME = "بانک ملی",
        TYPEACTIVITY = false,
        SOURCEANDCONSUME_ID = null,
        IDENTYGROUPS_ID = null,
        TYPEACCCODE = false,
        MOINFORCLOSE = "OLD",
        TYPEACTION = null,
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
        var repository = new Mock<IAccountCodeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateAccountCodeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.TypeCode, entity.TYPECODE);
        Assert.Equal(command.ParentId, entity.PARENTID);
        Assert.Equal(command.AccCode, entity.ACCCODE);
        Assert.Equal(command.AccCodeName, entity.ACCCODENAME);
        Assert.Equal(command.TypeActivity, entity.TYPEACTIVITY);
        Assert.Equal(command.SourceAndConsumeId, entity.SOURCEANDCONSUME_ID);
        Assert.Equal(command.IdentyGroupsId, entity.IDENTYGROUPS_ID);
        Assert.Equal(command.TypeAccCode, entity.TYPEACCCODE);
        Assert.Equal(command.MoInforClose, entity.MOINFORCLOSE);
        Assert.Equal(command.TypeAction, entity.TYPEACTION);
    }

    [Fact]
    public async Task Handle_SetsChangeUserIdFromCurrentUser_AndStampsUpdatedDate()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IAccountCodeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr02");

        var handler = new UpdateAccountCodeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

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

        var repository = new Mock<IAccountCodeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateAccountCodeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        // Most important security assertion of this test class: identity and creation audit
        // must be immutable via Update, and ISDELETED must never be toggled by Update.
        Assert.Equal(originalId, entity.ID);
        Assert.Equal(originalAddUserId, entity.ADDUSERID);
        Assert.Equal(originalCreatedDate, entity.CREATEDDATE);
        Assert.Equal(originalIsDeleted, entity.ISDELETED);
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IAccountCodeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_ACCOUNTCODE?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateAccountCodeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordIsSoftDeleted_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: true);
        var repository = new Mock<IAccountCodeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateAccountCodeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingRecord_CallsSaveChangesExactlyOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IAccountCodeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateAccountCodeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IAccountCodeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new UpdateAccountCodeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_RecordWithNullIsDeleted_IsTreatedAsNotDeleted_AndUpdateSucceeds()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: null);
        var repository = new Mock<IAccountCodeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateAccountCodeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal("100200", entity.ACCCODE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
