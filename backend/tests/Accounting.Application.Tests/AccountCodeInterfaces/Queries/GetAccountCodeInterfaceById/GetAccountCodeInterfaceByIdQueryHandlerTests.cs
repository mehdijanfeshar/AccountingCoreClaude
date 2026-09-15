using Accounting.Application.AccountCodeInterfaces.Queries;
using Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaceById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.AccountCodeInterfaces.Queries.GetAccountCodeInterfaceById;

public sealed class GetAccountCodeInterfaceByIdQueryHandlerTests
{
    private static AccountCodeInterfaceDto SampleDto(Guid id) => new(
        Id: id,
        Type: true,
        AccountCodeId: Guid.NewGuid(),
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_ExistingId_ReturnsRepositoryDto()
    {
        var id = Guid.NewGuid();
        var expected = SampleDto(id);
        var readRepository = new Mock<IAccountCodeInterfaceReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetAccountCodeInterfaceByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAccountCodeInterfaceByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IAccountCodeInterfaceReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountCodeInterfaceDto?)null);

        var handler = new GetAccountCodeInterfaceByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAccountCodeInterfaceByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IAccountCodeInterfaceReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((AccountCodeInterfaceDto?)null);

        var handler = new GetAccountCodeInterfaceByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetAccountCodeInterfaceByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetAccountCodeInterfaceByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
