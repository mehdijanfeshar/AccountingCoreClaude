using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Commands.Treasury.CreatePaymentRequest;
using Accounting.Application.Commands.Treasury.DeletePaymentRequest;
using Accounting.Application.Commands.Treasury.ResumePaymentRequest;
using Accounting.Application.Commands.Treasury.SubmitPaymentRequest;
using Accounting.Application.Commands.Treasury.SuspendPaymentRequest;
using Accounting.Application.Commands.Treasury.UpdatePaymentRequest;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.Accounts;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.PaymentRequests;

/// <summary>چرخهٔ عمر درخواست پرداخت: ایجاد/ویرایش/حذف/ارسال/تعلیق/ازسرگیری — فقط سازنده، فقط حالت‌های مجاز، رویداد حسابرسی.</summary>
public sealed class PaymentRequestLifecycleHandlerTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid Expense = Guid.NewGuid();
    private static readonly Guid PayAccount = Guid.NewGuid();

    private readonly Mock<IPaymentRequestRepository> _requests = new();
    private readonly Mock<IPaymentRequestEventRepository> _events = new();
    private readonly Mock<IAccountCodeReadRepository> _accounts = new();
    private readonly Mock<IBankAccountReadRepository> _banks = new();
    private readonly Mock<IPaymentRequestSubmitRuleChecker> _submitRules = new();
    private readonly Mock<IPaymentRequestTafsiliValidator> _tafsili = new();
    private readonly Mock<IPaymentRequestExecutionAuthorizer> _executionAuth = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();
    private readonly List<TB_TR_PAYMENT_REQUEST_EVENT> _added = new();

    public PaymentRequestLifecycleHandlerTests()
    {
        _user.SetupGet(u => u.UserId).Returns("creator");
        _client.SetupGet(c => c.ClientIp).Returns("9.9.9.9");
        _events.Setup(e => e.AddAsync(It.IsAny<TB_TR_PAYMENT_REQUEST_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_PAYMENT_REQUEST_EVENT, CancellationToken>((e, _) => _added.Add(e)).Returns(Task.CompletedTask);
        _accounts.Setup(a => a.GetByIdAsync(Expense, It.IsAny<CancellationToken>())).ReturnsAsync(Account(Expense));
        _banks.Setup(b => b.GetByIdAsync(PayAccount, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Bank());
        _requests.Setup(r => r.GetNextCodeAsync(Vahed, "1405", It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _requests.Setup(r => r.GetActiveCostCenterTafsiliLinksAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_TR_PAYMENT_REQUEST_LINK_TAFSILI>());
    }

    private static AccountCodeDto Account(Guid id) => new(id, null, null, "6101", "هزینه", null, null, null, null, null, null, null, null, false, null, null);

    private static BankAccountDto Bank() => new(
        PayAccount, "1", "h", null, null, null, null, null, null, Guid.NewGuid(), Vahed, null, null, null, null, null, false,
        new List<BankAccountTafsiliLinkDto>());

    private TB_TR_PAYMENT_REQUEST Existing(PaymentRequestState state, string creator = "creator")
    {
        var e = new TB_TR_PAYMENT_REQUEST
        {
            ID = Id, REQUEST_STATE = state, ADDUSERID = creator, YEAR = "1405", VAHEDCODE = Vahed,
            BENEFICIARY_NATIONAL_ID = "123", INVOICE_REF = "I1", INVOICE_APPROVED = true, DUE_DATE = "14990101",
        };
        _requests.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        return e;
    }

    // ── Create ──────────────────────────────────────────────────────────────

    private CreatePaymentRequestCommand CreateCmd(bool submit = false, decimal? vatPercent = null, params PaymentRequestTafsiliLinkInput[] links)
        => new("ذینفع", "123", null, TreasuryPaymentType.SupplierOrContractor, "I1", true, Expense, links,
            1_000m, vatPercent, null, null, null, "14990101", PayAccount, TreasuryPaymentMethod.Satna, null, "1405", submit)
        { VahedCode = Vahed };

    private CreatePaymentRequestCommandHandler CreateSut() => new(
        _requests.Object, _events.Object, _accounts.Object, _banks.Object, _submitRules.Object,
        _tafsili.Object, _uow.Object, _user.Object, _client.Object);

    [Fact]
    public async Task Create_Draft_StagesDraftWithComputedAmountsAndCreateEvent()
    {
        TB_TR_PAYMENT_REQUEST? added = null;
        _requests.Setup(r => r.AddAsync(It.IsAny<TB_TR_PAYMENT_REQUEST>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_PAYMENT_REQUEST, CancellationToken>((p, _) => added = p).Returns(Task.CompletedTask);
        var link = new PaymentRequestTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid());

        var id = await CreateSut().Handle(CreateCmd(submit: false, vatPercent: 10m, link), default);

        Assert.Equal(id, added!.ID);
        Assert.Equal("PAY-000005", added.CODE);
        Assert.Equal(PaymentRequestState.Draft, added.REQUEST_STATE);
        Assert.Null(added.SUBMITTED_DATE);
        Assert.Equal(100m, added.VAT_AMOUNT);
        Assert.Equal(1_100m, added.NET_PAYABLE_AMOUNT);
        Assert.Equal("creator", added.ADDUSERID);
        Assert.Equal(Vahed, added.VAHEDCODE);
        var ev = Assert.Single(_added);
        Assert.Equal(PaymentRequestEventAction.Create, ev.ACTION);
        Assert.Null(ev.FROM_STATE);
        Assert.Equal(PaymentRequestState.Draft, ev.TO_STATE);
        _requests.Verify(r => r.AddCostCenterTafsiliLinkAsync(
            It.Is<TB_TR_PAYMENT_REQUEST_LINK_TAFSILI>(l => l.TAFSILI_ID == link.TafsiliId && l.LEVEL_ID == link.LevelId), It.IsAny<CancellationToken>()), Times.Once);
        _submitRules.Verify(s => s.EnsureSubmittableAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithSubmit_RunsSubmitRulesAndStartsAtUnitManager()
    {
        TB_TR_PAYMENT_REQUEST? added = null;
        _requests.Setup(r => r.AddAsync(It.IsAny<TB_TR_PAYMENT_REQUEST>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_PAYMENT_REQUEST, CancellationToken>((p, _) => added = p).Returns(Task.CompletedTask);

        await CreateSut().Handle(CreateCmd(submit: true), default);

        Assert.Equal(PaymentRequestState.PendingUnitManager, added!.REQUEST_STATE);
        Assert.NotNull(added.SUBMITTED_DATE);
        _submitRules.Verify(s => s.EnsureSubmittableAsync(added.ID, "123", "I1", true, "14990101", Vahed, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(PaymentRequestState.PendingUnitManager, Assert.Single(_added).TO_STATE);
    }

    [Fact]
    public async Task Create_SubmitRuleFails_DoesNotSave()
    {
        _submitRules.Setup(s => s.EnsureSubmittableAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentRequestInvoiceNotApprovedException(Id));

        await Assert.ThrowsAsync<PaymentRequestInvoiceNotApprovedException>(() => CreateSut().Handle(CreateCmd(submit: true), default));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ExpenseAccountMissing_ThrowsNotFound()
    {
        _accounts.Setup(a => a.GetByIdAsync(Expense, It.IsAny<CancellationToken>())).ReturnsAsync((AccountCodeDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().Handle(CreateCmd(), default));
    }

    [Fact]
    public async Task Create_PaymentBankAccountMissing_ThrowsNotFound()
    {
        _banks.Setup(b => b.GetByIdAsync(PayAccount, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().Handle(CreateCmd(), default));
    }

    [Fact]
    public async Task Create_TafsiliValidationFails_Propagates()
    {
        _tafsili.Setup(t => t.EnsureBeneficiaryTafsiliValidAsync(It.IsAny<Guid?>(), Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentRequestBeneficiaryGroupNotConfiguredException(Vahed));

        await Assert.ThrowsAsync<PaymentRequestBeneficiaryGroupNotConfiguredException>(() => CreateSut().Handle(CreateCmd(), default));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Update ──────────────────────────────────────────────────────────────

    private UpdatePaymentRequestCommand UpdateCmd(params PaymentRequestTafsiliLinkInput[] links)
        => new(Id, "جدید", null, null, TreasuryPaymentType.SupplierOrContractor, null, false, Expense, links,
            2_000m, null, 50m, 5m, null, "14990202", PayAccount, null, "x") { VahedCode = Vahed };

    private UpdatePaymentRequestCommandHandler UpdateSut() => new(
        _requests.Object, _events.Object, _accounts.Object, _banks.Object, _tafsili.Object, _uow.Object, _user.Object, _client.Object);

    [Theory]
    [InlineData(PaymentRequestState.Draft)]
    [InlineData(PaymentRequestState.Returned)]
    public async Task Update_EditableState_AppliesFieldsAndRecomputesNet(PaymentRequestState state)
    {
        var e = Existing(state);

        await UpdateSut().Handle(UpdateCmd(), default);

        Assert.Equal("جدید", e.BENEFICIARY_NAME);
        Assert.Equal(2_000m, e.AMOUNT_BEFORE_TAX);
        Assert.Equal(50m, e.VAT_AMOUNT);
        Assert.Equal(100m, e.INSURANCE_DEDUCTION_AMOUNT);
        Assert.Equal(1_950m, e.NET_PAYABLE_AMOUNT);
        Assert.Equal(state, e.REQUEST_STATE);
        Assert.Equal(PaymentRequestEventAction.Update, Assert.Single(_added).ACTION);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReconcilesTafsiliLinks_SoftDeletesRemovedAndAddsNew()
    {
        Existing(PaymentRequestState.Draft);
        var keep = new TB_TR_PAYMENT_REQUEST_LINK_TAFSILI { TAFSILI_ID = Guid.NewGuid(), LEVEL_ID = Guid.NewGuid() };
        var drop = new TB_TR_PAYMENT_REQUEST_LINK_TAFSILI { TAFSILI_ID = Guid.NewGuid(), LEVEL_ID = Guid.NewGuid() };
        _requests.Setup(r => r.GetActiveCostCenterTafsiliLinksAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_TR_PAYMENT_REQUEST_LINK_TAFSILI> { keep, drop });
        var fresh = new PaymentRequestTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid());

        await UpdateSut().Handle(UpdateCmd(new PaymentRequestTafsiliLinkInput(keep.TAFSILI_ID, keep.LEVEL_ID), fresh), default);

        Assert.False(keep.ISDELETED);
        Assert.True(drop.ISDELETED);
        _requests.Verify(r => r.AddCostCenterTafsiliLinkAsync(
            It.Is<TB_TR_PAYMENT_REQUEST_LINK_TAFSILI>(l => l.TAFSILI_ID == fresh.TafsiliId), It.IsAny<CancellationToken>()), Times.Once);
        _requests.Verify(r => r.AddCostCenterTafsiliLinkAsync(It.IsAny<TB_TR_PAYMENT_REQUEST_LINK_TAFSILI>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PaymentRequestState.PendingUnitManager)]
    [InlineData(PaymentRequestState.ReadyForExecution)]
    [InlineData(PaymentRequestState.Executed)]
    public async Task Update_NonEditableState_ThrowsStateConflict(PaymentRequestState state)
    {
        Existing(state);

        await Assert.ThrowsAsync<PaymentRequestStateConflictException>(() => UpdateSut().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_NotCreator_Throws()
    {
        Existing(PaymentRequestState.Draft, creator: "someoneElse");

        await Assert.ThrowsAsync<PaymentRequestNotCreatorException>(() => UpdateSut().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_NotFoundOrDeleted_Throws()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => UpdateSut().Handle(UpdateCmd(), default));

        Existing(PaymentRequestState.Draft).ISDELETED = true;
        await Assert.ThrowsAsync<NotFoundException>(() => UpdateSut().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_ExpenseAccountMissing_ThrowsNotFound()
    {
        Existing(PaymentRequestState.Draft);
        _accounts.Setup(a => a.GetByIdAsync(Expense, It.IsAny<CancellationToken>())).ReturnsAsync((AccountCodeDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => UpdateSut().Handle(UpdateCmd(), default));
    }

    // ── Delete ──────────────────────────────────────────────────────────────

    private DeletePaymentRequestCommandHandler DeleteSut()
        => new(_requests.Object, _events.Object, _uow.Object, _user.Object, _client.Object);

    [Fact]
    public async Task Delete_DraftByCreator_SoftDeletesAndLogsEvent()
    {
        var e = Existing(PaymentRequestState.Draft);

        await DeleteSut().Handle(new DeletePaymentRequestCommand(Id) { VahedCode = Vahed }, default);

        Assert.True(e.ISDELETED);
        var ev = Assert.Single(_added);
        Assert.Equal(PaymentRequestEventAction.Delete, ev.ACTION);
        Assert.Equal(PaymentRequestState.Draft, ev.FROM_STATE);
    }

    [Fact]
    public async Task Delete_NotCreator_Throws_AndNotDeleted()
    {
        var e = Existing(PaymentRequestState.Draft, creator: "other");

        await Assert.ThrowsAsync<PaymentRequestNotCreatorException>(
            () => DeleteSut().Handle(new DeletePaymentRequestCommand(Id) { VahedCode = Vahed }, default));

        Assert.False(e.ISDELETED);
    }

    [Fact]
    public async Task Delete_PendingState_Throws()
    {
        Existing(PaymentRequestState.PendingCeo);

        await Assert.ThrowsAsync<PaymentRequestStateConflictException>(
            () => DeleteSut().Handle(new DeletePaymentRequestCommand(Id) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Delete_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(
            () => DeleteSut().Handle(new DeletePaymentRequestCommand(Id) { VahedCode = Vahed }, default));

    // ── Submit ──────────────────────────────────────────────────────────────

    private SubmitPaymentRequestCommandHandler SubmitSut()
        => new(_requests.Object, _events.Object, _submitRules.Object, _uow.Object, _user.Object, _client.Object);

    [Theory]
    [InlineData(PaymentRequestState.Draft)]
    [InlineData(PaymentRequestState.Returned)]
    public async Task Submit_Valid_MovesToPendingUnitManager(PaymentRequestState from)
    {
        var e = Existing(from);

        await SubmitSut().Handle(new SubmitPaymentRequestCommand(Id) { VahedCode = Vahed }, default);

        Assert.Equal(PaymentRequestState.PendingUnitManager, e.REQUEST_STATE);
        Assert.NotNull(e.SUBMITTED_DATE);
        var ev = Assert.Single(_added);
        Assert.Equal(PaymentRequestEventAction.Submit, ev.ACTION);
        Assert.Equal(from, ev.FROM_STATE);
        // خودِ درخواست از بررسی تکراری‌بودن مستثنا می‌شود
        _submitRules.Verify(s => s.EnsureSubmittableAsync(Id, "123", "I1", true, "14990101", Vahed, Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Submit_NotCreator_Throws()
    {
        Existing(PaymentRequestState.Draft, creator: "other");

        await Assert.ThrowsAsync<PaymentRequestNotCreatorException>(
            () => SubmitSut().Handle(new SubmitPaymentRequestCommand(Id) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Submit_AlreadyPending_ThrowsStateConflict()
    {
        Existing(PaymentRequestState.PendingUnitManager);

        await Assert.ThrowsAsync<PaymentRequestStateConflictException>(
            () => SubmitSut().Handle(new SubmitPaymentRequestCommand(Id) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Submit_RuleFails_StateUnchangedAndNoEvent()
    {
        var e = Existing(PaymentRequestState.Draft);
        _submitRules.Setup(s => s.EnsureSubmittableAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentRequestDueDatePastException(Id, "14000101"));

        await Assert.ThrowsAsync<PaymentRequestDueDatePastException>(
            () => SubmitSut().Handle(new SubmitPaymentRequestCommand(Id) { VahedCode = Vahed }, default));

        Assert.Equal(PaymentRequestState.Draft, e.REQUEST_STATE);
        Assert.Empty(_added);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(
            () => SubmitSut().Handle(new SubmitPaymentRequestCommand(Id) { VahedCode = Vahed }, default));

    // ── Suspend / Resume ────────────────────────────────────────────────────

    [Fact]
    public async Task Suspend_ReadyForExecution_SetsSuspendedWithReason()
    {
        var e = Existing(PaymentRequestState.ReadyForExecution);
        var sut = new SuspendPaymentRequestCommandHandler(_requests.Object, _events.Object, _executionAuth.Object, _uow.Object, _user.Object, _client.Object);

        await sut.Handle(new SuspendPaymentRequestCommand(Id, "کمبود نقدینگی") { VahedCode = Vahed }, default);

        Assert.Equal(PaymentRequestState.Suspended, e.REQUEST_STATE);
        Assert.Equal("کمبود نقدینگی", e.SUSPEND_REASON);
        var ev = Assert.Single(_added);
        Assert.Equal(PaymentRequestEventAction.Suspend, ev.ACTION);
        Assert.Equal("کمبود نقدینگی", ev.NOTE);
        _executionAuth.Verify(a => a.EnsureTreasurerAsync(Id, Vahed, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PaymentRequestState.Draft)]
    [InlineData(PaymentRequestState.Suspended)]
    [InlineData(PaymentRequestState.Executed)]
    public async Task Suspend_WrongState_Throws(PaymentRequestState state)
    {
        Existing(state);
        var sut = new SuspendPaymentRequestCommandHandler(_requests.Object, _events.Object, _executionAuth.Object, _uow.Object, _user.Object, _client.Object);

        await Assert.ThrowsAsync<PaymentRequestStateConflictException>(
            () => sut.Handle(new SuspendPaymentRequestCommand(Id, "x") { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Suspend_NotTreasurer_Throws_StateUnchanged()
    {
        var e = Existing(PaymentRequestState.ReadyForExecution);
        _executionAuth.Setup(a => a.EnsureTreasurerAsync(Id, Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentRequestTreasurerRoleRequiredException(Id));
        var sut = new SuspendPaymentRequestCommandHandler(_requests.Object, _events.Object, _executionAuth.Object, _uow.Object, _user.Object, _client.Object);

        await Assert.ThrowsAsync<PaymentRequestTreasurerRoleRequiredException>(
            () => sut.Handle(new SuspendPaymentRequestCommand(Id, "x") { VahedCode = Vahed }, default));

        Assert.Equal(PaymentRequestState.ReadyForExecution, e.REQUEST_STATE);
    }

    [Fact]
    public async Task Resume_Suspended_BackToReadyForExecution()
    {
        var e = Existing(PaymentRequestState.Suspended);
        var sut = new ResumePaymentRequestCommandHandler(_requests.Object, _events.Object, _executionAuth.Object, _uow.Object, _user.Object, _client.Object);

        await sut.Handle(new ResumePaymentRequestCommand(Id) { VahedCode = Vahed }, default);

        Assert.Equal(PaymentRequestState.ReadyForExecution, e.REQUEST_STATE);
        Assert.Equal(PaymentRequestEventAction.Resume, Assert.Single(_added).ACTION);
    }

    [Theory]
    [InlineData(PaymentRequestState.ReadyForExecution)]
    [InlineData(PaymentRequestState.Draft)]
    public async Task Resume_NotSuspended_Throws(PaymentRequestState state)
    {
        Existing(state);
        var sut = new ResumePaymentRequestCommandHandler(_requests.Object, _events.Object, _executionAuth.Object, _uow.Object, _user.Object, _client.Object);

        await Assert.ThrowsAsync<PaymentRequestStateConflictException>(
            () => sut.Handle(new ResumePaymentRequestCommand(Id) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Resume_NotFound_Throws()
    {
        var sut = new ResumePaymentRequestCommandHandler(_requests.Object, _events.Object, _executionAuth.Object, _uow.Object, _user.Object, _client.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new ResumePaymentRequestCommand(Id) { VahedCode = Vahed }, default));
    }

    // ── Validators ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateValidator_Valid_Passes()
        => Assert.True(new CreatePaymentRequestCommandValidator().Validate(CreateCmd()).IsValid);

    [Fact]
    public void CreateValidator_RuleViolations_Fail()
    {
        var v = new CreatePaymentRequestCommandValidator();
        Assert.False(v.Validate(CreateCmd() with { BeneficiaryName = "" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { BeneficiaryNationalId = new string('1', 12) }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { ExpenseAccountId = Guid.Empty }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { PaymentAccountId = Guid.Empty }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { AmountBeforeTax = 0 }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { VatPercent = 101m }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { VatPercent = -1m }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { VatAmount = -1m }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { InsuranceDeductionPercent = 150m }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { InsuranceDeductionAmount = -5m }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { DueDate = "1499-01-01" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { PaymentMethod = (TreasuryPaymentMethod)77 }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { PaymentType = (TreasuryPaymentType)77 }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { Year = "abcd" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { CostCenterTafsilis = null! }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { CostCenterTafsilis = new[] { new PaymentRequestTafsiliLinkInput(Guid.Empty, Guid.NewGuid()) } }).IsValid);
    }

    [Fact]
    public void CreateValidator_BoundaryPercentsAndNullMethod_Pass()
    {
        var v = new CreatePaymentRequestCommandValidator();
        Assert.True(v.Validate(CreateCmd() with { VatPercent = 0m, InsuranceDeductionPercent = 100m, PaymentMethod = null }).IsValid);
    }

    [Fact]
    public void UpdateValidator_ValidatesBasics()
    {
        var v = new UpdatePaymentRequestCommandValidator();
        Assert.True(v.Validate(UpdateCmd()).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { Id = Guid.Empty }).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { AmountBeforeTax = -1 }).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { DueDate = "x" }).IsValid);
    }

    [Fact]
    public void SuspendValidator_RequiresReason()
    {
        var v = new SuspendPaymentRequestCommandValidator();
        Assert.False(v.Validate(new SuspendPaymentRequestCommand(Id, "")).IsValid);
        Assert.False(v.Validate(new SuspendPaymentRequestCommand(Guid.Empty, "x")).IsValid);
        Assert.True(v.Validate(new SuspendPaymentRequestCommand(Id, "x")).IsValid);
    }

    [Fact]
    public void LinkInputValidator_RequiresBothIds()
    {
        var v = new PaymentRequestTafsiliLinkInputValidator();
        Assert.True(v.Validate(new PaymentRequestTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid())).IsValid);
        Assert.False(v.Validate(new PaymentRequestTafsiliLinkInput(Guid.Empty, Guid.NewGuid())).IsValid);
        Assert.False(v.Validate(new PaymentRequestTafsiliLinkInput(Guid.NewGuid(), Guid.Empty)).IsValid);
    }
}
