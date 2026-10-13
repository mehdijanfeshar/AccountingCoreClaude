using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.PettyCash.DeletePettyCashRefund;
using Accounting.Application.Commands.PettyCash.DeletePettyCashReplenishment;
using Accounting.Application.Commands.PettyCash.RecordPettyCashReplenishmentPayment;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.Replenishment;

/// <summary>ثبت پرداخت ترمیم (خزانه‌دار)، حذف ترمیم پیش‌نویس و حذف برگشت وجه.</summary>
public class PaymentAndDeleteTests
{
    private readonly Mock<IPettyCashReplenishmentRepository> _repl = new();
    private readonly Mock<IChargeAndCostRepository> _charge = new();
    private readonly Mock<IPettyCashReplenishmentAuthorizer> _auth = new();
    private readonly Mock<IPettyCashRefundRepository> _refunds = new();
    private readonly Mock<IPettyCashFundRepository> _funds = new();
    private readonly Mock<IPettyCashRefundRecorderAuthorizer> _recorder = new();
    private readonly Mock<IPettyCashSettlementPeriodRepository> _periods = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Guid _id = Guid.NewGuid();

    public PaymentAndDeleteTests() => _user.SetupGet(u => u.UserId).Returns("treasurer1");

    private void SaveNever() => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    private void SaveOnce() => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    private TB_PC_REPLENISHMENT SetupRepl(PettyCashReplenishmentState state, bool deleted = false, string approvedBy = "fm1")
    {
        var r = new TB_PC_REPLENISHMENT
        {
            ID = _id, FUND_ID = Guid.NewGuid(), CHARGEANDCOSTHEAD_ID = Guid.NewGuid(),
            STATE = state, ISDELETED = deleted, APPROVED_BY_USERID = approvedBy,
        };
        _repl.Setup(x => x.GetForUpdateAsync(_id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(r);
        return r;
    }

    private TB_CHARGEANDCOST_HEAD SetupHead(TB_PC_REPLENISHMENT r)
    {
        var head = new TB_CHARGEANDCOST_HEAD { STATUS = ChargeAndCostStatus.Temporary };
        _charge.Setup(c => c.GetHeadForUpdateAsync(r.CHARGEANDCOSTHEAD_ID, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(head);
        return head;
    }

    // ---------- RecordPayment ----------

    private RecordPettyCashReplenishmentPaymentCommandHandler Pay() => new(_repl.Object, _charge.Object, _auth.Object, _uow.Object, _user.Object);
    private RecordPettyCashReplenishmentPaymentCommand PayCmd(DateTime? d = null) => new(_id, d) { VahedCode = "0101" };

    [Fact]
    public async Task Pay_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() => Pay().Handle(PayCmd(), default));

    [Fact]
    public async Task Pay_Deleted_ThrowsNotFound()
    {
        SetupRepl(PettyCashReplenishmentState.PendingTreasurer, deleted: true);
        await Assert.ThrowsAsync<NotFoundException>(() => Pay().Handle(PayCmd(), default));
    }

    [Theory]
    [InlineData(PettyCashReplenishmentState.Draft)]
    [InlineData(PettyCashReplenishmentState.PendingFinanceManager)]
    [InlineData(PettyCashReplenishmentState.Paid)]
    [InlineData(PettyCashReplenishmentState.Rejected)]
    public async Task Pay_NotPendingTreasurer_ThrowsStateConflict(PettyCashReplenishmentState state)
    {
        SetupRepl(state);
        await Assert.ThrowsAsync<PettyCashReplenishmentStateConflictException>(() => Pay().Handle(PayCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Pay_RequiresTreasurerRole()
    {
        var r = SetupRepl(PettyCashReplenishmentState.PendingTreasurer);
        _auth.Setup(a => a.EnsureHasRoleAsync(r.FUND_ID, It.IsAny<IReadOnlyCollection<PettyCashRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashReplenishmentRoleRequiredException(r.FUND_ID, new[] { PettyCashRole.Treasurer }));

        await Assert.ThrowsAsync<PettyCashReplenishmentRoleRequiredException>(() => Pay().Handle(PayCmd(), default));
        _auth.Verify(a => a.EnsureHasRoleAsync(r.FUND_ID,
            It.Is<IReadOnlyCollection<PettyCashRole>>(x => x.Count == 1 && x.Contains(PettyCashRole.Treasurer)), It.IsAny<CancellationToken>()));
        SaveNever();
    }

    [Fact]
    public async Task Pay_SameUserApprovedAndPays_ThrowsPayerConflict()
    {
        // تفکیک وظایف: تأییدکنندهٔ مدیر مالی نباید پرداخت‌کننده هم باشد.
        SetupRepl(PettyCashReplenishmentState.PendingTreasurer, approvedBy: "treasurer1");
        await Assert.ThrowsAsync<PettyCashReplenishmentPayerConflictException>(() => Pay().Handle(PayCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Pay_Valid_MarksPaidAndAcceptsHead()
    {
        var r = SetupRepl(PettyCashReplenishmentState.PendingTreasurer);
        var head = SetupHead(r);
        var date = new DateTime(2026, 1, 5);

        await Pay().Handle(PayCmd(date), default);

        Assert.Equal(PettyCashReplenishmentState.Paid, r.STATE);
        Assert.Equal(date, r.PAID_DATE);
        Assert.Equal("treasurer1", r.PAID_BY_USERID);
        Assert.Equal(ChargeAndCostStatus.Accepted, head.STATUS);
        Assert.Equal("treasurer1", head.CHANGEUSERID);
        SaveOnce();
    }

    [Fact]
    public async Task Pay_NoDate_DefaultsToNow_AndMissingHeadIsTolerated()
    {
        var r = SetupRepl(PettyCashReplenishmentState.PendingTreasurer);

        await Pay().Handle(PayCmd(), default);

        Assert.NotNull(r.PAID_DATE);
        Assert.Equal(PettyCashReplenishmentState.Paid, r.STATE);
        SaveOnce();
    }

    [Fact]
    public void PayValidator_Rules()
    {
        var v = new RecordPettyCashReplenishmentPaymentCommandValidator();
        v.TestValidate(PayCmd()).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(new RecordPettyCashReplenishmentPaymentCommand(Guid.Empty, null) { VahedCode = "0101" }).ShouldHaveValidationErrorFor(x => x.Id);
        v.TestValidate(new RecordPettyCashReplenishmentPaymentCommand(_id, null) { VahedCode = "" }).ShouldHaveValidationErrorFor(x => x.VahedCode);
    }

    // ---------- DeletePettyCashReplenishment ----------

    private DeletePettyCashReplenishmentCommandHandler DelRepl() => new(_repl.Object, _charge.Object, _auth.Object, _uow.Object, _user.Object);
    private DeletePettyCashReplenishmentCommand DelReplCmd() => new(_id) { VahedCode = "0101" };

    [Fact]
    public async Task DelRepl_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() => DelRepl().Handle(DelReplCmd(), default));

    [Theory]
    [InlineData(PettyCashReplenishmentState.PendingFinanceManager)]
    [InlineData(PettyCashReplenishmentState.PendingTreasurer)]
    [InlineData(PettyCashReplenishmentState.Paid)]
    [InlineData(PettyCashReplenishmentState.Rejected)]
    public async Task DelRepl_NotDraft_ThrowsStateConflict(PettyCashReplenishmentState state)
    {
        SetupRepl(state);
        await Assert.ThrowsAsync<PettyCashReplenishmentStateConflictException>(() => DelRepl().Handle(DelReplCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task DelRepl_AuthorizerAllowsFinanceManagerOrTreasurer()
    {
        var r = SetupRepl(PettyCashReplenishmentState.Draft);
        _charge.Setup(c => c.GetActiveLinksByChargeIdAsync(r.CHARGEANDCOSTHEAD_ID, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_CHARGE_LINK_COST>());

        await DelRepl().Handle(DelReplCmd(), default);

        _auth.Verify(a => a.EnsureHasRoleAsync(r.FUND_ID,
            It.Is<IReadOnlyCollection<PettyCashRole>>(x => x.Contains(PettyCashRole.FinanceManager) && x.Contains(PettyCashRole.Treasurer)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DelRepl_NoRole_PropagatesAndDoesNotSave()
    {
        SetupRepl(PettyCashReplenishmentState.Draft);
        _auth.Setup(a => a.EnsureHasRoleAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<PettyCashRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashReplenishmentRoleRequiredException(Guid.NewGuid(), new[] { PettyCashRole.FinanceManager }));
        await Assert.ThrowsAsync<PettyCashReplenishmentRoleRequiredException>(() => DelRepl().Handle(DelReplCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task DelRepl_AlreadyDeleted_IsNoOp()
    {
        SetupRepl(PettyCashReplenishmentState.Draft, deleted: true);
        await DelRepl().Handle(DelReplCmd(), default);
        SaveNever();
    }

    [Fact]
    public async Task DelRepl_Valid_SoftDeletesReplenishmentLinksAndHead()
    {
        var r = SetupRepl(PettyCashReplenishmentState.Draft);
        var head = SetupHead(r);
        var l1 = new TB_CHARGE_LINK_COST();
        var l2 = new TB_CHARGE_LINK_COST();
        _charge.Setup(c => c.GetActiveLinksByChargeIdAsync(r.CHARGEANDCOSTHEAD_ID, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_CHARGE_LINK_COST> { l1, l2 });

        await DelRepl().Handle(DelReplCmd(), default);

        Assert.True(r.ISDELETED);
        Assert.True(l1.ISDELETED);
        Assert.True(l2.ISDELETED);
        Assert.True(head.ISDELETED);
        Assert.Equal("treasurer1", head.CHANGEUSERID);
        SaveOnce();
    }

    // ---------- DeletePettyCashRefund ----------

    private DeletePettyCashRefundCommandHandler DelRefund() => new(_refunds.Object, _funds.Object, _recorder.Object, _periods.Object, _uow.Object, _user.Object);
    private DeletePettyCashRefundCommand DelRefundCmd() => new(_id) { VahedCode = "0101" };

    private TB_PC_REFUND SetupRefund(bool deleted = false)
    {
        var fundId = Guid.NewGuid();
        var refund = new TB_PC_REFUND { ID = _id, FUND_ID = fundId, REFUND_DATE = "14040510", ISDELETED = deleted };
        _refunds.Setup(x => x.GetForUpdateAsync(_id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _funds.Setup(x => x.GetForUpdateAsync(fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(new TB_PC_FUND { ID = fundId });
        return refund;
    }

    [Fact]
    public async Task DelRefund_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() => DelRefund().Handle(DelRefundCmd(), default));

    [Fact]
    public async Task DelRefund_FundMissing_ThrowsNotFound()
    {
        var refund = new TB_PC_REFUND { ID = _id, FUND_ID = Guid.NewGuid() };
        _refunds.Setup(x => x.GetForUpdateAsync(_id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        await Assert.ThrowsAsync<NotFoundException>(() => DelRefund().Handle(DelRefundCmd(), default));
    }

    [Fact]
    public async Task DelRefund_RecorderMismatch_Propagates()
    {
        var refund = SetupRefund();
        _recorder.Setup(r => r.EnsureCanRecordAsync(It.IsAny<TB_PC_FUND>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashRefundRecorderMismatchException(refund.FUND_ID, PettyCashRefundRecorder.Treasurer));
        await Assert.ThrowsAsync<PettyCashRefundRecorderMismatchException>(() => DelRefund().Handle(DelRefundCmd(), default));
        Assert.False(refund.ISDELETED);
        SaveNever();
    }

    [Fact]
    public async Task DelRefund_AlreadyDeleted_IsNoOp()
    {
        SetupRefund(deleted: true);
        await DelRefund().Handle(DelRefundCmd(), default);
        SaveNever();
    }

    [Fact]
    public async Task DelRefund_InsideFinalizedPeriod_ThrowsLocked()
    {
        var refund = SetupRefund();
        _periods.Setup(p => p.ExistsFinalCoveringDateAsync(refund.FUND_ID, "14040510", "0101", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<PettyCashRefundLockedBySettledPeriodException>(() => DelRefund().Handle(DelRefundCmd(), default));
        Assert.False(refund.ISDELETED);
        SaveNever();
    }

    [Fact]
    public async Task DelRefund_Valid_SoftDeletes()
    {
        var refund = SetupRefund();
        await DelRefund().Handle(DelRefundCmd(), default);
        Assert.True(refund.ISDELETED);
        Assert.Equal("treasurer1", refund.CHANGEUSERID);
        SaveOnce();
    }
}
