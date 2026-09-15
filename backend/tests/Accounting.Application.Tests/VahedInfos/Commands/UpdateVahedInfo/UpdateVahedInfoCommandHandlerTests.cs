using Accounting.Application.VahedInfos.Commands.UpdateVahedInfo;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.VahedInfos.Commands.UpdateVahedInfo;

public sealed class UpdateVahedInfoCommandHandlerTests
{
    private static UpdateVahedInfoCommand ValidCommand(Guid id) => new(
        Id: id,
        VahedCode: "0002",
        VahedName: "واحد جدید",
        CityId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        ParentId: Guid.NewGuid());

    private static TB_VAHED_INFO ExistingEntity(Guid id) => new()
    {
        ID = id,
        VAHEDCODE = "0001",
        VAHEDNAME = "واحد قدیمی",
        CITY_ID = Guid.NewGuid(),
        VAHEDTYPE_ID = Guid.NewGuid(),
        PARENT_ID = null,
    };

    [Fact]
    public async Task Handle_ExistingRecord_WritesAllWritableFieldsFromCommand()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVahedInfoRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);
        var command = ValidCommand(id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.VahedName, entity.VAHEDNAME);
        Assert.Equal(command.CityId, entity.CITY_ID);
        Assert.Equal(command.VahedTypeId, entity.VAHEDTYPE_ID);
        Assert.Equal(command.ParentId, entity.PARENT_ID);
    }

    [Fact]
    public async Task Handle_NeverMutatesId()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var originalId = entity.ID;

        var repository = new Mock<IVahedInfoRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal(originalId, entity.ID);
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IVahedInfoRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_VAHED_INFO?)null);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingRecord_CallsSaveChangesExactlyOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVahedInfoRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IVahedInfoRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new UpdateVahedInfoCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnICurrentUser()
    {
        // Structural guarantee: TB_VAHED_INFO has no CHANGEUSERID/UPDATEDDATE audit columns to
        // stamp, so this handler has no use for ICurrentUser at all — unlike every other Update
        // handler in this project (except UpdatePreDescribCommandHandler, which shares this
        // trait for the same reason).
        var parameterTypes = typeof(UpdateVahedInfoCommandHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(ICurrentUser), parameterTypes);
    }
}
