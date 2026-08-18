using Accounting.Application.Accounts.Commands.CreateAccountCode;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Accounts.Commands.CreateAccountCode;

public sealed class CreateAccountCodeCommandHandlerTests
{
    private static CreateAccountCodeCommand ValidCommand() => new(
        TypeCode: true,
        ParentId: null,
        AccCode: "100100",
        AccCodeName: "بانک ملی",
        TypeActivity: true,
        SourceAndConsumeId: null,
        IdentyGroupsId: null,
        TypeAccCode: true,
        AddUserId: "user1",
        MoInforClose: null,
        TypeAction: null);

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IAccountCodeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TB_ACCOUNTCODE? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNTCODE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNTCODE, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateAccountCodeCommandHandler(repository.Object, unitOfWork.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.TypeCode, staged!.TYPECODE);
        Assert.Equal(command.ParentId, staged.PARENTID);
        Assert.Equal(command.AccCode, staged.ACCCODE);
        Assert.Equal(command.AccCodeName, staged.ACCCODENAME);
        Assert.Equal(command.TypeActivity, staged.TYPEACTIVITY);
        Assert.Equal(command.SourceAndConsumeId, staged.SOURCEANDCONSUME_ID);
        Assert.Equal(command.IdentyGroupsId, staged.IDENTYGROUPS_ID);
        Assert.Equal(command.TypeAccCode, staged.TYPEACCCODE);
        Assert.Equal(command.AddUserId, staged.ADDUSERID);
        Assert.Equal(command.MoInforClose, staged.MOINFORCLOSE);
        Assert.Equal(command.TypeAction, staged.TYPEACTION);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonNullCreatedDate()
    {
        var repository = new Mock<IAccountCodeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TB_ACCOUNTCODE? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNTCODE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNTCODE, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateAccountCodeCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotNull(staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IAccountCodeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TB_ACCOUNTCODE? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNTCODE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNTCODE, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateAccountCodeCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IAccountCodeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new CreateAccountCodeCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_ACCOUNTCODE>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IAccountCodeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNTCODE>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateAccountCodeCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IAccountCodeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateAccountCodeCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_ACCOUNTCODE>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
