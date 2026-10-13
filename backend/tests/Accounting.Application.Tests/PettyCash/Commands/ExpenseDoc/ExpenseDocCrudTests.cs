using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.PettyCash.CreatePettyCashExpenseDoc;
using Accounting.Application.Commands.PettyCash.DeletePettyCashExpenseDoc;
using Accounting.Application.Commands.PettyCash.SubmitPettyCashExpenseDoc;
using Accounting.Application.Commands.PettyCash.UpdatePettyCashExpenseDoc;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.PettyCash;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.ExpenseDoc;

/// <summary>ایجاد/ویرایش/ارسال/حذف صورت‌هزینه تنخواه توسط تنخواه‌دار.</summary>
public class ExpenseDocCrudTests
{
    private readonly Mock<IPettyCashFundRepository> _funds = new();
    private readonly Mock<IExpenseRepository> _expenses = new();
    private readonly Mock<IChargeAndCostRepository> _charge = new();
    private readonly Mock<IPettyCashExpenseDocRepository> _docs = new();
    private readonly Mock<IPettyCashDocEventRepository> _events = new();
    private readonly Mock<IPettyCashDocEventReadRepository> _eventsRead = new();
    private readonly Mock<IPettyCashSubmitRuleChecker> _rules = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();
    private readonly Guid _fundId = Guid.NewGuid();
    private readonly Guid _expenseId = Guid.NewGuid();
    private readonly Guid _docId = Guid.NewGuid();
    private readonly TB_PC_FUND _fund;
    private readonly List<TB_PC_DOC_EVENT> _added = new();

    public ExpenseDocCrudTests()
    {
        _user.SetupGet(u => u.UserId).Returns("custodian1");
        _client.SetupGet(c => c.ClientIp).Returns("1.2.3.4");
        _fund = new TB_PC_FUND { ID = _fundId, CUSTODIAN_USERID = "custodian1", IS_ACTIVE = true, CEILING = 1000m, PER_DOC_LIMIT = 500m };
        _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(_fund);
        _expenses.Setup(e => e.GetForUpdateAsync(_expenseId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(new TB_EXPENCE());
        _events.Setup(e => e.AddAsync(It.IsAny<TB_PC_DOC_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_DOC_EVENT, CancellationToken>((x, _) => _added.Add(x)).Returns(Task.CompletedTask);
    }

    private void SaveNever() => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    private void SaveOnce() => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    private TB_PC_EXPENSE_DOC SetupDoc(PettyCashDocState state, bool deleted = false)
    {
        var d = new TB_PC_EXPENSE_DOC
        {
            ID = _docId, FUND_ID = _fundId, CHARGEANDCOSTHEAD_ID = Guid.NewGuid(), DOC_STATE = state, ISDELETED = deleted,
            VENDOR_NAME = "فروشنده", VENDOR_NATIONAL_ID = "123", INVOICE_NO = "A1", INVOICE_DATE = "14040101",
            EVIDENCE_TYPE = PettyCashEvidenceType.Receipt, AMOUNT_BEFORE_TAX = 100m, VAT_AMOUNT = 9m, YEAR = "1404",
        };
        _docs.Setup(x => x.GetForUpdateAsync(_docId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(d);
        return d;
    }

    private (TB_CHARGEANDCOST_HEAD head, TB_CHARGEANDCOST_DETAIL detail) SetupLegacy(TB_PC_EXPENSE_DOC d)
    {
        var head = new TB_CHARGEANDCOST_HEAD { ID = d.CHARGEANDCOSTHEAD_ID, CHARGEANDCOST_DATE = "14040102", DESCRIPTION = "شرح" };
        var detail = new TB_CHARGEANDCOST_DETAIL { EXPENSE_ID = _expenseId };
        _charge.Setup(c => c.GetHeadForUpdateAsync(d.CHARGEANDCOSTHEAD_ID, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(head);
        _charge.Setup(c => c.GetSingleDetailForUpdateAsync(d.CHARGEANDCOSTHEAD_ID, It.IsAny<CancellationToken>())).ReturnsAsync(detail);
        return (head, detail);
    }

    // ---------- Create ----------

    private CreatePettyCashExpenseDocCommandHandler Create() => new(_funds.Object, _expenses.Object, _charge.Object, _docs.Object,
        _events.Object, _rules.Object, _uow.Object, _user.Object, _client.Object);

    private CreatePettyCashExpenseDocCommand CreateCmd(bool submit = false, string vendor = "فروشنده", string? nid = "123", string? inv = "A1")
        => new(_fundId, _expenseId, "14040102", "1404", vendor, nid, inv, "14040101", PettyCashEvidenceType.Receipt, 100m, 9m, "شرح", submit)
        { VahedCode = "0101" };

    [Fact]
    public async Task Create_FundMissing_ThrowsNotFound()
    {
        _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((TB_PC_FUND?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => Create().Handle(CreateCmd(), default));
    }

    [Fact]
    public async Task Create_NotCustodian_Throws()
    {
        _fund.CUSTODIAN_USERID = "other";
        await Assert.ThrowsAsync<PettyCashNotCustodianException>(() => Create().Handle(CreateCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Create_InactiveFund_Throws()
    {
        _fund.IS_ACTIVE = false;
        await Assert.ThrowsAsync<PettyCashFundInactiveException>(() => Create().Handle(CreateCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Create_ExpenseMissing_ThrowsNotFound()
    {
        _expenses.Setup(e => e.GetForUpdateAsync(_expenseId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((TB_EXPENCE?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => Create().Handle(CreateCmd(), default));
    }

    [Fact]
    public async Task Create_DuplicateInvoice_Throws()
    {
        _docs.Setup(d => d.ExistsActiveDuplicateAsync("123", "A1", "0101", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<PettyCashDuplicateExpenseDocException>(() => Create().Handle(CreateCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Create_NoInvoiceNo_SkipsDuplicateCheck()
    {
        await Create().Handle(CreateCmd(inv: null), default);
        _docs.Verify(d => d.ExistsActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_Draft_DoesNotRunSubmitRules_AndStampsDraft()
    {
        _charge.Setup(c => c.GetNextCodeAsync("0101", "1404", ChargeAndCostType.Cost, It.IsAny<CancellationToken>())).ReturnsAsync(7);
        TB_CHARGEANDCOST_HEAD? head = null; TB_CHARGEANDCOST_DETAIL? detail = null; TB_PC_EXPENSE_DOC? doc = null;
        _charge.Setup(c => c.AddHeadAsync(It.IsAny<TB_CHARGEANDCOST_HEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHARGEANDCOST_HEAD, CancellationToken>((x, _) => head = x).Returns(Task.CompletedTask);
        _charge.Setup(c => c.AddDetailAsync(It.IsAny<TB_CHARGEANDCOST_DETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHARGEANDCOST_DETAIL, CancellationToken>((x, _) => detail = x).Returns(Task.CompletedTask);
        _docs.Setup(d => d.AddAsync(It.IsAny<TB_PC_EXPENSE_DOC>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_EXPENSE_DOC, CancellationToken>((x, _) => doc = x).Returns(Task.CompletedTask);

        var id = await Create().Handle(CreateCmd(), default);

        Assert.Equal(id, doc!.ID);
        Assert.Equal(PettyCashDocState.Draft, doc.DOC_STATE);
        Assert.Null(doc.SUBMITTED_DATE);
        Assert.Equal("0101", doc.VAHEDCODE);
        Assert.Equal("custodian1", doc.ADDUSERID);
        Assert.Equal("00007", head!.CHARGEANDCOST_CODE);
        Assert.Equal(ChargeAndCostStatus.Temporary, head.STATUS);
        Assert.Equal(ChargeAndCostType.Cost, head.CHARGEANDCOST_TYPE);
        Assert.Equal(109m, detail!.CREDITOR);
        Assert.Equal(0m, detail.DEBTOR);
        Assert.Null(detail.REVOLVINGFUND_ID);
        Assert.Equal(head.ID, doc.CHARGEANDCOSTHEAD_ID);
        var ev = Assert.Single(_added);
        Assert.Equal(PettyCashDocAction.Create, ev.ACTION);
        Assert.Null(ev.FROM_STATE);
        Assert.Equal(PettyCashDocState.Draft, ev.TO_STATE);
        Assert.Equal("1.2.3.4", ev.CLIENT_IP);
        _rules.Verify(r => r.EnsureSubmittableAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        SaveOnce();
    }

    [Fact]
    public async Task Create_Submit_RunsRulesAndStampsNew()
    {
        TB_PC_EXPENSE_DOC? doc = null;
        _docs.Setup(d => d.AddAsync(It.IsAny<TB_PC_EXPENSE_DOC>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_EXPENSE_DOC, CancellationToken>((x, _) => doc = x).Returns(Task.CompletedTask);

        await Create().Handle(CreateCmd(submit: true), default);

        Assert.Equal(PettyCashDocState.New, doc!.DOC_STATE);
        Assert.NotNull(doc.SUBMITTED_DATE);
        _rules.Verify(r => r.EnsureSubmittableAsync(doc.ID, _fundId, 1000m, 500m, "14040101", "1404", 109m, "0101", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_SubmitRuleViolation_PropagatesAndDoesNotSave()
    {
        _rules.Setup(r => r.EnsureSubmittableAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("rule"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create().Handle(CreateCmd(submit: true), default));
        SaveNever();
    }

    [Fact]
    public async Task Create_LongVendorName_TruncatesLegacyPayToTo100()
    {
        TB_CHARGEANDCOST_DETAIL? detail = null;
        _charge.Setup(c => c.AddDetailAsync(It.IsAny<TB_CHARGEANDCOST_DETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHARGEANDCOST_DETAIL, CancellationToken>((x, _) => detail = x).Returns(Task.CompletedTask);
        await Create().Handle(CreateCmd(vendor: new string('ف', 150)), default);
        Assert.Equal(100, detail!.PAYTO!.Length);
    }

    private static CreatePettyCashExpenseDocCommand ValidCreate(bool submit = false) => new(
        Guid.NewGuid(), Guid.NewGuid(), "14040102", "1404", "v", null, null, "14040101", null, 100m, 9m, null, submit)
    { VahedCode = "0101" };

    [Fact]
    public void CreateValidator_Valid_Passes()
        => new CreatePettyCashExpenseDocCommandValidator().TestValidate(ValidCreate()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void CreateValidator_VatAboveBase_Fails()
        => new CreatePettyCashExpenseDocCommandValidator().TestValidate(ValidCreate() with { VatAmount = 101m })
            .ShouldHaveValidationErrorFor(x => x.VatAmount);

    [Fact]
    public void CreateValidator_InvoiceDateRequiredOnlyWhenSubmitting()
    {
        var v = new CreatePettyCashExpenseDocCommandValidator();
        v.TestValidate(ValidCreate(false) with { InvoiceDate = null }).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(ValidCreate(true) with { InvoiceDate = null }).ShouldHaveValidationErrorFor(x => x.InvoiceDate);
    }

    [Fact]
    public void CreateValidator_FieldRules()
    {
        var v = new CreatePettyCashExpenseDocCommandValidator();
        v.TestValidate(ValidCreate() with { FundId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.FundId);
        v.TestValidate(ValidCreate() with { ExpenseId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.ExpenseId);
        v.TestValidate(ValidCreate() with { Year = "140" }).ShouldHaveValidationErrorFor(x => x.Year);
        v.TestValidate(ValidCreate() with { RegisterDate = "1404" }).ShouldHaveValidationErrorFor(x => x.RegisterDate);
        v.TestValidate(ValidCreate() with { VendorName = "" }).ShouldHaveValidationErrorFor(x => x.VendorName);
        v.TestValidate(ValidCreate() with { VendorNationalId = "123456789012" }).ShouldHaveValidationErrorFor(x => x.VendorNationalId);
        v.TestValidate(ValidCreate() with { AmountBeforeTax = -1m, VatAmount = 0m }).ShouldHaveValidationErrorFor(x => x.AmountBeforeTax);
        v.TestValidate(ValidCreate() with { EvidenceType = (PettyCashEvidenceType)99 }).ShouldHaveValidationErrorFor(x => x.EvidenceType);
        v.TestValidate(ValidCreate() with { Description = new string('a', 201) }).ShouldHaveValidationErrorFor(x => x.Description);
    }

    // ---------- Update ----------

    private UpdatePettyCashExpenseDocCommandHandler Update() => new(_docs.Object, _charge.Object, _funds.Object, _expenses.Object,
        _events.Object, _eventsRead.Object, _uow.Object, _user.Object, _client.Object);

    private UpdatePettyCashExpenseDocCommand UpdateCmd(
        decimal amount = 100m, string? description = "شرح", string registerDate = "14040102", Guid? expenseId = null, string vendor = "فروشنده")
        => new(_docId, _fundId, expenseId ?? _expenseId, registerDate, "1404", vendor, "123", "A1", "14040101",
            PettyCashEvidenceType.Receipt, amount, 9m, description)
        { VahedCode = "0101" };

    [Fact]
    public async Task Update_DocMissing_ThrowsNotFound()
        => await Assert.ThrowsAsync<NotFoundException>(() => Update().Handle(UpdateCmd(), default));

    [Fact]
    public async Task Update_DocDeleted_ThrowsNotFound()
    {
        SetupDoc(PettyCashDocState.Draft, deleted: true);
        await Assert.ThrowsAsync<NotFoundException>(() => Update().Handle(UpdateCmd(), default));
    }

    [Theory]
    [InlineData(PettyCashDocState.New)]
    [InlineData(PettyCashDocState.PendingReview)]
    [InlineData(PettyCashDocState.Approved)]
    [InlineData(PettyCashDocState.Settled)]
    public async Task Update_NonEditableState_Throws(PettyCashDocState state)
    {
        SetupDoc(state);
        await Assert.ThrowsAsync<PettyCashDocNotEditableException>(() => Update().Handle(UpdateCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Update_FundMissing_ThrowsNotFound()
    {
        SetupDoc(PettyCashDocState.Draft);
        _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((TB_PC_FUND?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => Update().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_NotCustodian_Throws()
    {
        SetupDoc(PettyCashDocState.Draft);
        _fund.CUSTODIAN_USERID = "other";
        await Assert.ThrowsAsync<PettyCashNotCustodianException>(() => Update().Handle(UpdateCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Update_ExpenseMissing_ThrowsNotFound()
    {
        SetupDoc(PettyCashDocState.Draft);
        await Assert.ThrowsAsync<NotFoundException>(() => Update().Handle(UpdateCmd(expenseId: Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Update_Duplicate_ExcludesOwnDocAndThrows()
    {
        SetupDoc(PettyCashDocState.Draft);
        _docs.Setup(d => d.ExistsActiveDuplicateAsync("123", "A1", "0101", _docId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<PettyCashDuplicateExpenseDocException>(() => Update().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_Draft_OverwritesDocHeadAndDetail_AndLogsEvent()
    {
        var d = SetupDoc(PettyCashDocState.Draft);
        var (head, detail) = SetupLegacy(d);

        await Update().Handle(UpdateCmd(amount: 200m, description: "جدید", registerDate: "14040110", vendor: "فروشندهٔ جدید"), default);

        Assert.Equal(200m, d.AMOUNT_BEFORE_TAX);
        Assert.Equal("فروشندهٔ جدید", d.VENDOR_NAME);
        Assert.Equal("custodian1", d.CHANGEUSERID);
        Assert.Equal("14040110", head.CHARGEANDCOST_DATE);
        Assert.Equal("جدید", head.DESCRIPTION);
        Assert.Equal(209m, detail.CREDITOR);
        var ev = Assert.Single(_added);
        Assert.Equal(PettyCashDocAction.Update, ev.ACTION);
        Assert.Equal(PettyCashDocState.Draft, ev.FROM_STATE);
        Assert.Equal(PettyCashDocState.Draft, ev.TO_STATE);
        SaveOnce();
    }

    private void SetupLastReturn(string? reasons)
        => _eventsRead.Setup(e => e.GetLastReturnEventAsync(_docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashDocEventDto(Guid.NewGuid(), PettyCashDocAction.Return, PettyCashDocState.PendingReview,
                PettyCashDocState.Returned, null, reasons, "insp", DateTime.UtcNow, null));

    [Fact]
    public async Task Update_Returned_ChangingFieldOutsideAllowedReasons_ThrowsLocked()
    {
        var d = SetupDoc(PettyCashDocState.Returned);
        SetupLegacy(d);
        // دلیل: شرح نیازمند توضیح (4) — فقط description مجاز است، تغییر مبلغ ممنوع.
        SetupLastReturn("4");

        await Assert.ThrowsAsync<PettyCashReturnFieldLockedException>(() => Update().Handle(UpdateCmd(amount: 999m), default));
        Assert.Equal(100m, d.AMOUNT_BEFORE_TAX);
        SaveNever();
    }

    [Fact]
    public async Task Update_Returned_ChangingAllowedField_Succeeds()
    {
        var d = SetupDoc(PettyCashDocState.Returned);
        var (head, _) = SetupLegacy(d);
        SetupLastReturn("4");

        await Update().Handle(UpdateCmd(description: "توضیح کامل‌تر"), default);

        Assert.Equal("توضیح کامل‌تر", head.DESCRIPTION);
        Assert.Equal(PettyCashDocState.Returned, d.DOC_STATE);
        SaveOnce();
    }

    [Fact]
    public async Task Update_Returned_OtherReason_AllowsAnyChange()
    {
        var d = SetupDoc(PettyCashDocState.Returned);
        SetupLegacy(d);
        SetupLastReturn(((int)PettyCashReturnReason.Other).ToString());

        await Update().Handle(UpdateCmd(amount: 999m, vendor: "تغییر"), default);

        Assert.Equal(999m, d.AMOUNT_BEFORE_TAX);
    }

    [Fact]
    public async Task Update_Returned_NoReturnEvent_AllowsAnyChange()
    {
        var d = SetupDoc(PettyCashDocState.Returned);
        SetupLegacy(d);
        _eventsRead.Setup(e => e.GetLastReturnEventAsync(_docId, It.IsAny<CancellationToken>())).ReturnsAsync((PettyCashDocEventDto?)null);

        await Update().Handle(UpdateCmd(amount: 300m), default);

        Assert.Equal(300m, d.AMOUNT_BEFORE_TAX);
    }

    [Fact]
    public async Task Update_Returned_NoChange_PassesEvenWithRestrictiveReason()
    {
        var d = SetupDoc(PettyCashDocState.Returned);
        SetupLegacy(d);
        SetupLastReturn("1");

        await Update().Handle(UpdateCmd(), default);

        SaveOnce();
    }

    [Fact]
    public void UpdateValidator_Rules()
    {
        var v = new UpdatePettyCashExpenseDocCommandValidator();
        var ok = UpdateCmd();
        v.TestValidate(ok).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(ok with { Id = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.Id);
        v.TestValidate(ok with { VatAmount = 500m }).ShouldHaveValidationErrorFor(x => x.VatAmount);
        v.TestValidate(ok with { Year = "14" }).ShouldHaveValidationErrorFor(x => x.Year);
        v.TestValidate(ok with { InvoiceDate = "1404" }).ShouldHaveValidationErrorFor(x => x.InvoiceDate);
        v.TestValidate(ok with { InvoiceDate = null }).ShouldNotHaveAnyValidationErrors();
    }

    // ---------- Submit ----------

    private SubmitPettyCashExpenseDocCommandHandler Submit() => new(_docs.Object, _charge.Object, _funds.Object,
        _events.Object, _rules.Object, _uow.Object, _user.Object, _client.Object);

    private SubmitPettyCashExpenseDocCommand SubmitCmd() => new(_docId) { VahedCode = "0101" };

    [Fact]
    public async Task Submit_DocMissingOrDeleted_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Submit().Handle(SubmitCmd(), default));
        SetupDoc(PettyCashDocState.Draft, deleted: true);
        await Assert.ThrowsAsync<NotFoundException>(() => Submit().Handle(SubmitCmd(), default));
    }

    [Theory]
    [InlineData(PettyCashDocState.New)]
    [InlineData(PettyCashDocState.Approved)]
    [InlineData(PettyCashDocState.Settled)]
    public async Task Submit_NonEditableState_Throws(PettyCashDocState state)
    {
        SetupDoc(state);
        await Assert.ThrowsAsync<PettyCashDocNotEditableException>(() => Submit().Handle(SubmitCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Submit_NotCustodian_Throws()
    {
        SetupDoc(PettyCashDocState.Draft);
        _fund.CUSTODIAN_USERID = "other";
        await Assert.ThrowsAsync<PettyCashNotCustodianException>(() => Submit().Handle(SubmitCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Submit_InactiveFund_Throws()
    {
        SetupDoc(PettyCashDocState.Draft);
        _fund.IS_ACTIVE = false;
        await Assert.ThrowsAsync<PettyCashFundInactiveException>(() => Submit().Handle(SubmitCmd(), default));
    }

    [Fact]
    public async Task Submit_FundMissing_ThrowsNotFound()
    {
        SetupDoc(PettyCashDocState.Draft);
        _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((TB_PC_FUND?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => Submit().Handle(SubmitCmd(), default));
    }

    [Fact]
    public async Task Submit_RuleViolation_PropagatesAndStateUnchanged()
    {
        var d = SetupDoc(PettyCashDocState.Draft);
        _rules.Setup(r => r.EnsureSubmittableAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("rule"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Submit().Handle(SubmitCmd(), default));
        Assert.Equal(PettyCashDocState.Draft, d.DOC_STATE);
        SaveNever();
    }

    [Theory]
    [InlineData(PettyCashDocState.Draft)]
    [InlineData(PettyCashDocState.Returned)]
    public async Task Submit_Valid_MovesToNewExcludingOwnDocFromExposure(PettyCashDocState from)
    {
        var d = SetupDoc(from);
        var (head, _) = SetupLegacy(d);

        await Submit().Handle(SubmitCmd(), default);

        Assert.Equal(PettyCashDocState.New, d.DOC_STATE);
        Assert.NotNull(d.SUBMITTED_DATE);
        Assert.Equal(ChargeAndCostStatus.Temporary, head.STATUS);
        var ev = Assert.Single(_added);
        Assert.Equal(PettyCashDocAction.Submit, ev.ACTION);
        Assert.Equal(from, ev.FROM_STATE);
        Assert.Equal(PettyCashDocState.New, ev.TO_STATE);
        _rules.Verify(r => r.EnsureSubmittableAsync(_docId, _fundId, 1000m, 500m, "14040101", "1404", 109m, "0101", _docId, It.IsAny<CancellationToken>()), Times.Once);
        SaveOnce();
    }

    [Fact]
    public void SubmitValidator_EmptyId_Fails()
        => new SubmitPettyCashExpenseDocCommandValidator().TestValidate(new SubmitPettyCashExpenseDocCommand(Guid.Empty) { VahedCode = "0101" })
            .ShouldHaveValidationErrorFor(x => x.Id);

    // ---------- Delete ----------

    private DeletePettyCashExpenseDocCommandHandler Delete() => new(_docs.Object, _funds.Object, _charge.Object, _events.Object, _uow.Object, _user.Object, _client.Object);
    private DeletePettyCashExpenseDocCommand DelCmd() => new(_docId) { VahedCode = "0101" };

    [Fact]
    public async Task Delete_DocMissing_ThrowsNotFound()
        => await Assert.ThrowsAsync<NotFoundException>(() => Delete().Handle(DelCmd(), default));

    [Fact]
    public async Task Delete_FundMissing_ThrowsNotFound()
    {
        SetupDoc(PettyCashDocState.Draft);
        _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((TB_PC_FUND?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => Delete().Handle(DelCmd(), default));
    }

    [Fact]
    public async Task Delete_NotCustodian_Throws()
    {
        SetupDoc(PettyCashDocState.Draft);
        _fund.CUSTODIAN_USERID = "other";
        await Assert.ThrowsAsync<PettyCashNotCustodianException>(() => Delete().Handle(DelCmd(), default));
        SaveNever();
    }

    [Theory]
    [InlineData(PettyCashDocState.Returned)]
    [InlineData(PettyCashDocState.New)]
    [InlineData(PettyCashDocState.Approved)]
    public async Task Delete_NonDraft_Throws(PettyCashDocState state)
    {
        var d = SetupDoc(state);
        await Assert.ThrowsAsync<PettyCashDocNotEditableException>(() => Delete().Handle(DelCmd(), default));
        Assert.False(d.ISDELETED);
        SaveNever();
    }

    [Fact]
    public async Task Delete_NonDraftAlreadyDeleted_StillReportsStateConflict()
    {
        // ترتیب بررسی: قفل بودن وضعیت پیش از ایدمپوتنت‌بودن حذف.
        SetupDoc(PettyCashDocState.Approved, deleted: true);
        await Assert.ThrowsAsync<PettyCashDocNotEditableException>(() => Delete().Handle(DelCmd(), default));
    }

    [Fact]
    public async Task Delete_AlreadyDeletedDraft_IsNoOp()
    {
        SetupDoc(PettyCashDocState.Draft, deleted: true);
        await Delete().Handle(DelCmd(), default);
        SaveNever();
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Delete_Draft_SoftDeletesDocHeadDetailAndLogs()
    {
        var d = SetupDoc(PettyCashDocState.Draft);
        var (head, detail) = SetupLegacy(d);

        await Delete().Handle(DelCmd(), default);

        Assert.True(d.ISDELETED);
        Assert.True(head.ISDELETED);
        Assert.True(detail.ISDELETED);
        var ev = Assert.Single(_added);
        Assert.Equal(PettyCashDocAction.Delete, ev.ACTION);
        Assert.Equal(PettyCashDocState.Draft, ev.FROM_STATE);
        Assert.Null(ev.TO_STATE);
        Assert.Equal("1.2.3.4", ev.CLIENT_IP);
        SaveOnce();
    }
}
