using Accounting.Application.WorkShops.Queries;
using Accounting.Application.WorkShops.Queries.GetWorkShopById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.WorkShops.Queries.GetWorkShopById;

public sealed class GetWorkShopByIdQueryHandlerTests
{
    private static WorkShopDto SampleDto(Guid id) => new(
        Id: id,
        AccountCodeId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        WorkShopName: "کارگاه شماره یک",
        WorkShopCode: "WS001",
        VahedCode: "0001",
        IsActive: true,
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
        var readRepository = new Mock<IWorkShopReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetWorkShopByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetWorkShopByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IWorkShopReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkShopDto?)null);

        var handler = new GetWorkShopByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetWorkShopByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IWorkShopReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((WorkShopDto?)null);

        var handler = new GetWorkShopByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetWorkShopByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetWorkShopByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
