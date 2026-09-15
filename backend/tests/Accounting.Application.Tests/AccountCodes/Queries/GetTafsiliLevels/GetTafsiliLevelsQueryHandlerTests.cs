using Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.AccountCodes.Queries.GetTafsiliLevels;

public sealed class GetTafsiliLevelsQueryHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToRepository_WithAccountCodeIdAndToken_AndReturnsItsResult()
    {
        var accountCodeId = Guid.NewGuid();
        var expected = new List<TafsiliLevelDto>
        {
            new(Guid.NewGuid(), 1, "معین", true),
            new(Guid.NewGuid(), 2, "تفصیلی سطح ۲", true),
        };

        var readRepository = new Mock<ITafsiliLookupReadRepository>();
        using var cts = new CancellationTokenSource();
        readRepository
            .Setup(r => r.GetActiveLevelsAsync(accountCodeId, cts.Token))
            .ReturnsAsync(expected);

        var handler = new GetTafsiliLevelsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetTafsiliLevelsQuery(accountCodeId), cts.Token);

        Assert.Same(expected, result);
        readRepository.Verify(r => r.GetActiveLevelsAsync(accountCodeId, cts.Token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetTafsiliLevelsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
