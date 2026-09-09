using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.TrialBalance;
using Accounting.Application.Reports.TrialBalance.GetTrialBalance8;
using Moq;

namespace Accounting.Application.Tests.Reports.TrialBalance.GetTrialBalance8;

public sealed class GetTrialBalance8QueryHandlerTests
{
    private static TrialBalanceAggregateRow Row(
        decimal periodDebtor,
        decimal periodCreditor,
        decimal totalDebtor,
        decimal totalCreditor,
        decimal openingDebtor,
        decimal openingCreditor) => new()
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

    private static GetTrialBalance8Query Query() => new(
        Year: "1405",
        FromDate: "14050101",
        ToDate: "14051230",
        Level: TrialBalanceLevel.Moin,
        DocLife: null)
    {
        VahedCode = "0001",
    };

    [Fact]
    public async Task Handle_MapsTotDebtorAndTotCreditor_FromAggregateTotals()
    {
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                It.IsAny<TrialBalanceLevel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Row(periodDebtor: 100m, periodCreditor: 50m, totalDebtor: 800m, totalCreditor: 350m, openingDebtor: 700m, openingCreditor: 300m) });

        var handler = new GetTrialBalance8QueryHandler(readRepository.Object);

        var row = Assert.Single(await handler.Handle(Query(), CancellationToken.None));

        Assert.Equal(800m, row.TotDebtor);
        Assert.Equal(350m, row.TotCreditor);
    }

    /// <summary>
    /// Regression test for the project-owner decision (see <c>docs/open-decisions.md</c>):
    /// FirstDebtor/FirstCreditor are netted (one-sided opening balance), matching
    /// <c>GetTrialBalance6QueryHandlerTests</c>. Deliberately both opening sides non-zero on input
    /// to prove the netting actually happens.
    ///
    /// ⚠️ Superseded a prior test asserting <c>TotDebtor == FirstDebtor + Debtor</c> — that raw
    /// arithmetic identity no longer holds now that FirstDebtor/FirstCreditor are netted while
    /// TotDebtor/TotCreditor deliberately stay raw (see the handler's and DTO's XML docs). Do not
    /// reintroduce that assertion.
    /// </summary>
    [Fact]
    public async Task Handle_MapsFirstDebtorAndFirstCreditor_AsNettedOpeningBalance()
    {
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                It.IsAny<TrialBalanceLevel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Row(periodDebtor: 100m, periodCreditor: 50m, totalDebtor: 800m, totalCreditor: 350m, openingDebtor: 700m, openingCreditor: 300m) });

        var handler = new GetTrialBalance8QueryHandler(readRepository.Object);

        var row = Assert.Single(await handler.Handle(Query(), CancellationToken.None));

        Assert.Equal(400m, row.FirstDebtor);
        Assert.Equal(0m, row.FirstCreditor);
    }

    [Fact]
    public async Task Handle_DebtorHeavyRow_DebtorBalanceIsPositiveDifference_CreditorBalanceIsZero()
    {
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                It.IsAny<TrialBalanceLevel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Row(periodDebtor: 500m, periodCreditor: 100m, totalDebtor: 1500m, totalCreditor: 600m, openingDebtor: 1000m, openingCreditor: 500m) });

        var handler = new GetTrialBalance8QueryHandler(readRepository.Object);

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
            .ReturnsAsync(new[] { Row(periodDebtor: 100m, periodCreditor: 500m, totalDebtor: 600m, totalCreditor: 1500m, openingDebtor: 500m, openingCreditor: 1000m) });

        var handler = new GetTrialBalance8QueryHandler(readRepository.Object);

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
            .ReturnsAsync(new[] { Row(periodDebtor: 200m, periodCreditor: 200m, totalDebtor: 1000m, totalCreditor: 1000m, openingDebtor: 800m, openingCreditor: 800m) });

        var handler = new GetTrialBalance8QueryHandler(readRepository.Object);

        var row = Assert.Single(await handler.Handle(Query(), CancellationToken.None));

        Assert.Equal(0m, row.DebtorBalance);
        Assert.Equal(0m, row.CreditorBalance);
    }

    [Fact]
    public async Task Handle_PassesRequestVahedCodeToRepository_AtFaceValue()
    {
        // Proves the handler trusts request.VahedCode as-is: by the time this handler runs,
        // VahedScopeBehavior has already overwritten it with the authenticated caller's own unit
        // code, so the handler must forward exactly that value, not derive its own. This is the
        // IDOR-closure proof for what used to be the largest single data-leak surface in the
        // project (CLAUDE.md risk #1) — an entire unit's trial balance reachable via one
        // query-string parameter.
        var readRepository = new Mock<ITrialBalanceReadRepository>();
        readRepository
            .Setup(r => r.GetAggregatesAsync(
                It.IsAny<TrialBalanceLevel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                "0007", It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TrialBalanceAggregateRow>());

        var handler = new GetTrialBalance8QueryHandler(readRepository.Object);
        var query = Query() with { VahedCode = "0007" };

        await handler.Handle(query, CancellationToken.None);

        readRepository.Verify(
            r => r.GetAggregatesAsync(
                query.Level, query.Year, query.FromDate, query.ToDate, "0007", query.DocLife,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetTrialBalance8QueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
