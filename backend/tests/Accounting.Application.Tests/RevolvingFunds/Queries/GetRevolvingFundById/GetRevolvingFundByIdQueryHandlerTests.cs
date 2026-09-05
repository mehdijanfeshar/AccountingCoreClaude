using Accounting.Application.RevolvingFunds.Queries;
using Accounting.Application.RevolvingFunds.Queries.GetRevolvingFundById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.RevolvingFunds.Queries.GetRevolvingFundById;

public sealed class GetRevolvingFundByIdQueryHandlerTests
{
    private static RevolvingFundDto SampleDto(Guid id) => new(
        Id: id,
        Code: "01",
        Name: "تنخواه شماره یک",
        Description: null,
        DefaultAmount: 1000000m,
        AccountCodeId: Guid.NewGuid(),
        VahedCode: "0001",
        Year: "1404",
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
        var readRepository = new Mock<IRevolvingFundReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetRevolvingFundByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetRevolvingFundByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IRevolvingFundReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RevolvingFundDto?)null);

        var handler = new GetRevolvingFundByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetRevolvingFundByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IRevolvingFundReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((RevolvingFundDto?)null);

        var handler = new GetRevolvingFundByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetRevolvingFundByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetRevolvingFundByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
