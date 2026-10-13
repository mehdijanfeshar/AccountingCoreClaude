using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.PettyCash;
using Accounting.Application.Queries.PettyCash.GetPettyCashFundDashboard;
using Accounting.Application.Queries.PettyCash.GetPettyCashFundSettlementPreview;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Queries;

/// <summary>داشبورد تنخواه و پیش‌نمایش تسویه دوره.</summary>
public class DashboardAndSettlementPreviewTests
{
    private readonly Guid _fundId = Guid.NewGuid();

    // ================= Dashboard =================

    private readonly Mock<IPettyCashFundReadRepository> _funds = new();
    private readonly Mock<IPettyCashReplenishmentReadRepository> _repl = new();
    private readonly Mock<IPettyCashRefundReadRepository> _refunds = new();
    private readonly Mock<IPettyCashExpenseDocReadRepository> _docs = new();

    private GetPettyCashFundDashboardQueryHandler Dash() => new(_funds.Object, _repl.Object, _refunds.Object, _docs.Object);
    private GetPettyCashFundDashboardQuery DashQ() => new(_fundId) { VahedCode = "0101" };

    private static PettyCashExpenseDocListItemDto Item(PettyCashDocState state, int? age, decimal amount = 10m)
        => new(Guid.NewGuid(), "D1", "C1", "14040101", Guid.NewGuid(), "f", Guid.NewGuid(), "e", "v", "شرح", amount, state, null, age, "u", null, null);

    private void SetupDash(PettyCashFundDto fund, decimal paid, int paidCount, decimal refunds, params PettyCashExpenseDocListItemDto[] items)
    {
        _funds.Setup(f => f.GetByIdAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(fund);
        _repl.Setup(r => r.GetPaidSummaryAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(new PettyCashReplenishmentPaidSummaryDto(paid, paidCount));
        _refunds.Setup(r => r.GetTotalAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(refunds);
        _docs.Setup(d => d.GetPagedAsync(1, 500, It.IsAny<PettyCashExpenseDocFilter>(), "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashExpenseDocListResult(
                new PagedResult<PettyCashExpenseDocListItemDto> { Items = items, PageNumber = 1, PageSize = 500, TotalCount = items.Length },
                new List<PettyCashDocStateCountDto>()));
    }

    private PettyCashFundDto Fund(decimal ceiling = 1000m, decimal cash = 600m, decimal approved = 200m, int approvedCount = 2,
        decimal inFlight = 200m, int inFlightCount = 1, int? alert = null)
        => new(_fundId, "F1", "تنخواه", "1234567890", "علی", ceiling, 500m, 800m, alert, null, null, PettyCashSettlementPeriod.Monthly,
            true, false, cash, approved, approvedCount, inFlight, inFlightCount, PettyCashRefundRecorder.Treasurer);

    [Fact]
    public async Task Dashboard_FundMissing_ReturnsNull()
    {
        Assert.Null(await Dash().Handle(DashQ(), default));
        _docs.Verify(d => d.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<PettyCashExpenseDocFilter>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dashboard_RequestsOnlyInFlightStatesForThisFund()
    {
        SetupDash(Fund(), 0m, 0, 0m);
        PettyCashExpenseDocFilter? filter = null;
        _docs.Setup(d => d.GetPagedAsync(1, 500, It.IsAny<PettyCashExpenseDocFilter>(), "0101", It.IsAny<CancellationToken>()))
            .Callback<int, int, PettyCashExpenseDocFilter, string, CancellationToken>((_, _, f, _, _) => filter = f)
            .ReturnsAsync(new PettyCashExpenseDocListResult(new PagedResult<PettyCashExpenseDocListItemDto>(), new List<PettyCashDocStateCountDto>()));

        await Dash().Handle(DashQ(), default);

        Assert.Equal(_fundId, filter!.FundId);
        Assert.Equal(
            new[] { PettyCashDocState.New, PettyCashDocState.PendingReview, PettyCashDocState.Returned }.OrderBy(x => x),
            filter.States!.OrderBy(x => x));
    }

    [Fact]
    public async Task Dashboard_BalancedEquation_FlagsBalanced()
    {
        // نقد 600 + تأییدشده 200 + در جریان 200 - ترمیم‌پرداخت‌شده 0 - برگشت 0 = سقف 1000
        SetupDash(Fund(), 0m, 0, 0m);
        var d = await Dash().Handle(DashQ(), default);
        Assert.True(d!.BalanceCheck.Balanced);
        Assert.Equal(200m, d.AwaitingReplenishment.Amount);
        Assert.Equal(2, d.AwaitingReplenishment.Count);
    }

    [Fact]
    public async Task Dashboard_PaidReplenishmentAndRefunds_AdjustEquation()
    {
        // 600 + 200 + 200 - 100 - 100 = 800 != 1000 : نامتوازن
        SetupDash(Fund(), 100m, 1, 100m);
        var d = await Dash().Handle(DashQ(), default);
        Assert.False(d!.BalanceCheck.Balanced);
        Assert.Equal(100m, d.AwaitingReplenishment.Amount);
        Assert.Equal(1, d.AwaitingReplenishment.Count);
        Assert.Equal(100m, d.BalanceCheck.ReplenishedNotSettled);
    }

    [Fact]
    public async Task Dashboard_CashBelowThreshold_RaisesAlert()
    {
        // نقد 200 از 1000 = 20% < 30%
        SetupDash(Fund(cash: 200m, alert: 30), 0m, 0, 0m);
        var d = await Dash().Handle(DashQ(), default);
        Assert.True(d!.BelowAlertThreshold);
        Assert.Equal(20m, d.CashPercentOfCeiling);
        Assert.Contains(d.Alerts, a => a.Message.Contains("آستانه"));
    }

    [Fact]
    public async Task Dashboard_CashAtThreshold_NoAlert_AndNoThresholdNoAlert()
    {
        SetupDash(Fund(cash: 300m, alert: 30), 0m, 0, 0m);
        Assert.False((await Dash().Handle(DashQ(), default))!.BelowAlertThreshold);

        SetupDash(Fund(cash: 1m, alert: null), 0m, 0, 0m);
        Assert.False((await Dash().Handle(DashQ(), default))!.BelowAlertThreshold);
    }

    [Fact]
    public async Task Dashboard_ZeroCeiling_DoesNotDivideByZero()
    {
        SetupDash(Fund(ceiling: 0m, cash: 0m, alert: 10), 0m, 0, 0m);
        var d = await Dash().Handle(DashQ(), default);
        Assert.Equal(0m, d!.CashPercentOfCeiling);
    }

    [Fact]
    public async Task Dashboard_AgesAndReturnedStats()
    {
        SetupDash(Fund(), 0m, 0, 0m,
            Item(PettyCashDocState.New, 2),
            Item(PettyCashDocState.PendingReview, 6),
            Item(PettyCashDocState.Returned, 9),
            Item(PettyCashDocState.Returned, 3),
            Item(PettyCashDocState.New, 5),   // دقیقاً ۵ روز: «بیش از ۵» نیست
            Item(PettyCashDocState.New, null));

        var d = await Dash().Handle(DashQ(), default);

        Assert.Equal(2, d!.InFlight.OlderThan5DaysCount);
        Assert.Equal(2, d.Returned.Count);
        Assert.Equal(9, d.Returned.OldestAgeDays);
        Assert.Contains(d.Alerts, a => a.Message.Contains("۵ روز"));
        Assert.Equal(6, d.TodayActions.Count);
    }

    [Fact]
    public async Task Dashboard_NoReturned_OldestIsNull_AndTodayActionsCappedAt10()
    {
        SetupDash(Fund(), 0m, 0, 0m, Enumerable.Range(0, 15).Select(_ => Item(PettyCashDocState.New, 1)).ToArray());
        var d = await Dash().Handle(DashQ(), default);
        Assert.Null(d!.Returned.OldestAgeDays);
        Assert.Equal(0, d.Returned.Count);
        Assert.Equal(10, d.TodayActions.Count);
        Assert.Empty(d.Alerts);
    }

    [Fact]
    public void DashboardValidator_EmptyFundIdFails()
        => new GetPettyCashFundDashboardQueryValidator().TestValidate(new GetPettyCashFundDashboardQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.FundId);

    // ================= Settlement preview =================

    private readonly Mock<IPettyCashFundRepository> _fundRepo = new();
    private readonly Mock<IPettyCashSettlementPeriodProvisioner> _prov = new();
    private readonly Mock<IPettyCashSettlementReadRepository> _setRead = new();
    private readonly Mock<IPettyCashExpenseDocRepository> _docRepo = new();
    private readonly Mock<IVoucherTafsiliLevelGuard> _guard = new();

    private GetPettyCashFundSettlementPreviewQueryHandler Preview() => new(_fundRepo.Object, _prov.Object, _setRead.Object, _docRepo.Object, _guard.Object);
    private GetPettyCashFundSettlementPreviewQuery PrevQ() => new(_fundId) { VahedCode = "0101" };

    private static PettyCashSettlementExpenseGroupDto Group(decimal amt, Guid? acc)
        => new(Guid.NewGuid(), acc, "6101", "هزینه", amt, 1, new List<Guid>(), new List<PettyCashSettlementTafsiliDto>());

    private TB_PC_FUND SetupPreview(
        decimal opening, decimal? counted, decimal replAndRefunds, decimal approvedTotal, IReadOnlyList<PettyCashSettlementExpenseGroupDto> groups,
        Guid? fundAcc, int inFlightCount = 0)
    {
        var fund = new TB_PC_FUND
        {
            ID = _fundId, NAME = "تنخواه", ACCOUNTCODE_ID = fundAcc,
            ACCOUNTCODE = fundAcc is null ? null : new TB_ACCOUNTCODE { ACCCODE = "1101", ACCCODENAME = "تنخواه" },
        };
        _fundRepo.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(fund);
        _prov.Setup(p => p.ComputeCurrentAsync(fund, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashSettlementPeriodSnapshot(null, "14040201", "14040231", PettyCashSettlementState.Draft, opening, counted));
        var docIds = groups.Count > 0 ? new List<Guid> { Guid.NewGuid() } : new List<Guid>();
        _setRead.Setup(r => r.GetPeriodMovementAsync(_fundId, "0101", "14040201", "14040231", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashSettlementMovementDto(replAndRefunds, groups, approvedTotal, docIds));
        _setRead.Setup(r => r.GetFundTafsilisAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(new List<PettyCashSettlementTafsiliDto>());
        _docRepo.Setup(d => d.GetFundExposureAsync(_fundId, "0101", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashFundExposure(0m, 0, 25m * inFlightCount, inFlightCount));
        return fund;
    }

    private static PettyCashSettlementCheckDto Check(PettyCashSettlementPreviewDto p, string key) => p.Checks.Single(c => c.Key == key);

    [Fact]
    public async Task Preview_FundMissing_ReturnsNull()
        => Assert.Null(await Preview().Handle(PrevQ(), default));

    [Fact]
    public async Task Preview_HappyPath_AllChecksOkAndVoucherBalanced()
    {
        var acc = Guid.NewGuid();
        SetupPreview(100m, 150m, 200m, 150m, new[] { Group(100m, acc), Group(50m, acc) }, Guid.NewGuid());

        var p = (await Preview().Handle(PrevQ(), default))!;

        Assert.Equal(150m, p.ClosingCashBalance);
        Assert.All(p.Checks, c => Assert.True(c.Ok, c.Key));
        Assert.True(p.VoucherPreview.Balanced);
        Assert.Equal(150m, p.VoucherPreview.TotalDebtor);
        Assert.Equal(150m, p.VoucherPreview.TotalCredit);
        // دو سطر هزینه + یک سطر بستانکار تنخواه
        Assert.Equal(3, p.VoucherPreview.Lines.Count);
        Assert.Equal(150m, p.VoucherPreview.Lines[^1].Creditor);
        Assert.Equal("14040231", p.VoucherPreview.Date);
        Assert.Contains("تنخواه", p.VoucherPreview.Description);
    }

    [Fact]
    public async Task Preview_NoDocs_FailsHasApprovedDocsAndHasNoCreditLine()
    {
        SetupPreview(100m, 100m, 0m, 0m, new List<PettyCashSettlementExpenseGroupDto>(), Guid.NewGuid());
        var p = (await Preview().Handle(PrevQ(), default))!;
        Assert.False(Check(p, "hasApprovedDocs").Ok);
        Assert.Empty(p.VoucherPreview.Lines);
    }

    [Fact]
    public async Task Preview_NoFundAccount_FailsFundAccountCheck()
    {
        SetupPreview(100m, 100m, 0m, 0m, new List<PettyCashSettlementExpenseGroupDto>(), null);
        Assert.False(Check((await Preview().Handle(PrevQ(), default))!, "fundAccountConfigured").Ok);
    }

    [Fact]
    public async Task Preview_NoCountedBalance_FailsRecordedAndOmitsMatchCheck()
    {
        SetupPreview(100m, null, 0m, 0m, new List<PettyCashSettlementExpenseGroupDto>(), Guid.NewGuid());
        var p = (await Preview().Handle(PrevQ(), default))!;
        Assert.False(Check(p, "countedBalanceRecorded").Ok);
        Assert.DoesNotContain(p.Checks, c => c.Key == "countedBalanceMatches");
    }

    [Fact]
    public async Task Preview_CountedMismatch_FailsMatchCheck()
    {
        SetupPreview(100m, 149m, 200m, 150m, new[] { Group(150m, Guid.NewGuid()) }, Guid.NewGuid());
        var p = (await Preview().Handle(PrevQ(), default))!;
        Assert.True(Check(p, "countedBalanceRecorded").Ok);
        Assert.False(Check(p, "countedBalanceMatches").Ok);
    }

    [Fact]
    public async Task Preview_InFlightDocs_FailsAcknowledgeCheck()
    {
        SetupPreview(100m, 100m, 0m, 0m, new List<PettyCashSettlementExpenseGroupDto>(), Guid.NewGuid(), inFlightCount: 2);
        var p = (await Preview().Handle(PrevQ(), default))!;
        Assert.False(Check(p, "inFlightAcknowledged").Ok);
        Assert.Equal(2, p.InFlightCount);
        Assert.Equal(50m, p.InFlightAmount);
    }

    [Fact]
    public async Task Preview_ExpenseGroupWithoutAccount_FailsTafsiliCheck()
    {
        SetupPreview(100m, 100m, 0m, 10m, new[] { Group(10m, null) }, Guid.NewGuid());
        var p = (await Preview().Handle(PrevQ(), default))!;
        Assert.False(Check(p, "voucherLinesTafsiliValid").Ok);
    }

    [Fact]
    public async Task Preview_TafsiliRuleViolation_ReportsPublicDetail()
    {
        var acc = Guid.NewGuid();
        SetupPreview(100m, 100m, 0m, 10m, new[] { Group(10m, acc) }, Guid.NewGuid());
        var ex = new RequiredTafsiliLevelMissingException(acc, new[] { "L1" });
        _guard.Setup(g => g.EnsureSatisfiedAsync(acc, It.IsAny<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        var c = Check((await Preview().Handle(PrevQ(), default))!, "voucherLinesTafsiliValid");

        Assert.False(c.Ok);
        Assert.Equal(ex.PublicDetail, c.Message);
    }

    [Fact]
    public async Task Preview_FundAccountTafsiliViolation_AlsoReported()
    {
        var fundAcc = Guid.NewGuid();
        SetupPreview(100m, 100m, 0m, 10m, new[] { Group(10m, Guid.NewGuid()) }, fundAcc);
        _guard.Setup(g => g.EnsureSatisfiedAsync(fundAcc, It.IsAny<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TafsiliLevelNotPermittedException(fundAcc, Guid.NewGuid()));
        Assert.False(Check((await Preview().Handle(PrevQ(), default))!, "voucherLinesTafsiliValid").Ok);
    }
}
