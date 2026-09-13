using Accounting.Application.Tafsilis.Commands.CreateTafsili;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Tafsilis.Commands.CreateTafsili;

public sealed class CreateTafsiliCommandHandlerTests
{
    private static CreateTafsiliCommand ValidCommand(IReadOnlyList<Guid>? tafsilGroupIds = null) => new(
        TafsiliCode: "001",
        TafsiliName: "تفصیلی یک",
        TafsilDesc: "توضیحات",
        IsActive: true,
        PersonType: true,
        Owner: false,
        VahedType: null,
        TafsilGroupIds: tafsilGroupIds ?? Array.Empty<Guid>())
    {
        VahedCode = "1001",
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<ITafsiliRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_TAFSILI? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSILI, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.TafsiliCode, staged!.TAFSILI_CODE);
        Assert.Equal(command.TafsiliName, staged.TAFSILI_NAME);
        Assert.Equal(command.TafsilDesc, staged.TAFSIL_DESC);
        Assert.Equal(command.IsActive, staged.ISACTIVE);
        Assert.Equal(command.PersonType, staged.PERSONTYPE);
        Assert.Equal(command.Owner, staged.OWNER);
        Assert.Equal(command.VahedType, staged.VAHEDTYPE);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<ITafsiliRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_TAFSILI? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSILI, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<ITafsiliRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_TAFSILI? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSILI, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotEqual(default, staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<ITafsiliRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_TAFSILI? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSILI, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_WithTafsilGroupIds_AddsOneLinkPerGroupId_ScopedToNewEntity_WithNullVahedType()
    {
        var groupIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var repository = new Mock<ITafsiliRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("linker1");
        TB_TAFSILI? staged = null;
        var stagedLinks = new List<TB_TAFSIL_LINK_TAFSILGROUP>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSILI, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);
        repository
            .Setup(r => r.AddTafsiliGroupLinkAsync(It.IsAny<TB_TAFSIL_LINK_TAFSILGROUP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TAFSIL_LINK_TAFSILGROUP, CancellationToken>((link, _) => stagedLinks.Add(link))
            .Returns(Task.CompletedTask);

        var handler = new CreateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(groupIds);

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(2, stagedLinks.Count);
        Assert.All(stagedLinks, l => Assert.Equal(staged!.ID, l.TAFSIL_ID));
        Assert.Equal(groupIds.OrderBy(g => g), stagedLinks.Select(l => l.TAFSILGROUP_ID).OrderBy(g => g));
        Assert.All(stagedLinks, l => Assert.Equal(command.VahedCode, l.VAHEDCODE));
        Assert.All(stagedLinks, l => Assert.Null(l.VAHEDTYPE));
        Assert.All(stagedLinks, l => Assert.False(l.ISDELETED));
    }

    [Fact]
    public async Task Handle_NoTafsilGroupIds_NeverCallsAddTafsiliGroupLinkAsync()
    {
        var repository = new Mock<ITafsiliRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddTafsiliGroupLinkAsync(It.IsAny<TB_TAFSIL_LINK_TAFSILGROUP>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<ITafsiliRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_TAFSILI>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<ITafsiliRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateTafsiliCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_TAFSILI>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
