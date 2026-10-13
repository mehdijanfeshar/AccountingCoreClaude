using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.Treasury;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.Common;

/// <summary>گذارهای زنجیرهٔ تأیید درخواست پرداخت: نقش مرحله، SoD، سقف تأیید گروهی، آستانهٔ مدیرعامل و سند بدهی.</summary>
public sealed class PaymentRequestApprovalServiceTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();

    private readonly Mock<IPaymentRequestRepository> _requests = new();
    private readonly Mock<IPaymentRequestEventRepository> _events = new();
    private readonly Mock<ITreasurySettingReadRepository> _settings = new();
    private readonly Mock<ITreasuryRoleRepository> _roles = new();
    private readonly Mock<IPaymentRequestLiabilityVoucherBuilder> _liability = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();
    private readonly List<TB_TR_PAYMENT_REQUEST_EVENT> _added = new();

    public PaymentRequestApprovalServiceTests()
    {
        _user.SetupGet(u => u.UserId).Returns("approver");
        _client.SetupGet(c => c.ClientIp).Returns("10.0.0.1");
        _events.Setup(e => e.AddAsync(It.IsAny<TB_TR_PAYMENT_REQUEST_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_PAYMENT_REQUEST_EVENT, CancellationToken>((e, _) => _added.Add(e))
            .Returns(Task.CompletedTask);
        _settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Setting(ceoThreshold: 1_000m, bulkLimit: 500m));
        _liability.Setup(b => b.BuildAndStageAsync(It.IsAny<TB_TR_PAYMENT_REQUEST>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRequestVoucherBuildResult(Guid.NewGuid(), "77", "14050101", "1405", Array.Empty<PaymentRequestVoucherLineResult>()));
    }

    private static TreasurySettingDto Setting(decimal ceoThreshold, decimal bulkLimit)
        => TreasuryTestData.Setting(ceoThreshold: ceoThreshold, bulkLimit: bulkLimit);

    private PaymentRequestApprovalService Sut() => new(
        _requests.Object, _events.Object, _settings.Object, _roles.Object, _liability.Object, _user.Object, _client.Object);

    private void GiveRoles(params TreasuryRole[] roles)
        => _roles.Setup(r => r.GetActiveRolesAsync(Vahed, "approver", It.IsAny<CancellationToken>())).ReturnsAsync(roles);

    private TB_TR_PAYMENT_REQUEST Request(PaymentRequestState state, decimal net = 100m, string creator = "creator")
    {
        var entity = new TB_TR_PAYMENT_REQUEST
        {
            ID = Id, REQUEST_STATE = state, NET_PAYABLE_AMOUNT = net, ADDUSERID = creator, YEAR = "1405", VAHEDCODE = Vahed,
        };
        _requests.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        return entity;
    }

    [Fact]
    public async Task Approve_NotFound_Throws()
    {
        _requests.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((TB_TR_PAYMENT_REQUEST?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().ApproveAsync(Id, Vahed, null, false));
    }

    [Fact]
    public async Task Approve_SoftDeleted_ThrowsNotFound()
    {
        Request(PaymentRequestState.PendingUnitManager).ISDELETED = true;

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().ApproveAsync(Id, Vahed, null, false));
    }

    [Theory]
    [InlineData(PaymentRequestState.Draft)]
    [InlineData(PaymentRequestState.ReadyForExecution)]
    [InlineData(PaymentRequestState.Executed)]
    [InlineData(PaymentRequestState.Rejected)]
    public async Task Approve_NotPendingState_ThrowsStateConflict(PaymentRequestState state)
    {
        Request(state);

        await Assert.ThrowsAsync<PaymentRequestStateConflictException>(() => Sut().ApproveAsync(Id, Vahed, null, false));
    }

    [Fact]
    public async Task Approve_MissingStageRole_Throws()
    {
        Request(PaymentRequestState.PendingFinanceManager);
        GiveRoles(TreasuryRole.UnitManager);

        await Assert.ThrowsAsync<PaymentRequestRoleRequiredException>(() => Sut().ApproveAsync(Id, Vahed, null, false));
    }

    [Fact]
    public async Task Approve_CallerIsCreator_ThrowsApproverConflict()
    {
        Request(PaymentRequestState.PendingUnitManager, creator: "approver");
        GiveRoles(TreasuryRole.UnitManager);

        await Assert.ThrowsAsync<PaymentRequestApproverConflictException>(() => Sut().ApproveAsync(Id, Vahed, null, false));
    }

    [Fact]
    public async Task Approve_PreviousStageApproverIsCaller_ThrowsConsecutiveConflict()
    {
        Request(PaymentRequestState.PendingFinanceManager);
        GiveRoles(TreasuryRole.FinanceManager);
        _events.Setup(e => e.GetLastApproveEventAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_TR_PAYMENT_REQUEST_EVENT { ADDUSERID = "approver" });

        await Assert.ThrowsAsync<PaymentRequestConsecutiveApproverConflictException>(() => Sut().ApproveAsync(Id, Vahed, null, false));
    }

    [Fact]
    public async Task Approve_SettingsMissing_Throws()
    {
        Request(PaymentRequestState.PendingUnitManager);
        GiveRoles(TreasuryRole.UnitManager);
        _settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((TreasurySettingDto?)null);

        await Assert.ThrowsAsync<PaymentRequestSettingsMissingException>(() => Sut().ApproveAsync(Id, Vahed, null, false));
    }

    [Fact]
    public async Task Approve_BulkOverLimit_Throws()
    {
        Request(PaymentRequestState.PendingUnitManager, net: 501m);
        GiveRoles(TreasuryRole.UnitManager);

        await Assert.ThrowsAsync<PaymentRequestBulkLimitExceededException>(() => Sut().ApproveAsync(Id, Vahed, null, true));
    }

    [Fact]
    public async Task Approve_NonBulkOverBulkLimit_IsAllowed()
    {
        var request = Request(PaymentRequestState.PendingUnitManager, net: 501m);
        GiveRoles(TreasuryRole.UnitManager);

        await Sut().ApproveAsync(Id, Vahed, null, false);

        Assert.Equal(PaymentRequestState.PendingFinanceManager, request.REQUEST_STATE);
    }

    [Fact]
    public async Task Approve_UnitManager_MovesToFinanceManagerAndLogsEvent()
    {
        var request = Request(PaymentRequestState.PendingUnitManager);
        GiveRoles(TreasuryRole.UnitManager);

        await Sut().ApproveAsync(Id, Vahed, "ok", false);

        Assert.Equal(PaymentRequestState.PendingFinanceManager, request.REQUEST_STATE);
        Assert.Equal("approver", request.CHANGEUSERID);
        Assert.NotNull(request.UPDATEDDATE);
        var ev = Assert.Single(_added);
        Assert.Equal(PaymentRequestEventAction.Approve, ev.ACTION);
        Assert.Equal(PaymentRequestState.PendingUnitManager, ev.FROM_STATE);
        Assert.Equal(PaymentRequestState.PendingFinanceManager, ev.TO_STATE);
        Assert.Equal("ok", ev.NOTE);
        Assert.Equal("10.0.0.1", ev.CLIENT_IP);
        Assert.Equal(Vahed, ev.VAHEDCODE);
        _liability.Verify(b => b.BuildAndStageAsync(It.IsAny<TB_TR_PAYMENT_REQUEST>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Approve_FinanceManager_AboveCeoThreshold_MovesToCeo()
    {
        var request = Request(PaymentRequestState.PendingFinanceManager, net: 1_001m);
        GiveRoles(TreasuryRole.FinanceManager);

        await Sut().ApproveAsync(Id, Vahed, null, false);

        Assert.Equal(PaymentRequestState.PendingCeo, request.REQUEST_STATE);
        _liability.Verify(b => b.BuildAndStageAsync(It.IsAny<TB_TR_PAYMENT_REQUEST>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Approve_FinanceManager_AtCeoThreshold_GoesStraightToReady_AndIssuesLiabilityVoucher()
    {
        // مرز: دقیقاً برابر آستانه نیاز به مدیرعامل ندارد (فقط «بیشتر از»)
        var request = Request(PaymentRequestState.PendingFinanceManager, net: 1_000m);
        GiveRoles(TreasuryRole.FinanceManager);
        var voucherId = Guid.NewGuid();
        _liability.Setup(b => b.BuildAndStageAsync(request, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRequestVoucherBuildResult(voucherId, "77", "14050101", "1405", Array.Empty<PaymentRequestVoucherLineResult>()));

        await Sut().ApproveAsync(Id, Vahed, null, false);

        Assert.Equal(PaymentRequestState.ReadyForExecution, request.REQUEST_STATE);
        Assert.Equal(voucherId, request.LIABILITY_VOUCHER_ID);
        Assert.Equal(2, _added.Count);
        Assert.Equal(PaymentRequestEventAction.Approve, _added[0].ACTION);
        Assert.Equal(PaymentRequestEventAction.LiabilityVoucherIssued, _added[1].ACTION);
        Assert.Contains("77", _added[1].NOTE);
    }

    [Fact]
    public async Task Approve_Ceo_MovesToReadyForExecution()
    {
        var request = Request(PaymentRequestState.PendingCeo, net: 5_000m);
        GiveRoles(TreasuryRole.Ceo);

        await Sut().ApproveAsync(Id, Vahed, null, false);

        Assert.Equal(PaymentRequestState.ReadyForExecution, request.REQUEST_STATE);
        Assert.NotNull(request.LIABILITY_VOUCHER_ID);
    }

    [Fact]
    public async Task Approve_VoucherBuildFails_PropagatesException()
    {
        Request(PaymentRequestState.PendingCeo);
        GiveRoles(TreasuryRole.Ceo);
        _liability.Setup(b => b.BuildAndStageAsync(It.IsAny<TB_TR_PAYMENT_REQUEST>(), Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentRequestVoucherUnbalancedException("liability", 1m, 2m));

        await Assert.ThrowsAsync<PaymentRequestVoucherUnbalancedException>(() => Sut().ApproveAsync(Id, Vahed, null, false));
    }

    // ── Return / Reject ─────────────────────────────────────────────────────

    [Fact]
    public async Task Return_Valid_SetsReturnedAndLogsReason()
    {
        var request = Request(PaymentRequestState.PendingFinanceManager);
        GiveRoles(TreasuryRole.FinanceManager);

        await Sut().ReturnAsync(Id, Vahed, "اصلاح مبلغ");

        Assert.Equal(PaymentRequestState.Returned, request.REQUEST_STATE);
        var ev = Assert.Single(_added);
        Assert.Equal(PaymentRequestEventAction.Return, ev.ACTION);
        Assert.Equal("اصلاح مبلغ", ev.NOTE);
        Assert.Equal(PaymentRequestState.PendingFinanceManager, ev.FROM_STATE);
    }

    [Fact]
    public async Task Return_ConsecutiveApproverIsAllowed()
    {
        // برگشت بخشی از پیشروی زنجیره نیست؛ بررسی تأییدکنندهٔ متوالی اعمال نمی‌شود
        var request = Request(PaymentRequestState.PendingFinanceManager);
        GiveRoles(TreasuryRole.FinanceManager);
        _events.Setup(e => e.GetLastApproveEventAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_TR_PAYMENT_REQUEST_EVENT { ADDUSERID = "approver" });

        await Sut().ReturnAsync(Id, Vahed, "r");

        Assert.Equal(PaymentRequestState.Returned, request.REQUEST_STATE);
    }

    [Fact]
    public async Task Return_Creator_Throws()
    {
        Request(PaymentRequestState.PendingUnitManager, creator: "approver");
        GiveRoles(TreasuryRole.UnitManager);

        await Assert.ThrowsAsync<PaymentRequestApproverConflictException>(() => Sut().ReturnAsync(Id, Vahed, "r"));
    }

    [Fact]
    public async Task Return_WrongRole_Throws()
    {
        Request(PaymentRequestState.PendingCeo);
        GiveRoles(TreasuryRole.FinanceManager);

        await Assert.ThrowsAsync<PaymentRequestRoleRequiredException>(() => Sut().ReturnAsync(Id, Vahed, "r"));
    }

    [Fact]
    public async Task Reject_Valid_SetsRejected()
    {
        var request = Request(PaymentRequestState.PendingCeo);
        GiveRoles(TreasuryRole.Ceo);

        await Sut().RejectAsync(Id, Vahed, "غیرمجاز");

        Assert.Equal(PaymentRequestState.Rejected, request.REQUEST_STATE);
        Assert.Equal(PaymentRequestEventAction.Reject, Assert.Single(_added).ACTION);
    }

    [Fact]
    public async Task Reject_NotPending_Throws()
    {
        Request(PaymentRequestState.Returned);

        await Assert.ThrowsAsync<PaymentRequestStateConflictException>(() => Sut().RejectAsync(Id, Vahed, "r"));
    }

    [Fact]
    public async Task Reject_Creator_Throws()
    {
        Request(PaymentRequestState.PendingUnitManager, creator: "approver");
        GiveRoles(TreasuryRole.UnitManager);

        await Assert.ThrowsAsync<PaymentRequestApproverConflictException>(() => Sut().RejectAsync(Id, Vahed, "r"));
    }
}
