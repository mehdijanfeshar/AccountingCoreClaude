using Accounting.Application.VahedInfos.Commands.CreateVahedInfo;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.VahedInfos.Commands.CreateVahedInfo;

public sealed class CreateVahedInfoCommandHandlerTests
{
    private static CreateVahedInfoCommand ValidCommand() => new(
        VahedCode: "0001",
        VahedName: "واحد مرکزی",
        CityId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        ParentId: null);

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IVahedInfoRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TB_VAHED_INFO? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VAHED_INFO>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VAHED_INFO, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.VahedCode, staged!.VAHEDCODE);
        Assert.Equal(command.VahedName, staged.VAHEDNAME);
        Assert.Equal(command.CityId, staged.CITY_ID);
        Assert.Equal(command.VahedTypeId, staged.VAHEDTYPE_ID);
        Assert.Equal(command.ParentId, staged.PARENT_ID);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IVahedInfoRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TB_VAHED_INFO? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VAHED_INFO>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VAHED_INFO, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IVahedInfoRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new CreateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_VAHED_INFO>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IVahedInfoRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_VAHED_INFO>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IVahedInfoRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_VAHED_INFO>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnICurrentUser()
    {
        // Structural guarantee: TB_VAHED_INFO has no ADDUSERID (or any audit) column to stamp,
        // so this handler has no use for ICurrentUser at all — unlike every other Create
        // handler in this project.
        var parameterTypes = typeof(CreateVahedInfoCommandHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(ICurrentUser), parameterTypes);
    }
}
