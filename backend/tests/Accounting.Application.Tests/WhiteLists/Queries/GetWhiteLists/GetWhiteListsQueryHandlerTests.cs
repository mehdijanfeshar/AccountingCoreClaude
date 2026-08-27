using Accounting.Application.WhiteLists.Queries;
using Accounting.Application.WhiteLists.Queries.GetWhiteLists;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.WhiteLists.Queries.GetWhiteLists;

public sealed class GetWhiteListsQueryHandlerTests
{
    private static WhiteListDto SampleDto(Guid id) => new(
        Id: id,
        AccountCodeId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        VahedInfoId: Guid.NewGuid(),
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false,
        FromAuthorizedDate: "14030101",
        ToAuthorizedDate: "14031231",
        FromLimitationDate: "14030101",
        ToLimitationDate: "14031231");

    [Fact]
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IWhiteListReadRepository>();
        var expected = new PagedResult<WhiteListDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetWhiteListsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetWhiteListsQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IWhiteListReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<WhiteListDto>());

        var handler = new GetWhiteListsQueryHandler(readRepository.Object);

        await handler.Handle(new GetWhiteListsQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetWhiteListsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
