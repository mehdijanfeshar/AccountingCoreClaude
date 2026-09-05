using Accounting.Application.RevolvingFunds.Queries;
using Accounting.Application.RevolvingFunds.Queries.GetRevolvingFunds;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.RevolvingFunds.Queries.GetRevolvingFunds;

public sealed class GetRevolvingFundsQueryHandlerTests
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
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IRevolvingFundReadRepository>();
        var expected = new PagedResult<RevolvingFundDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetRevolvingFundsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetRevolvingFundsQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IRevolvingFundReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<RevolvingFundDto>());

        var handler = new GetRevolvingFundsQueryHandler(readRepository.Object);

        await handler.Handle(new GetRevolvingFundsQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetRevolvingFundsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
