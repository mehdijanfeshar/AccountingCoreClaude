using Accounting.Application.PreDescribs.Commands.UpdatePreDescrib;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;
using Moq;

namespace Accounting.Application.Tests.PreDescribs.Commands.UpdatePreDescrib;

public sealed class UpdatePreDescribCommandHandlerTests
{
    private static UpdatePreDescribCommand ValidCommand(Guid id) => new(
        Id: id,
        AccountId: null,
        Descrip: "توضیحات جدید",
        FlagVoucher: true)
    {
        VahedCode = "0002",
    };

    private static TB_PREDESCRIB ExistingEntity(Guid id) => new()
    {
        ID = id,
        ACCOUNTID = null,
        DESCRIP = "توضیحات قدیمی",
        ADDUSERID = "creator1",
        VAHEDCODE = "0001",
        FLAGVOUCHER = false,
    };

    [Fact]
    public async Task Handle_ExistingRecord_WritesAllWritableFieldsFromCommand()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IPreDescribRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdatePreDescribCommandHandler(repository.Object, unitOfWork.Object);
        var command = ValidCommand(id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.AccountId, entity.ACCOUNTID);
        Assert.Equal(command.Descrip, entity.DESCRIP);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
        Assert.Equal(command.FlagVoucher, entity.FLAGVOUCHER);
    }

    [Fact]
    public async Task Handle_NeverMutatesIdOrAddUserId()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var originalId = entity.ID;
        var originalAddUserId = entity.ADDUSERID;

        var repository = new Mock<IPreDescribRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdatePreDescribCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal(originalId, entity.ID);
        Assert.Equal(originalAddUserId, entity.ADDUSERID);
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IPreDescribRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_PREDESCRIB?)null);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdatePreDescribCommandHandler(repository.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingRecord_CallsSaveChangesExactlyOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IPreDescribRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdatePreDescribCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IPreDescribRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new UpdatePreDescribCommandHandler(repository.Object, unitOfWork.Object);

        await handler.Handle(ValidCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnICurrentUser()
    {
        // Structural guarantee: TB_PREDESCRIB has no CHANGEUSERID/UPDATEDDATE audit columns to
        // stamp and ADDUSERID is immutable creation audit on Update, so this handler has no use
        // for ICurrentUser at all — unlike every other Update handler in this project.
        var parameterTypes = typeof(UpdatePreDescribCommandHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(ICurrentUser), parameterTypes);
    }

    [Fact]
    public async Task Handle_ThroughVahedScopeBehavior_ClientSuppliedVahedCodeIsDiscarded_ServerValueIsWritten()
    {
        // End-to-end forgery-prevention proof: even when a "client" manages to populate
        // command.VahedCode with a forged value before dispatch, running the command through
        // VahedScopeBehavior — exactly as the real MediatR pipeline does — overwrites it
        // unconditionally with ICurrentUser.VahedCode before the handler ever sees it. The
        // behavior's ICurrentUser is independent of the handler's own dependencies (this handler
        // has none — see Constructor_DoesNotDependOnICurrentUser above).
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IPreDescribRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");

        var handler = new UpdatePreDescribCommandHandler(repository.Object, unitOfWork.Object);
        var behavior = new VahedScopeBehavior<UpdatePreDescribCommand, Unit>(currentUser.Object);
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
