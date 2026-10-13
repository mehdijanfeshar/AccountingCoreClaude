using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Common.FinalApproval;

public class PettyCashFinalApprovalServiceTests
{
    private readonly Mock<IPettyCashExpenseDocRepository> _docs = new();
    private readonly Mock<IPettyCashFundRepository> _funds = new();
    private readonly Mock<IChargeAndCostRepository> _heads = new();
    private readonly Mock<IPettyCashDocEventRepository> _events = new();
    private readonly Mock<IPettyCashReviewAuthorizer> _auth = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();
    private readonly Guid _docId = Guid.NewGuid();
    private readonly Guid _fundId = Guid.NewGuid();

    private PettyCashFinalApprovalService Sut(string caller = "approver")
    {
        _user.SetupGet(u => u.UserId).Returns(caller);
        return new(_docs.Object, _funds.Object, _heads.Object, _events.Object, _auth.Object, _user.Object, _client.Object);
    }

    private TB_PC_EXPENSE_DOC SetupDoc(PettyCashDocState state = PettyCashDocState.PendingReview,
        string? verifier = "inspector", decimal amount = 1000m, decimal vat = 100m, bool deleted = false)
    {
        var doc = new TB_PC_EXPENSE_DOC
        {
            ID = _docId, FUND_ID = _fundId, CHARGEANDCOSTHEAD_ID = Guid.NewGuid(), DOC_STATE = state,
            ADDUSERID = "creator", VERIFIED_BY_USERID = verifier, AMOUNT_BEFORE_TAX = amount, VAT_AMOUNT = vat,
            YEAR = "1404", ISDELETED = deleted,
        };
        _docs.Setup(r => r.GetForUpdateAsync(_docId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(doc);
        return doc;
    }

    private void SetupRoles(params PettyCashRole[] roles)
        => _auth.Setup(a => a.EnsureCanReviewAsync(_docId, _fundId, "creator",
                It.IsAny<IReadOnlyCollection<PettyCashRole>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

    private void SetupFund(decimal limit)
        => _funds.Setup(r => r.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_PC_FUND { ID = _fundId, FINANCE_MANAGER_APPROVAL_LIMIT = limit });

    [Fact]
    public async Task Handle_DocMissing_ThrowsNotFound()
    {
        _docs.Setup(r => r.GetForUpdateAsync(_docId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TB_PC_EXPENSE_DOC?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().FinalApproveAsync(_docId, "0101", null));
    }

    [Fact]
    public async Task Handle_DocDeleted_ThrowsNotFound()
    {
        SetupDoc(deleted: true);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().FinalApproveAsync(_docId, "0101", null));
    }

    [Fact]
    public async Task Handle_RequestsOnlyFinanceManagerAndChiefExecutiveRoles()
    {
        SetupDoc();
        SetupRoles(PettyCashRole.ChiefExecutive);
        SetupFund(1);

        await Sut().FinalApproveAsync(_docId, "0101", null);

        _auth.Verify(a => a.EnsureCanReviewAsync(_docId, _fundId, "creator",
            It.Is<IReadOnlyCollection<PettyCashRole>>(r => r.Count == 2
                && r.Contains(PettyCashRole.FinanceManager) && r.Contains(PettyCashRole.ChiefExecutive)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PettyCashDocState.Draft)]
    [InlineData(PettyCashDocState.New)]
    [InlineData(PettyCashDocState.Returned)]
    [InlineData(PettyCashDocState.Approved)]
    public async Task Handle_StateNotPendingReview_ThrowsStateConflict(PettyCashDocState state)
    {
        SetupDoc(state);
        SetupRoles(PettyCashRole.FinanceManager);

        await Assert.ThrowsAsync<PettyCashReviewStateConflictException>(() => Sut().FinalApproveAsync(_docId, "0101", null));
    }

    [Fact]
    public async Task Handle_NotVerified_ThrowsNotVerified()
    {
        SetupDoc(verifier: null);
        SetupRoles(PettyCashRole.FinanceManager);

        await Assert.ThrowsAsync<PettyCashNotVerifiedException>(() => Sut().FinalApproveAsync(_docId, "0101", null));
    }

    [Fact]
    public async Task Handle_VerifierIsApprover_ThrowsVerifierCannotApprove()
    {
        SetupDoc(verifier: "approver");
        SetupRoles(PettyCashRole.FinanceManager);

        await Assert.ThrowsAsync<PettyCashVerifierCannotApproveException>(() => Sut("approver").FinalApproveAsync(_docId, "0101", null));
    }

    [Fact]
    public async Task Handle_FundMissing_ThrowsNotFound()
    {
        SetupDoc();
        SetupRoles(PettyCashRole.FinanceManager);
        _funds.Setup(r => r.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TB_PC_FUND?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().FinalApproveAsync(_docId, "0101", null));
    }

    [Fact]
    public async Task Handle_FinanceManagerOverLimit_ThrowsAuthorityExceeded()
    {
        SetupDoc(amount: 1000m, vat: 100m); // جمع ۱۱۰۰
        SetupRoles(PettyCashRole.FinanceManager);
        SetupFund(1099m);

        await Assert.ThrowsAsync<PettyCashApprovalAuthorityExceededException>(() => Sut().FinalApproveAsync(_docId, "0101", null));
    }

    [Fact]
    public async Task Handle_FinanceManagerExactlyAtLimit_Approves()
    {
        var doc = SetupDoc(amount: 1000m, vat: 100m);
        SetupRoles(PettyCashRole.FinanceManager);
        SetupFund(1100m);

        await Sut().FinalApproveAsync(_docId, "0101", "ok");

        Assert.Equal(PettyCashDocState.Approved, doc.DOC_STATE);
    }

    [Fact]
    public async Task Handle_ChiefExecutiveIgnoresLimit_Approves()
    {
        var doc = SetupDoc(amount: 1_000_000m, vat: 0m);
        SetupRoles(PettyCashRole.FinanceManager, PettyCashRole.ChiefExecutive);
        SetupFund(10m);

        await Sut().FinalApproveAsync(_docId, "0101", null);

        Assert.Equal(PettyCashDocState.Approved, doc.DOC_STATE);
    }

    [Fact]
    public async Task Handle_NullAmounts_TreatedAsZero()
    {
        var doc = SetupDoc();
        doc.AMOUNT_BEFORE_TAX = null;
        doc.VAT_AMOUNT = null;
        SetupRoles(PettyCashRole.FinanceManager);
        SetupFund(0m);

        await Sut().FinalApproveAsync(_docId, "0101", null);

        Assert.Equal(PettyCashDocState.Approved, doc.DOC_STATE);
    }

    [Fact]
    public async Task Handle_Valid_SyncsHeadAndWritesFinalApproveEvent()
    {
        var doc = SetupDoc();
        var head = new TB_CHARGEANDCOST_HEAD();
        SetupRoles(PettyCashRole.FinanceManager);
        SetupFund(5000m);
        _heads.Setup(r => r.GetHeadForUpdateAsync(doc.CHARGEANDCOSTHEAD_ID, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(head);
        _client.SetupGet(c => c.ClientIp).Returns("9.9.9.9");
        TB_PC_DOC_EVENT? ev = null;
        _events.Setup(e => e.AddAsync(It.IsAny<TB_PC_DOC_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_DOC_EVENT, CancellationToken>((x, _) => ev = x).Returns(Task.CompletedTask);

        var result = await Sut().FinalApproveAsync(_docId, "0101", "تأیید");

        Assert.Same(doc, result);
        Assert.Equal("approver", head.CHANGEUSERID);
        Assert.Equal(PettyCashDocAction.FinalApprove, ev!.ACTION);
        Assert.Equal(PettyCashDocState.PendingReview, ev.FROM_STATE);
        Assert.Equal(PettyCashDocState.Approved, ev.TO_STATE);
        Assert.Equal("تأیید", ev.NOTE);
        Assert.Equal("9.9.9.9", ev.CLIENT_IP);
    }
}
