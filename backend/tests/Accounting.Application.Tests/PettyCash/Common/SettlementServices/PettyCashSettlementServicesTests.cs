using System.Globalization;
using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.PettyCash;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Common.SettlementServices;

/// <summary>سرویس‌های دوره تسویه (Provisioner) و ساخت سند تسویه (VoucherBuilder).</summary>
public class PettyCashSettlementServicesTests
{
    private static readonly PersianCalendar Pc = new();
    private readonly Mock<ICurrentUser> _user = new();

    public PettyCashSettlementServicesTests() => _user.SetupGet(u => u.UserId).Returns("acc1");

    // ================= Provisioner =================

    private readonly Mock<IPettyCashSettlementPeriodRepository> _periods = new();

    private PettyCashSettlementPeriodProvisioner Prov() => new(_periods.Object, _user.Object);

    private static TB_PC_FUND Fund(PettyCashSettlementPeriod? mode, decimal ceiling = 1000m)
        => new()
        {
            ID = Guid.NewGuid(), CEILING = ceiling, SETTLEMENT_PERIOD = mode, YEAR = "1404",
            CREATEDDATE = Pc.ToDateTime(1404, 2, 25, 10, 0, 0, 0),
        };

    [Fact]
    public async Task Compute_ExistingDraft_ReturnsItsSnapshot()
    {
        var fund = Fund(PettyCashSettlementPeriod.Monthly);
        var draft = new TB_PC_SETTLEMENT_PERIOD
        {
            ID = Guid.NewGuid(), PERIOD_START = "14040301", PERIOD_END = "14040331", OPENING_BALANCE = 70m,
            COUNTED_BALANCE = 65m, STATE = PettyCashSettlementState.Draft,
        };
        _periods.Setup(p => p.GetDraftAsync(fund.ID, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var s = await Prov().ComputeCurrentAsync(fund, "0101");

        Assert.Equal(draft.ID, s.PeriodId);
        Assert.Equal("14040301", s.PeriodStart);
        Assert.Equal(70m, s.OpeningBalance);
        Assert.Equal(65m, s.CountedBalance);
    }

    [Fact]
    public async Task Compute_NoDraftNoFinal_UsesFundCreationMonthAndCeilingAsOpening()
    {
        var fund = Fund(PettyCashSettlementPeriod.Monthly, ceiling: 1234m);

        var s = await Prov().ComputeCurrentAsync(fund, "0101");

        Assert.Null(s.PeriodId);
        Assert.Equal("14040201", s.PeriodStart);
        Assert.Equal("14040231", s.PeriodEnd);
        Assert.Equal(1234m, s.OpeningBalance);
        Assert.Null(s.CountedBalance);
        Assert.Equal(PettyCashSettlementState.Draft, s.State);
    }

    [Fact]
    public async Task Compute_Quarterly_CoversWholeQuarter()
    {
        var fund = Fund(PettyCashSettlementPeriod.Quarterly);
        var s = await Prov().ComputeCurrentAsync(fund, "0101");
        Assert.Equal("14040101", s.PeriodStart);
        Assert.Equal("14040331", s.PeriodEnd);
    }

    [Fact]
    public async Task Compute_NullMode_DefaultsToMonthly()
    {
        var s = await Prov().ComputeCurrentAsync(Fund(null), "0101");
        Assert.Equal("14040201", s.PeriodStart);
        Assert.Equal("14040231", s.PeriodEnd);
    }

    [Fact]
    public async Task Compute_AfterFinal_StartsDayAfterAndOpensWithCountedBalance()
    {
        var fund = Fund(PettyCashSettlementPeriod.Monthly, ceiling: 1000m);
        _periods.Setup(p => p.GetLatestFinalAsync(fund.ID, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_PC_SETTLEMENT_PERIOD { PERIOD_END = "14040231", COUNTED_BALANCE = 420m });

        var s = await Prov().ComputeCurrentAsync(fund, "0101");

        Assert.Equal("14040301", s.PeriodStart);
        Assert.Equal("14040331", s.PeriodEnd);
        Assert.Equal(420m, s.OpeningBalance);
    }

    [Fact]
    public async Task Compute_FinalWithoutCountedBalance_FallsBackToCeiling()
    {
        var fund = Fund(PettyCashSettlementPeriod.Monthly, ceiling: 1000m);
        _periods.Setup(p => p.GetLatestFinalAsync(fund.ID, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_PC_SETTLEMENT_PERIOD { PERIOD_END = "14040231", COUNTED_BALANCE = null });

        var s = await Prov().ComputeCurrentAsync(fund, "0101");

        Assert.Equal(1000m, s.OpeningBalance);
    }

    [Fact]
    public async Task Compute_LastDayOfYearRollsToNextYear()
    {
        var fund = Fund(PettyCashSettlementPeriod.Monthly);
        _periods.Setup(p => p.GetLatestFinalAsync(fund.ID, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_PC_SETTLEMENT_PERIOD { PERIOD_END = "14041229", COUNTED_BALANCE = 1m });

        var s = await Prov().ComputeCurrentAsync(fund, "0101");

        Assert.StartsWith("1405", s.PeriodStart);
    }

    [Fact]
    public async Task EnsureDraft_Existing_ReturnedWithoutAdding()
    {
        var fund = Fund(PettyCashSettlementPeriod.Monthly);
        var draft = new TB_PC_SETTLEMENT_PERIOD { ID = Guid.NewGuid() };
        _periods.Setup(p => p.GetDraftAsync(fund.ID, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var r = await Prov().EnsureDraftAsync(fund, "0101");

        Assert.Same(draft, r);
        _periods.Verify(p => p.AddAsync(It.IsAny<TB_PC_SETTLEMENT_PERIOD>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureDraft_None_CreatesStampedDraft()
    {
        var fund = Fund(PettyCashSettlementPeriod.Monthly, ceiling: 900m);
        TB_PC_SETTLEMENT_PERIOD? added = null;
        _periods.Setup(p => p.AddAsync(It.IsAny<TB_PC_SETTLEMENT_PERIOD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_SETTLEMENT_PERIOD, CancellationToken>((x, _) => added = x).Returns(Task.CompletedTask);

        var r = await Prov().EnsureDraftAsync(fund, "0101");

        Assert.Same(added, r);
        Assert.Equal(PettyCashSettlementState.Draft, r.STATE);
        Assert.Equal(fund.ID, r.FUND_ID);
        Assert.Equal("14040201", r.PERIOD_START);
        Assert.Equal(900m, r.OPENING_BALANCE);
        Assert.Null(r.COUNTED_BALANCE);
        Assert.Equal("0101", r.VAHEDCODE);
        Assert.Equal("1404", r.YEAR);
        Assert.Equal("acc1", r.ADDUSERID);
        Assert.False(r.ISDELETED);
    }

    // ================= VoucherBuilder =================

    private readonly Mock<IVoucherHeadRepository> _heads = new();
    private readonly Mock<IVoucherDetailRepository> _details = new();
    private readonly Mock<IVoucherTafsiliLevelGuard> _guard = new();
    private readonly Mock<IPettyCashSettlementReadRepository> _read = new();

    private PettyCashSettlementVoucherBuilder Builder() => new(_heads.Object, _details.Object, _guard.Object, _read.Object, _user.Object);

    private static TB_PC_SETTLEMENT_PERIOD Period() => new() { ID = Guid.NewGuid(), PERIOD_START = "14040201", PERIOD_END = "14040231" };

    private static PettyCashSettlementTafsiliDto Taf() => new(Guid.NewGuid(), Guid.NewGuid(), "c", "t", "l");

    private static PettyCashSettlementExpenseGroupDto Group(decimal amount, Guid? acc, params PettyCashSettlementTafsiliDto[] tafs)
        => new(Guid.NewGuid(), acc, "6101", "هزینه", amount, 1, new List<Guid>(), tafs);

    private TB_PC_FUND VFund(Guid? acc) => new() { ID = Guid.NewGuid(), NAME = "تنخواه ۱", ACCOUNTCODE_ID = acc };

    private void SetupCommon(TB_PC_FUND fund, params PettyCashSettlementTafsiliDto[] fundTafsilis)
    {
        _read.Setup(r => r.GetFundTafsilisAsync(fund.ID, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(fundTafsilis);
        _heads.Setup(h => h.GetNextDocNumAsync("0101", "1404", It.IsAny<CancellationToken>())).ReturnsAsync("000042");
    }

    [Fact]
    public async Task Build_FundHasNoAccount_Throws()
    {
        var fund = VFund(null);
        await Assert.ThrowsAsync<PettyCashSettlementFundAccountMissingException>(
            () => Builder().BuildAndStageAsync(fund, Period(), new[] { Group(10m, Guid.NewGuid()) }, "0101"));
        _heads.Verify(h => h.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Build_ExpenseGroupWithoutAccount_Throws()
    {
        var fund = VFund(Guid.NewGuid());
        SetupCommon(fund);
        await Assert.ThrowsAsync<PettyCashSettlementExpenseAccountMissingException>(
            () => Builder().BuildAndStageAsync(fund, Period(), new[] { Group(10m, null) }, "0101"));
        _heads.Verify(h => h.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Build_TafsiliRuleViolationOnExpenseLine_WrappedAndNothingStaged()
    {
        var fund = VFund(Guid.NewGuid());
        SetupCommon(fund);
        var acc = Guid.NewGuid();
        _guard.Setup(g => g.EnsureSatisfiedAsync(acc, It.IsAny<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequiredTafsiliLevelMissingException(acc, new[] { "L1" }));

        await Assert.ThrowsAsync<PettyCashSettlementTafsiliMissingException>(
            () => Builder().BuildAndStageAsync(fund, Period(), new[] { Group(10m, acc) }, "0101"));
        _heads.Verify(h => h.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()), Times.Never);
        _details.Verify(d => d.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Build_TafsiliRuleViolationOnFundLine_Wrapped()
    {
        var fundAcc = Guid.NewGuid();
        var fund = VFund(fundAcc);
        SetupCommon(fund);
        _guard.Setup(g => g.EnsureSatisfiedAsync(fundAcc, It.IsAny<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TafsiliLevelNotPermittedException(fundAcc, Guid.NewGuid()));

        await Assert.ThrowsAsync<PettyCashSettlementTafsiliMissingException>(
            () => Builder().BuildAndStageAsync(fund, Period(), new[] { Group(10m, Guid.NewGuid()) }, "0101"));
    }

    [Fact]
    public async Task Build_Valid_StagesBalancedTemporaryAutomaticVoucher()
    {
        var fundAcc = Guid.NewGuid();
        var fund = VFund(fundAcc);
        var fundTaf = Taf();
        SetupCommon(fund, fundTaf);
        var accA = Guid.NewGuid(); var accB = Guid.NewGuid();
        var expTaf = Taf();
        var groups = new[] { Group(300m, accA, expTaf), Group(200m, accB) };

        TB_VOUCHERSHEAD? head = null;
        var lines = new List<TB_VOUCHERSDETAIL>();
        var links = new List<TB_VOUCHERDETAIL_LINK_TAFSILI>();
        _heads.Setup(h => h.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((x, _) => head = x).Returns(Task.CompletedTask);
        _details.Setup(d => d.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((x, _) => lines.Add(x)).Returns(Task.CompletedTask);
        _details.Setup(d => d.AddTafsiliLinkAsync(It.IsAny<TB_VOUCHERDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERDETAIL_LINK_TAFSILI, CancellationToken>((x, _) => links.Add(x)).Returns(Task.CompletedTask);

        var res = await Builder().BuildAndStageAsync(fund, Period(), groups, "0101");

        Assert.Equal(head!.ID, res.VoucherHeadId);
        Assert.Equal("000042", res.DocNum);
        Assert.Equal(500m, res.TotalAmount);

        Assert.Equal(DocLife.Temporary, head.DOCLIFE);
        Assert.True(head.ISAUTOMATIC);
        Assert.Equal("14040231", head.DATE_DOC);
        Assert.Equal("1404", head.YEAR);
        Assert.Equal("0101", head.VAHEDCODE);
        Assert.Equal("acc1", head.ADDUSERID);
        Assert.Contains("تنخواه ۱", head.HEAD_DESC);

        // ۲ سطر هزینه (بدهکار) + ۱ سطر بستانکار حساب تنخواه؛ جمع بدهکار = جمع بستانکار.
        Assert.Equal(3, lines.Count);
        Assert.Equal(new int?[] { 1, 2, 3 }, lines.Select(l => l.RADIF).ToArray());
        Assert.Equal(300m, lines[0].DEBTOR);
        Assert.Equal(accA, lines[0].ACCOUNT_ID);
        Assert.Equal(200m, lines[1].DEBTOR);
        Assert.Equal(fundAcc, lines[2].ACCOUNT_ID);
        Assert.Equal(500m, lines[2].CREDITOR);
        Assert.Equal(0m, lines[2].DEBTOR);
        Assert.Equal(lines.Sum(l => l.DEBTOR), lines.Sum(l => l.CREDITOR));
        Assert.All(lines, l => Assert.Equal(head.ID, l.VOUCHERSHEAD_ID));

        // تفصیلی: یکی برای سطر هزینه، یکی برای سطر تنخواه.
        Assert.Equal(2, links.Count);
        Assert.Contains(links, l => l.VOUCHERSDETAIL_ID == lines[0].ID && l.TAFSILI_ID == expTaf.TafsiliId);
        Assert.Contains(links, l => l.VOUCHERSDETAIL_ID == lines[2].ID && l.TAFSILI_ID == fundTaf.TafsiliId);
    }

    [Fact]
    public async Task Build_PassesTafsiliLinksToGuardPerLine()
    {
        var fundAcc = Guid.NewGuid();
        var fund = VFund(fundAcc);
        var fundTaf = Taf();
        SetupCommon(fund, fundTaf);

        await Builder().BuildAndStageAsync(fund, Period(), new[] { Group(10m, Guid.NewGuid()) }, "0101");

        _guard.Verify(g => g.EnsureSatisfiedAsync(fundAcc,
            It.Is<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(l => l.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Build_NoGroups_ProducesZeroCreditOnlyLine()
    {
        // سناریوی مرزی: بدون گروه هزینه فقط سطر بستانکار با مبلغ صفر ساخته می‌شود (در عمل Finalize پیش‌تر خطای «بدون سند» می‌دهد).
        var fund = VFund(Guid.NewGuid());
        SetupCommon(fund);
        var lines = new List<TB_VOUCHERSDETAIL>();
        _details.Setup(d => d.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((x, _) => lines.Add(x)).Returns(Task.CompletedTask);

        var res = await Builder().BuildAndStageAsync(fund, Period(), Array.Empty<PettyCashSettlementExpenseGroupDto>(), "0101");

        Assert.Equal(0m, res.TotalAmount);
        Assert.Single(lines);
    }
}
