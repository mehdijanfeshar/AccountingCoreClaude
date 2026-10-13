using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.PettyCash.CountPettyCashSettlement;
using Accounting.Application.Commands.PettyCash.CreatePettyCashRefund;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.Settlement;

public class RefundAndCountTests
{
    private readonly Mock<IPettyCashFundRepository> _funds = new();
    private readonly Mock<IPettyCashRefundRepository> _refunds = new();
    private readonly Mock<IPettyCashRefundRecorderAuthorizer> _recorder = new();
    private readonly Mock<IPettyCashSettlementPeriodProvisioner> _prov = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Guid _fundId = Guid.NewGuid();

    public RefundAndCountTests() => _user.SetupGet(u => u.UserId).Returns("u1");

    private void SetupFund(TB_PC_FUND? fund)
        => _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(fund);

    // ---------- CreatePettyCashRefund ----------

    private CreatePettyCashRefundCommandHandler Refund() => new(_funds.Object, _refunds.Object, _recorder.Object, _uow.Object, _user.Object);

    private CreatePettyCashRefundCommand RefundCmd(decimal amount = 500m) => new(_fundId, amount, "14040510", "برگشت") { VahedCode = "0101" };

    [Fact]
    public async Task Refund_FundMissing_ThrowsNotFound()
    {
        SetupFund(null);
        await Assert.ThrowsAsync<NotFoundException>(() => Refund().Handle(RefundCmd(), default));
    }

    [Fact]
    public async Task Refund_RecorderMismatch_PropagatesAndDoesNotSave()
    {
        SetupFund(new TB_PC_FUND { ID = _fundId });
        _recorder.Setup(r => r.EnsureCanRecordAsync(It.IsAny<TB_PC_FUND>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashRefundRecorderMismatchException(_fundId, PettyCashRefundRecorder.Treasurer));

        await Assert.ThrowsAsync<PettyCashRefundRecorderMismatchException>(() => Refund().Handle(RefundCmd(), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refund_Valid_StampsCodeRecorderAndVahed()
    {
        SetupFund(new TB_PC_FUND { ID = _fundId });
        _refunds.Setup(r => r.GetNextCodeAsync("0101", It.IsAny<CancellationToken>())).ReturnsAsync(12);
        TB_PC_REFUND? saved = null;
        _refunds.Setup(r => r.AddAsync(It.IsAny<TB_PC_REFUND>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_REFUND, CancellationToken>((x, _) => saved = x).Returns(Task.CompletedTask);

        var id = await Refund().Handle(RefundCmd(), default);

        Assert.Equal(id, saved!.ID);
        Assert.Equal("REF-00012", saved.CODE);
        Assert.Equal(500m, saved.AMOUNT);
        Assert.Equal("u1", saved.RECORDED_BY_USERID);
        Assert.Equal("0101", saved.VAHEDCODE);
        Assert.False(saved.ISDELETED);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private readonly CreatePettyCashRefundCommandValidator _refVal = new();

    [Fact]
    public void RefundValidator_Valid_Passes() => _refVal.TestValidate(RefundCmd()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RefundValidator_NonPositiveAmount_Fails(int amount)
        => _refVal.TestValidate(RefundCmd(amount)).ShouldHaveValidationErrorFor(x => x.Amount);

    [Fact]
    public void RefundValidator_BadDate_Fails()
        => _refVal.TestValidate(RefundCmd() with { RefundDate = "1404-05-10" }).ShouldHaveValidationErrorFor(x => x.RefundDate);

    [Fact]
    public void RefundValidator_ReasonTooLong_Fails()
        => _refVal.TestValidate(RefundCmd() with { Reason = new string('x', 501) }).ShouldHaveValidationErrorFor(x => x.Reason);

    // ---------- CountPettyCashSettlement ----------

    private CountPettyCashSettlementCommandHandler Count() => new(_funds.Object, _prov.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task Count_FundMissing_ThrowsNotFound()
    {
        SetupFund(null);
        await Assert.ThrowsAsync<NotFoundException>(
            () => Count().Handle(new CountPettyCashSettlementCommand(_fundId, 10m) { VahedCode = "0101" }, default));
    }

    [Fact]
    public async Task Count_Valid_SetsCountedBalanceOnDraftPeriod()
    {
        var fund = new TB_PC_FUND { ID = _fundId };
        SetupFund(fund);
        var period = new TB_PC_SETTLEMENT_PERIOD { ID = Guid.NewGuid() };
        _prov.Setup(p => p.EnsureDraftAsync(fund, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(period);

        var id = await Count().Handle(new CountPettyCashSettlementCommand(_fundId, 750m) { VahedCode = "0101" }, default);

        Assert.Equal(period.ID, id);
        Assert.Equal(750m, period.COUNTED_BALANCE);
        Assert.Equal("u1", period.CHANGEUSERID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private readonly CountPettyCashSettlementCommandValidator _cntVal = new();

    [Fact]
    public void CountValidator_ZeroBalance_Passes()
        => _cntVal.TestValidate(new CountPettyCashSettlementCommand(_fundId, 0m)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void CountValidator_NegativeBalance_Fails()
        => _cntVal.TestValidate(new CountPettyCashSettlementCommand(_fundId, -1m)).ShouldHaveValidationErrorFor(x => x.CountedBalance);

    [Fact]
    public void CountValidator_EmptyFund_Fails()
        => _cntVal.TestValidate(new CountPettyCashSettlementCommand(Guid.Empty, 1m)).ShouldHaveValidationErrorFor(x => x.FundId);
}
