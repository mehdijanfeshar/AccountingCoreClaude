using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.TrialBalance;
using Accounting.Application.Reports.TrialBalance.GetTrialBalance4;
using Moq;

namespace Accounting.Application.Tests.Reports.TrialBalance.GetTrialBalance4;

public sealed class GetTrialBalance4QueryHandlerTests
{
    private static TrialBalanceAggregateRow Row(
        decimal periodDebtor,
        decimal periodCreditor,
        decimal totalDebtor,
        decimal totalCreditor,
        decimal openingDebtor = 0m,
        decimal openingCreditor = 0m) => new()
    {
        Code = "110101",
        Description = "بانک ملت",
        PeriodDebtor = periodDebtor,
        PeriodCreditor = periodCreditor,
        OpeningDebtor = openingDebtor,
        OpeningCreditor = openingCreditor,
        TotalDebtor = totalDebtor,
        TotalCreditor = totalCreditor,
    };

    private static GetTrialBalance4Query Query() => new(
        Year: "1405",
        FromDate: "14050101",
        ToDate: "14051230",
        VahedCode: "0001",
        Level: TrialBalanceLevel.Moin,
        DocLife: null);

    [Fact]
    public async Task Handle_MapsCodeAndDescription_AndPeriodTurnoverDirectly()
    {
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                TrialBalanceLevel.Moin, "1405", "14050101", "14051230", "0001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Row(periodDebtor: 1000m, periodCreditor: 400m, totalDebtor: 1000m, totalCreditor: 400m) });

        var handler = new GetTrialBalance4QueryHandler(readRepository.Object);

        var result = await handler.Handle(Query(), CancellationToken.None);

        var row = Assert.Single(result);
        Assert.Equal("110101", row.Code);
        Assert.Equal("بانک ملت", row.Description);
        Assert.Equal(1000m, row.Debtor);
        Assert.Equal(400m, row.Creditor);
    }

    [Fact]
    public async Task Handle_DebtorHeavyRow_DebtorBalanceIsPositiveDifference_CreditorBalanceIsZero()
    {
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                It.IsAny<TrialBalanceLevel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Row(periodDebtor: 1000m, periodCreditor: 400m, totalDebtor: 1500m, totalCreditor: 600m) });

        var handler = new GetTrialBalance4QueryHandler(readRepository.Object);

        var row = Assert.Single(await handler.Handle(Query(), CancellationToken.None));

        Assert.Equal(900m, row.DebtorBalance);
        Assert.Equal(0m, row.CreditorBalance);
    }

    [Fact]
    public async Task Handle_CreditorHeavyRow_CreditorBalanceIsPositiveDifference_DebtorBalanceIsZero()
    {
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                It.IsAny<TrialBalanceLevel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Row(periodDebtor: 200m, periodCreditor: 900m, totalDebtor: 600m, totalCreditor: 1500m) });

        var handler = new GetTrialBalance4QueryHandler(readRepository.Object);

        var row = Assert.Single(await handler.Handle(Query(), CancellationToken.None));

        Assert.Equal(0m, row.DebtorBalance);
        Assert.Equal(900m, row.CreditorBalance);
    }

    [Fact]
    public async Task Handle_ExactlyEqualTotals_BothBalancesAreZero()
    {
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                It.IsAny<TrialBalanceLevel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Row(periodDebtor: 500m, periodCreditor: 500m, totalDebtor: 1200m, totalCreditor: 1200m) });

        var handler = new GetTrialBalance4QueryHandler(readRepository.Object);

        var row = Assert.Single(await handler.Handle(Query(), CancellationToken.None));

        Assert.Equal(0m, row.DebtorBalance);
        Assert.Equal(0m, row.CreditorBalance);
    }

    [Theory]
    [InlineData(1500, 600)]
    [InlineData(600, 1500)]
    [InlineData(1200, 1200)]
    public async Task Handle_DebtorBalanceAndCreditorBalance_AreNeverBothNonZero(decimal totalDebtor, decimal totalCreditor)
    {
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                It.IsAny<TrialBalanceLevel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Row(periodDebtor: 0m, periodCreditor: 0m, totalDebtor: totalDebtor, totalCreditor: totalCreditor) });

        var handler = new GetTrialBalance4QueryHandler(readRepository.Object);

        var row = Assert.Single(await handler.Handle(Query(), CancellationToken.None));

        Assert.False(row.DebtorBalance > 0m && row.CreditorBalance > 0m);
    }

    [Fact]
    public async Task Handle_NoRows_ReturnsEmptyList()
    {
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                It.IsAny<TrialBalanceLevel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TrialBalanceAggregateRow>());

        var handler = new GetTrialBalance4QueryHandler(readRepository.Object);

        var result = await handler.Handle(Query(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetTrialBalance4QueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
