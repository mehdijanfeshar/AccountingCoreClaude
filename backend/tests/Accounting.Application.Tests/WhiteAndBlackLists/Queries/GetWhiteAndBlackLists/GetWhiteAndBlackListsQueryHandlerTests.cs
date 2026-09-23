using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;

public sealed class GetWhiteAndBlackListsQueryHandlerTests
{
    private static WhiteAndBlackListDto SampleDto(Guid id) => new(
        Id: id,
        AccountCodeId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false,
        FromAuthorizedDate: "14030101",
        ToAuthorizedDate: "14031231",
        FromLimitationDate: "14030101",
        ToLimitationDate: "14031231",
        State: WhiteBlackListState.Blacklisted);

    [Fact]
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IWhiteAndBlackListReadRepository>();
        var expected = new PagedResult<WhiteAndBlackListDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<WhiteAndBlackListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetWhiteAndBlackListsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetWhiteAndBlackListsQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(
            r => r.GetPagedAsync(2, 25, It.IsAny<WhiteAndBlackListFilter>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IWhiteAndBlackListReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, It.IsAny<WhiteAndBlackListFilter>(), token))
            .ReturnsAsync(new PagedResult<WhiteAndBlackListDto>());

        var handler = new GetWhiteAndBlackListsQueryHandler(readRepository.Object);

        await handler.Handle(new GetWhiteAndBlackListsQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(
            r => r.GetPagedAsync(1, 20, It.IsAny<WhiteAndBlackListFilter>(), token),
            Times.Once);
    }

    /// <summary>
    /// A query with no filter arguments must reach the repository as an empty filter, not as one
    /// that silently narrows the page. This is the default every existing caller relies on.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoFilterArguments_PassesAnAllNullFilter()
    {
        var readRepository = new Mock<IWhiteAndBlackListReadRepository>();
        WhiteAndBlackListFilter? captured = null;
        readRepository
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<WhiteAndBlackListFilter>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, WhiteAndBlackListFilter, CancellationToken>((_, _, filter, _) => captured = filter)
            .ReturnsAsync(new PagedResult<WhiteAndBlackListDto>());

        var handler = new GetWhiteAndBlackListsQueryHandler(readRepository.Object);

        await handler.Handle(new GetWhiteAndBlackListsQuery(PageNumber: 1, PageSize: 20), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(WhiteAndBlackListFilter.None, captured);
    }

    /// <summary>
    /// Every filter the query accepts must arrive at the repository. Written as one test over all
    /// seven rather than seven tests, because the failure being guarded against is a single
    /// forgotten line in the handler's projection — which this catches for any of them.
    /// </summary>
    [Fact]
    public async Task Handle_ForwardsEveryFilterToRepository()
    {
        var accountCodeId = Guid.NewGuid();
        var vahedTypeId = Guid.NewGuid();
        var readRepository = new Mock<IWhiteAndBlackListReadRepository>();
        WhiteAndBlackListFilter? captured = null;
        readRepository
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<WhiteAndBlackListFilter>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, WhiteAndBlackListFilter, CancellationToken>((_, _, filter, _) => captured = filter)
            .ReturnsAsync(new PagedResult<WhiteAndBlackListDto>());

        var handler = new GetWhiteAndBlackListsQueryHandler(readRepository.Object);

        await handler.Handle(
            new GetWhiteAndBlackListsQuery(
                PageNumber: 1,
                PageSize: 20,
                AccountCodeId: accountCodeId,
                VahedTypeId: vahedTypeId,
                State: WhiteBlackListState.SystemOnly,
                FromAuthorizedDate: "14040101",
                ToAuthorizedDate: "14041229",
                FromLimitationDate: "14050101",
                ToLimitationDate: "14051229"),
            CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(accountCodeId, captured!.AccountCodeId);
        Assert.Equal(vahedTypeId, captured.VahedTypeId);
        Assert.Equal(WhiteBlackListState.SystemOnly, captured.State);
        Assert.Equal("14040101", captured.FromAuthorizedDate);
        Assert.Equal("14041229", captured.ToAuthorizedDate);
        Assert.Equal("14050101", captured.FromLimitationDate);
        Assert.Equal("14051229", captured.ToLimitationDate);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetWhiteAndBlackListsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
