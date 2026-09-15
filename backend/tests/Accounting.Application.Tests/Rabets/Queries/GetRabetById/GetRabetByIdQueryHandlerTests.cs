using Accounting.Application.Rabets.Queries;
using Accounting.Application.Rabets.Queries.GetRabetById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Rabets.Queries.GetRabetById;

public sealed class GetRabetByIdQueryHandlerTests
{
    private static RabetDto SampleDto(Guid id) => new(
        Id: id,
        RabetTypeId: Guid.NewGuid(),
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
        var readRepository = new Mock<IRabetReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetRabetByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetRabetByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IRabetReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RabetDto?)null);

        var handler = new GetRabetByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetRabetByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IRabetReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((RabetDto?)null);

        var handler = new GetRabetByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetRabetByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetRabetByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
