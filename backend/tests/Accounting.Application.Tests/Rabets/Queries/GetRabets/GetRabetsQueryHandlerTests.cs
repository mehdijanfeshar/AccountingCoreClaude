using Accounting.Application.Rabets.Queries;
using Accounting.Application.Rabets.Queries.GetRabets;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Rabets.Queries.GetRabets;

public sealed class GetRabetsQueryHandlerTests
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
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IRabetReadRepository>();
        var expected = new PagedResult<RabetDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetRabetsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetRabetsQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IRabetReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<RabetDto>());

        var handler = new GetRabetsQueryHandler(readRepository.Object);

        await handler.Handle(new GetRabetsQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetRabetsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
