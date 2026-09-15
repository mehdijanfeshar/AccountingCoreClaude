using Accounting.Application.AccountExceptions.Queries;
using Accounting.Application.AccountExceptions.Queries.GetAccountExceptionById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.AccountExceptions.Queries.GetAccountExceptionById;

public sealed class GetAccountExceptionByIdQueryHandlerTests
{
    private static AccountExceptionDto SampleDto(Guid id) => new(
        Id: id,
        AccountCoeId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
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
        var readRepository = new Mock<IAccountExceptionReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetAccountExceptionByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAccountExceptionByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IAccountExceptionReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountExceptionDto?)null);

        var handler = new GetAccountExceptionByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAccountExceptionByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IAccountExceptionReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((AccountExceptionDto?)null);

        var handler = new GetAccountExceptionByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetAccountExceptionByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetAccountExceptionByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
