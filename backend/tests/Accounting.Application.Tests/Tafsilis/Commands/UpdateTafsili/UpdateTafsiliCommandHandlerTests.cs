using Accounting.Application.Tafsilis.Commands.UpdateTafsili;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Tafsilis.Commands.UpdateTafsili;

public sealed class UpdateTafsiliCommandHandlerTests
{
    private static UpdateTafsiliCommand ValidCommand(
        Guid id,
        IReadOnlyList<Guid>? tafsilGroupIds = null,
        VahedCategory? tafsilGroupLinkVahedType = null) => new(
        Id: id,
        TafsiliCode: "002",
        TafsiliName: "تفصیلی دو",
        TafsilDesc: "توضیحات جدید",
        IsActive: TafsiliActiveState.DeActive,
        PersonType: PersonTypes.Legal,
        Owner: Owners.Global,
        VahedType: VahedCategory.Insurance,
        TafsilGroupIds: tafsilGroupIds ?? Array.Empty<Guid>(),
        TafsilGroupLinkVahedType: tafsilGroupLinkVahedType);

    private static TB_TAFSILI ExistingEntity(Guid id, bool? isDeleted = false) => new()
    {
        ID = id,
        TAFSILI_CODE = "001",
        TAFSILI_NAME = "تفصیلی یک",
        TAFSIL_DESC = "قدیمی",
        ISACTIVE = TafsiliActiveState.IsActive,
        PERSONTYPE = null,
        OWNER = null,
        VAHEDTYPE = null,
        VAHEDCODE = "1001",
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

    private static Mock<ITafsiliRepository> RepositoryWithNoExistingLinks(Guid id, TB_TAFSILI entity)
    {
        var repository = new Mock<ITafsiliRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repository
            .Setup(r => r.GetTafsiliGroupLinksAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TB_TAFSIL_LINK_TAFSILGROUP>());
        return repository;
    }

    [Fact]
    public async Task Handle_ExistingNonDeletedRecord_WritesAllWritableFieldsFromCommand()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = RepositoryWithNoExistingLinks(id, entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.TafsiliCode, entity.TAFSILI_CODE);
        Assert.Equal(command.TafsiliName, entity.TAFSILI_NAME);
        Assert.Equal(command.TafsilDesc, entity.TAFSIL_DESC);
        Assert.Equal(command.IsActive, entity.ISACTIVE);
        Assert.Equal(command.PersonType, entity.PERSONTYPE);
        Assert.Equal(command.Owner, entity.OWNER);
        Assert.Equal(command.VahedType, entity.VAHEDTYPE);
    }

    [Fact]
    public async Task Handle_SetsChangeUserIdFromCurrentUser_AndStampsUpdatedDate()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = RepositoryWithNoExistingLinks(id, entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr02");

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal("srvusr02", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
    }

    [Fact]
    public async Task Handle_NeverMutatesIdentityOrCreationAudit()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var originalId = entity.ID;
        var originalAddUserId = entity.ADDUSERID;
        var originalCreatedDate = entity.CREATEDDATE;

        var repository = RepositoryWithNoExistingLinks(id, entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal(originalId, entity.ID);
        Assert.Equal(originalAddUserId, entity.ADDUSERID);
        Assert.Equal(originalCreatedDate, entity.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_RestampsVahedCodeFromVahedScopedRequest_NeverPreservingTheOriginal()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        Assert.Equal("1001", entity.VAHEDCODE);

        var repository = RepositoryWithNoExistingLinks(id, entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id);
        command.VahedCode = "2002"; // simulates VahedScopeBehavior having already run

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal("2002", entity.VAHEDCODE);
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<ITafsiliRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((TB_TAFSILI?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordIsSoftDeleted_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: true);
        var repository = new Mock<ITafsiliRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LinkReconciliation_SoftDeletesDroppedLinks_AddsNewOnes_KeepsUnchangedOnes()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var keepGroupId = Guid.NewGuid();
        var dropGroupId = Guid.NewGuid();
        var addGroupId = Guid.NewGuid();
        var keepLink = new TB_TAFSIL_LINK_TAFSILGROUP { ID = Guid.NewGuid(), TAFSIL_ID = id, TAFSILGROUP_ID = keepGroupId, ISDELETED = false };
        var dropLink = new TB_TAFSIL_LINK_TAFSILGROUP { ID = Guid.NewGuid(), TAFSIL_ID = id, TAFSILGROUP_ID = dropGroupId, ISDELETED = false };

        var repository = new Mock<ITafsiliRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repository
            .Setup(r => r.GetTafsiliGroupLinksAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { keepLink, dropLink });
        TB_TAFSIL_LINK_TAFSILGROUP? addedLink = null;
        repository
            .Setup(r => r.AddTafsiliGroupLinkAsync(It.IsAny<TB_TAFSIL_LINK_TAFSILGROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSIL_LINK_TAFSILGROUP, CancellationToken>((l, _) => addedLink = l)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id, new[] { keepGroupId, addGroupId }), CancellationToken.None);

        Assert.False(keepLink.ISDELETED);
        Assert.True(dropLink.ISDELETED);
        Assert.NotNull(addedLink);
        Assert.Equal(addGroupId, addedLink!.TAFSILGROUP_ID);
        Assert.Equal(id, addedLink.TAFSIL_ID);
        repository.Verify(
            r => r.AddTafsiliGroupLinkAsync(It.IsAny<TB_TAFSIL_LINK_TAFSILGROUP>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LinkReconciliation_NewLinkGetsRequestedVahedType_KeptLinkIsUntouched()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var keepGroupId = Guid.NewGuid();
        var addGroupId = Guid.NewGuid();
        var keepLink = new TB_TAFSIL_LINK_TAFSILGROUP
        {
            ID = Guid.NewGuid(),
            TAFSIL_ID = id,
            TAFSILGROUP_ID = keepGroupId,
            VAHEDTYPE = (short)VahedCategory.Insurance,
            ISDELETED = false,
        };

        var repository = new Mock<ITafsiliRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repository
            .Setup(r => r.GetTafsiliGroupLinksAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { keepLink });
        TB_TAFSIL_LINK_TAFSILGROUP? addedLink = null;
        repository
            .Setup(r => r.AddTafsiliGroupLinkAsync(It.IsAny<TB_TAFSIL_LINK_TAFSILGROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSIL_LINK_TAFSILGROUP, CancellationToken>((l, _) => addedLink = l)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(
            ValidCommand(id, new[] { keepGroupId, addGroupId }, VahedCategory.All),
            CancellationToken.None);

        Assert.NotNull(addedLink);
        Assert.Equal((short)VahedCategory.All, addedLink!.VAHEDTYPE);
        // The kept link's own VAHEDTYPE is never retroactively changed by this command.
        Assert.Equal((short)VahedCategory.Insurance, keepLink.VAHEDTYPE);
    }

    [Fact]
    public async Task Handle_ExistingRecord_CallsSaveChangesExactlyOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = RepositoryWithNoExistingLinks(id, entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<ITafsiliRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<string>(), token)).ReturnsAsync(entity);
        repository
            .Setup(r => r.GetTafsiliGroupLinksAsync(id, token))
            .ReturnsAsync(Array.Empty<TB_TAFSIL_LINK_TAFSILGROUP>());

        var handler = new UpdateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, It.IsAny<string>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
