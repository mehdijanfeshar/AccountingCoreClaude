using Accounting.Application.Commands.Treasury.BulkApprovePaymentRequests;
using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Commands.Treasury.ExecutePaymentRequest;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.PaymentRequests;

/// <summary>تأیید گروهی (همه یا هیچ) و اجرای پرداخت (SoD، تراکنش، سند پرداخت و آینهٔ PayRecive).</summary>
public sealed class BulkApproveAndExecuteTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();

    // ── Bulk approve ────────────────────────────────────────────────────────

    private readonly Mock<IPaymentRequestApprovalService> _approval = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private BulkApprovePaymentRequestsCommandHandler BulkSut() => new(_approval.Object, _uow.Object);

    private static BulkApprovePaymentRequestsCommand BulkCmd(params Guid[] ids)
        => new(ids) { VahedCode = Vahed };

    [Fact]
    public async Task Bulk_AllSucceed_ApprovesEachAsBulkAndSavesOnce()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        await BulkSut().Handle(BulkCmd(a, b), default);

        _approval.Verify(s => s.ApproveAsync(a, Vahed, null, true, It.IsAny<CancellationToken>()), Times.Once);
        _approval.Verify(s => s.ApproveAsync(b, Vahed, null, true, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Bulk_DuplicateIds_ProcessedOnce()
    {
        var a = Guid.NewGuid();

        await BulkSut().Handle(BulkCmd(a, a, a), default);

        _approval.Verify(s => s.ApproveAsync(a, Vahed, null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    public static IEnumerable<object[]> FailureMap() => new[]
    {
        new object[] { new NotFoundException("PaymentRequest", Id), "not-found" },
        new object[] { new TreasuryRoleRequiredException(Vahed, TreasuryRole.FinanceManager), "forbidden" },
        new object[] { new PaymentRequestRoleRequiredException(Id, TreasuryRole.Ceo), "forbidden" },
        new object[] { new PaymentRequestApproverConflictException(Id), "self-approve" },
        new object[] { new PaymentRequestConsecutiveApproverConflictException(Id), "consecutive-approver" },
        new object[] { new PaymentRequestStateConflictException(Id, PaymentRequestState.Draft, "x"), "invalid-state" },
        new object[] { new PaymentRequestSettingsMissingException(Vahed), "settings-missing" },
        new object[] { new PaymentRequestBulkLimitExceededException(Id, 10m, 5m), "over-bulk-limit" },
        new object[] { new PaymentRequestTreasurySettingAccountMissingException("x"), "liability-voucher-account-missing" },
        new object[] { new PaymentRequestPayablesBeneficiaryRequiredException(Id), "liability-voucher-beneficiary-required" },
        new object[] { new PaymentRequestVoucherAccountConfigException("x", Id, "d"), "liability-voucher-account-config" },
        new object[] { new PaymentRequestVoucherTafsiliMissingException(Id, "d"), "liability-voucher-tafsili-missing" },
        new object[] { new PaymentRequestVoucherUnbalancedException("x", 1m, 2m), "liability-voucher-unbalanced" },
    };

    [Theory]
    [MemberData(nameof(FailureMap))]
    public async Task Bulk_OneFails_ThrowsConflictWithReasonCode_AndNothingSaved(Exception failure, string code)
    {
        var good = Guid.NewGuid();
        var bad = Guid.NewGuid();
        _approval.Setup(s => s.ApproveAsync(bad, Vahed, null, true, It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        var ex = await Assert.ThrowsAsync<PaymentRequestBulkApproveConflictException>(() => BulkSut().Handle(BulkCmd(good, bad), default));

        Assert.Equal(code, ex.Failures[bad]);
        Assert.False(ex.Failures.ContainsKey(good));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Bulk_AllFail_ReportsEveryFailure()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        _approval.Setup(s => s.ApproveAsync(a, Vahed, null, true, It.IsAny<CancellationToken>())).ThrowsAsync(new NotFoundException("PaymentRequest", a));
        _approval.Setup(s => s.ApproveAsync(b, Vahed, null, true, It.IsAny<CancellationToken>())).ThrowsAsync(new PaymentRequestApproverConflictException(b));

        var ex = await Assert.ThrowsAsync<PaymentRequestBulkApproveConflictException>(() => BulkSut().Handle(BulkCmd(a, b), default));

        Assert.Equal(2, ex.Failures.Count);
    }

    [Fact]
    public async Task Bulk_UnexpectedException_IsNotSwallowed()
    {
        var a = Guid.NewGuid();
        _approval.Setup(s => s.ApproveAsync(a, Vahed, null, true, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => BulkSut().Handle(BulkCmd(a), default));
    }

    [Fact]
    public void BulkValidator_EmptyList_Fails_NonEmpty_Passes()
    {
        var v = new BulkApprovePaymentRequestsCommandValidator();
        Assert.False(v.Validate(BulkCmd()).IsValid);
        Assert.True(v.Validate(BulkCmd(Guid.NewGuid())).IsValid);
    }

    // ── Execute ─────────────────────────────────────────────────────────────

    private readonly Mock<IPaymentRequestRepository> _requests = new();
    private readonly Mock<IPaymentRequestEventRepository> _events = new();
    private readonly Mock<IPaymentRequestExecutionAuthorizer> _execAuth = new();
    private readonly Mock<IPaymentRequestPaymentVoucherBuilder> _voucher = new();
    private readonly Mock<IPayReciveHeadRepository> _payRecive = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();

    private ExecutePaymentRequestCommandHandler ExecSut() => new(
        _requests.Object, _events.Object, _execAuth.Object, _voucher.Object, _payRecive.Object, _uow.Object, _user.Object, _client.Object);

    private static ExecutePaymentRequestCommand ExecCmd(TreasuryPaymentMethod? method = null, string? iban = null)
        => new(Id, "BR1", "14050505", iban, method) { VahedCode = Vahed };

    private TB_TR_PAYMENT_REQUEST Existing(PaymentRequestState state, string creator = "creator")
    {
        _user.SetupGet(u => u.UserId).Returns("treasurer");
        var e = new TB_TR_PAYMENT_REQUEST
        {
            ID = Id, REQUEST_STATE = state, ADDUSERID = creator, YEAR = "1405", CODE = "PAY-1", BENEFICIARY_NAME = "x",
            PAYMENT_METHOD = TreasuryPaymentMethod.Satna,
        };
        _requests.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        return e;
    }

    private void SetupVoucher(Guid voucherId)
    {
        _voucher.Setup(v => v.BuildAndStageAsync(It.IsAny<TB_TR_PAYMENT_REQUEST>(), "14050505", Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRequestVoucherBuildResult(voucherId, "31", "14050505", "1405", new[]
            {
                new PaymentRequestVoucherLineResult(Guid.NewGuid(), 900m, 0m, new[] { new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()) }),
                new PaymentRequestVoucherLineResult(Guid.NewGuid(), 0m, 900m, Array.Empty<VoucherDetailTafsiliLinkInput>()),
            }));
        _payRecive.Setup(p => p.GetNextCodeAsync(Vahed, "1405", It.IsAny<CancellationToken>())).ReturnsAsync(8);
    }

    [Fact]
    public async Task Execute_Valid_MarksExecutedAndMirrorsVoucherInTransaction()
    {
        var e = Existing(PaymentRequestState.ReadyForExecution);
        var voucherId = Guid.NewGuid();
        SetupVoucher(voucherId);
        TB_PAYRECIVHEAD? head = null;
        var details = new List<TB_PAYRECIVDETAIL>();
        TB_TR_PAYMENT_REQUEST_EVENT? ev = null;
        _payRecive.Setup(p => p.AddAsync(It.IsAny<TB_PAYRECIVHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PAYRECIVHEAD, CancellationToken>((h, _) => head = h).Returns(Task.CompletedTask);
        _payRecive.Setup(p => p.AddDetailAsync(It.IsAny<TB_PAYRECIVDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PAYRECIVDETAIL, CancellationToken>((d, _) => details.Add(d)).Returns(Task.CompletedTask);
        _events.Setup(x => x.AddAsync(It.IsAny<TB_TR_PAYMENT_REQUEST_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_PAYMENT_REQUEST_EVENT, CancellationToken>((x, _) => ev = x).Returns(Task.CompletedTask);

        await ExecSut().Handle(ExecCmd(TreasuryPaymentMethod.Paya, "IR" + new string('1', 24)), default);

        Assert.Equal(PaymentRequestState.Executed, e.REQUEST_STATE);
        Assert.Equal(voucherId, e.PAYMENT_VOUCHER_ID);
        Assert.Equal(head!.ID, e.PAYRECIVHEAD_ID);
        Assert.Equal("BR1", e.BANK_REFERENCE);
        Assert.Equal("14050505", e.PAID_DATE);
        Assert.Equal(TreasuryPaymentMethod.Paya, e.PAYMENT_METHOD);
        Assert.Equal("treasurer", e.EXECUTED_BY);
        Assert.Equal(PayRecivType.Pay, head.PAYRECIVTYPE);
        Assert.Equal("00008", head.PAYRECIVCODE);
        Assert.Equal(2, details.Count);
        Assert.Equal("پرداخت — بستانکاران", details[0].ARTICLEDESCRIPTION);
        Assert.Equal("پرداخت — بانک", details[1].ARTICLEDESCRIPTION);
        Assert.Equal(PaymentRequestEventAction.Execute, ev!.ACTION);
        Assert.Equal(PaymentRequestState.Executed, ev.TO_STATE);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_NoPaymentMethodInCommand_KeepsExistingMethod()
    {
        var e = Existing(PaymentRequestState.ReadyForExecution);
        SetupVoucher(Guid.NewGuid());

        await ExecSut().Handle(ExecCmd(method: null), default);

        Assert.Equal(TreasuryPaymentMethod.Satna, e.PAYMENT_METHOD);
    }

    [Fact]
    public async Task Execute_CreatorExecutes_ThrowsExecutorConflict_BeforeTransaction()
    {
        Existing(PaymentRequestState.ReadyForExecution, creator: "treasurer");

        await Assert.ThrowsAsync<PaymentRequestExecutorConflictException>(() => ExecSut().Handle(ExecCmd(), default));

        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(PaymentRequestState.Suspended)]
    [InlineData(PaymentRequestState.PendingCeo)]
    [InlineData(PaymentRequestState.Executed)]
    public async Task Execute_NotReady_ThrowsStateConflict(PaymentRequestState state)
    {
        Existing(state);

        await Assert.ThrowsAsync<PaymentRequestStateConflictException>(() => ExecSut().Handle(ExecCmd(), default));
    }

    [Fact]
    public async Task Execute_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() => ExecSut().Handle(ExecCmd(), default));

    [Fact]
    public async Task Execute_NotTreasurer_Throws()
    {
        Existing(PaymentRequestState.ReadyForExecution);
        _execAuth.Setup(a => a.EnsureTreasurerAsync(Id, Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentRequestTreasurerRoleRequiredException(Id));

        await Assert.ThrowsAsync<PaymentRequestTreasurerRoleRequiredException>(() => ExecSut().Handle(ExecCmd(), default));

        _voucher.Verify(v => v.BuildAndStageAsync(It.IsAny<TB_TR_PAYMENT_REQUEST>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_VoucherBuildFails_RollsBackAndStateUnchangedInPractice()
    {
        Existing(PaymentRequestState.ReadyForExecution);
        _voucher.Setup(v => v.BuildAndStageAsync(It.IsAny<TB_TR_PAYMENT_REQUEST>(), It.IsAny<string>(), Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentRequestVoucherUnbalancedException("pay", 1m, 2m));

        await Assert.ThrowsAsync<PaymentRequestVoucherUnbalancedException>(() => ExecSut().Handle(ExecCmd(), default));

        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ExecuteValidator_Rules()
    {
        var v = new ExecutePaymentRequestCommandValidator();
        Assert.True(v.Validate(ExecCmd()).IsValid);
        Assert.True(v.Validate(ExecCmd(iban: "IR" + new string('0', 24))).IsValid);
        Assert.True(v.Validate(ExecCmd(iban: "")).IsValid);
        Assert.False(v.Validate(ExecCmd(iban: "IR123")).IsValid);
        Assert.False(v.Validate(ExecCmd(iban: "ir" + new string('1', 24))).IsValid);
        Assert.False(v.Validate(ExecCmd() with { BankReference = "" }).IsValid);
        Assert.False(v.Validate(ExecCmd() with { PaidDate = "1405/05/05" }).IsValid);
        Assert.False(v.Validate(ExecCmd() with { Id = Guid.Empty }).IsValid);
        Assert.False(v.Validate(ExecCmd((TreasuryPaymentMethod)55)).IsValid);
    }
}
