using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Reports.CrossTab;
using Accounting.Application.Reports.CrossTab.GetCrossTabReport;
using Moq;

namespace Accounting.Application.Tests.Reports.CrossTab;

public sealed class GetCrossTabReportQueryHandlerTests
{
    private static GetCrossTabReportQuery Query() => new(
        "1403", CrossTabDimension.Tafsili1, CrossTabDimension.Moin,
        null, null, null, null, null, null);

    [Fact]
    public async Task Handle_ReturnsRepositoryResult()
    {
        var expected = new CrossTabResultDto(
            CrossTabDimension.Tafsili1, "تفصیلی ۱",
            CrossTabDimension.Moin, "معین",
            [], [], 0m, 0m, 0, false);

        var repository = new Mock<ICrossTabReportReadRepository>();
        repository
            .Setup(r => r.GetAsync(It.IsAny<GetCrossTabReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetCrossTabReportQueryHandler(repository.Object);

        Assert.Same(expected, await handler.Handle(Query(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PassesTheQueryAndTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var query = Query();

        var repository = new Mock<ICrossTabReportReadRepository>();
        repository
            .Setup(r => r.GetAsync(query, token))
            .ReturnsAsync(new CrossTabResultDto(
                CrossTabDimension.Tafsili1, "تفصیلی ۱", CrossTabDimension.Moin, "معین",
                [], [], 0m, 0m, 0, false));

        var handler = new GetCrossTabReportQueryHandler(repository.Object);

        await handler.Handle(query, token);

        repository.Verify(r => r.GetAsync(query, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetCrossTabReportQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }

    /// <summary>
    /// The view carries <c>VAHEDCODE</c>, so without server-assigned scoping this report would
    /// expose every unit's figures at once. <c>VahedScopeBehavior</c> only fills the property on
    /// requests that declare the marker.
    /// </summary>
    [Fact]
    public void Query_IsVahedScoped()
    {
        Assert.True(typeof(IVahedScopedQuery).IsAssignableFrom(typeof(GetCrossTabReportQuery)));
    }
}
