using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.AccountCodes.Queries.GetTafsiliLevelItems;

public sealed class GetTafsiliLevelItemsQueryHandlerTests
{
    [Fact]
    public async Task Handle_PassesAllArguments_AndReturnsRepositoryResult()
    {
        var accountCodeId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var expected = new PagedResult<TafsiliLookupItemDto>
        {
            Items = new[] { new TafsiliLookupItemDto(Guid.NewGuid(), "100", "نمونه", "100 - نمونه") },
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 11,
        };

        var readRepository = new Mock<ITafsiliLookupReadRepository>();
        readRepository
            .Setup(r => r.GetSelectableItemsAsync(
                accountCodeId, levelId, "term", "0001", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetTafsiliLevelItemsQueryHandler(readRepository.Object);
        var query = new GetTafsiliLevelItemsQuery(accountCodeId, levelId, "term", 2, 10) { VahedCode = "0001" };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Same(expected, result);
        readRepository.Verify(
            r => r.GetSelectableItemsAsync(accountCodeId, levelId, "term", "0001", 2, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PassesRequestVahedCodeToRepository_AtFaceValue()
    {
        // Proves the handler trusts request.VahedCode as-is: by the time this handler runs,
        // VahedScopeBehavior has already overwritten it with the authenticated caller's own unit
        // code, so the handler must forward exactly that value, not derive its own, and must
        // never read ICurrentUser directly (there is no such dependency in the constructor below).
        var readRepository = new Mock<ITafsiliLookupReadRepository>();
        readRepository
            .Setup(r => r.GetSelectableItemsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), "0007", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<TafsiliLookupItemDto>());

        var handler = new GetTafsiliLevelItemsQueryHandler(readRepository.Object);
        var query = new GetTafsiliLevelItemsQuery(Guid.NewGuid(), Guid.NewGuid(), null, 1, 20) { VahedCode = "0007" };

        await handler.Handle(query, CancellationToken.None);

        readRepository.Verify(
            r => r.GetSelectableItemsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), "0007", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork_OrICurrentUser()
    {
        var parameterTypes = typeof(GetTafsiliLevelItemsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
        Assert.DoesNotContain(typeof(ICurrentUser), parameterTypes);
    }
}
