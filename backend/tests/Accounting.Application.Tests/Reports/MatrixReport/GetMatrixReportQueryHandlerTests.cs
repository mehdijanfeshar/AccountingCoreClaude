using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Reports.MatrixReport;
using Accounting.Application.Reports.MatrixReport.GetMatrixReport;
using Moq;

namespace Accounting.Application.Tests.Reports.MatrixReport;

public sealed class GetMatrixReportQueryHandlerTests
{
    private static GetMatrixReportQuery Query() => new(
        "1403", MatrixDimension.Tafsili1, MatrixDimension.Moin,
        null, null, null, null, null, null);

    [Fact]
    public async Task Handle_ReturnsRepositoryResult()
    {
        var expected = new MatrixResultDto(
            MatrixDimension.Tafsili1, "تفصیلی ۱",
            MatrixDimension.Moin, "معین",
            [], [], 0m, 0m, 0, false);

        var repository = new Mock<IMatrixReportReadRepository>();
        repository
            .Setup(r => r.GetAsync(It.IsAny<GetMatrixReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetMatrixReportQueryHandler(repository.Object);

        Assert.Same(expected, await handler.Handle(Query(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PassesTheQueryAndTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var query = Query();

        var repository = new Mock<IMatrixReportReadRepository>();
        repository
            .Setup(r => r.GetAsync(query, token))
            .ReturnsAsync(new MatrixResultDto(
                MatrixDimension.Tafsili1, "تفصیلی ۱", MatrixDimension.Moin, "معین",
                [], [], 0m, 0m, 0, false));

        var handler = new GetMatrixReportQueryHandler(repository.Object);

        await handler.Handle(query, token);

        repository.Verify(r => r.GetAsync(query, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetMatrixReportQueryHandler)
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
        Assert.True(typeof(IVahedScopedQuery).IsAssignableFrom(typeof(GetMatrixReportQuery)));
    }
}
