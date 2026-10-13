using Accounting.Application.Commands.Treasury.CancelReceipt;
using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Commands.Treasury.CreateReceipt;
using Accounting.Application.Commands.Treasury.DeleteReceipt;
using Accounting.Application.Commands.Treasury.RegisterReceipt;
using Accounting.Application.Commands.Treasury.UpdateReceipt;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Application.Queries.Tafsilis;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.Receipts;

/// <summary>دریافت‌ها: ایجاد (با/بدون ثبت)، ویرایش، لغو، حذف نرم، ثبت و سرویس ثبت (سند + PayRecive).</summary>
public sealed class ReceiptHandlerTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid Payer = Guid.NewGuid();
    private static readonly Guid Bank = Guid.NewGuid();

    private readonly Mock<ITreasuryReceiptRepository> _receipts = new();
    private readonly Mock<IBankAccountReadRepository> _banks = new();
    private readonly Mock<IReceiptPayerValidator> _payer = new();
    private readonly Mock<IReceiptRegistrationService> _registration = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public ReceiptHandlerTests()
    {
        _user.SetupGet(u => u.UserId).Returns("u1");
        _banks.Setup(b => b.GetByIdAsync(Bank, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(BankDto());
        _payer.Setup(p => p.EnsurePayerValidAsync(Payer, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync("مشتری الف");
        _receipts.Setup(r => r.GetNextCodeAsync(Vahed, "1405", It.IsAny<CancellationToken>())).ReturnsAsync(12);
    }

    private static BankAccountDto BankDto() => new(
        Bank, "1", "h", null, null, null, null, null, null, Guid.NewGuid(), Vahed, null, null, null, null, null, false,
        new List<BankAccountTafsiliLinkDto>());

    private static CreateReceiptCommand CreateCmd(bool register = false) => new(
        Payer, 1000m, Bank, TreasuryPaymentMethod.Satna, "14050101", "REF1", "INV", "d", "1405", register) { VahedCode = Vahed };

    private static UpdateReceiptCommand UpdateCmd() => new(
        Id, Payer, 2000m, Bank, TreasuryPaymentMethod.Paya, "14050202", "REF2", null, "new") { VahedCode = Vahed };

    private TB_TR_RECEIPT Existing(ReceiptState state)
    {
        var r = new TB_TR_RECEIPT { ID = Id, STATE = state, VAHEDCODE = Vahed };
        _receipts.Setup(x => x.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(r);
        return r;
    }

    private CreateReceiptCommandHandler CreateSut()
        => new(_receipts.Object, _banks.Object, _payer.Object, _registration.Object, _uow.Object, _user.Object);

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_NoRegister_StagesDraftReceiptAndSavesOnly()
    {
        TB_TR_RECEIPT? added = null;
        _receipts.Setup(r => r.AddAsync(It.IsAny<TB_TR_RECEIPT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_RECEIPT, CancellationToken>((r, _) => added = r).Returns(Task.CompletedTask);

        var id = await CreateSut().Handle(CreateCmd(register: false), default);

        Assert.Equal(id, added!.ID);
        Assert.Equal("RCV-000012", added.CODE);
        Assert.Equal(ReceiptState.Draft, added.STATE);
        Assert.Equal("مشتری الف", added.PAYER_NAME);
        Assert.Equal(Vahed, added.VAHEDCODE);
        Assert.Equal("u1", added.ADDUSERID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _registration.Verify(s => s.RegisterAsync(It.IsAny<TB_TR_RECEIPT>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithRegister_RunsRegistrationInTransaction()
    {
        await CreateSut().Handle(CreateCmd(register: true), default);

        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _registration.Verify(s => s.RegisterAsync(It.IsAny<TB_TR_RECEIPT>(), Vahed, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithRegister_RegistrationFails_RollsBack()
    {
        _registration.Setup(s => s.RegisterAsync(It.IsAny<TB_TR_RECEIPT>(), Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryTreasurerRoleRequiredException());

        await Assert.ThrowsAsync<TreasuryTreasurerRoleRequiredException>(() => CreateSut().Handle(CreateCmd(register: true), default));

        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_BankAccountMissing_ThrowsNotFound()
    {
        _banks.Setup(b => b.GetByIdAsync(Bank, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().Handle(CreateCmd(), default));
    }

    [Fact]
    public async Task Create_DuplicateBankReference_Throws()
    {
        _receipts.Setup(r => r.ExistsDuplicateBankReferenceAsync(Bank, "REF1", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<TreasuryReceiptDuplicateBankReferenceException>(() => CreateSut().Handle(CreateCmd(), default));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_PayerInvalid_PropagatesException()
    {
        _payer.Setup(p => p.EnsurePayerValidAsync(Payer, Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryTafsiliNotInGroupException(Payer, Guid.NewGuid()));

        await Assert.ThrowsAsync<TreasuryTafsiliNotInGroupException>(() => CreateSut().Handle(CreateCmd(), default));
    }

    // ── Update ──────────────────────────────────────────────────────────────

    private UpdateReceiptCommandHandler UpdateSut()
        => new(_receipts.Object, _banks.Object, _payer.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task Update_Draft_AppliesFieldsAndExcludesSelfFromDuplicateCheck()
    {
        var r = Existing(ReceiptState.Draft);

        await UpdateSut().Handle(UpdateCmd(), default);

        Assert.Equal(2000m, r.AMOUNT);
        Assert.Equal("REF2", r.BANK_REFERENCE);
        Assert.Equal("مشتری الف", r.PAYER_NAME);
        Assert.Equal("u1", r.CHANGEUSERID);
        _receipts.Verify(x => x.ExistsDuplicateBankReferenceAsync(Bank, "REF2", Id, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ReceiptState.Registered)]
    [InlineData(ReceiptState.Cancelled)]
    public async Task Update_NotDraft_ThrowsStateConflict(ReceiptState state)
    {
        Existing(state);

        await Assert.ThrowsAsync<TreasuryReceiptStateConflictException>(() => UpdateSut().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_NotFoundOrDeleted_Throws()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => UpdateSut().Handle(UpdateCmd(), default));

        Existing(ReceiptState.Draft).ISDELETED = true;
        await Assert.ThrowsAsync<NotFoundException>(() => UpdateSut().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_Duplicate_Throws_AndEntityUntouched()
    {
        var r = Existing(ReceiptState.Draft);
        r.AMOUNT = 1m;
        _receipts.Setup(x => x.ExistsDuplicateBankReferenceAsync(Bank, "REF2", Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<TreasuryReceiptDuplicateBankReferenceException>(() => UpdateSut().Handle(UpdateCmd(), default));

        Assert.Equal(1m, r.AMOUNT);
    }

    // ── Cancel / Delete / Register ──────────────────────────────────────────

    [Fact]
    public async Task Cancel_Draft_SetsCancelled()
    {
        var r = Existing(ReceiptState.Draft);

        await new CancelReceiptCommandHandler(_receipts.Object, _uow.Object, _user.Object)
            .Handle(new CancelReceiptCommand(Id) { VahedCode = Vahed }, default);

        Assert.Equal(ReceiptState.Cancelled, r.STATE);
        Assert.Equal("u1", r.CHANGEUSERID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ReceiptState.Registered)]
    [InlineData(ReceiptState.Cancelled)]
    public async Task Cancel_NotDraft_Throws(ReceiptState state)
    {
        Existing(state);

        await Assert.ThrowsAsync<TreasuryReceiptStateConflictException>(
            () => new CancelReceiptCommandHandler(_receipts.Object, _uow.Object, _user.Object)
                .Handle(new CancelReceiptCommand(Id) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Cancel_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(
            () => new CancelReceiptCommandHandler(_receipts.Object, _uow.Object, _user.Object)
                .Handle(new CancelReceiptCommand(Id) { VahedCode = Vahed }, default));

    [Fact]
    public async Task Delete_Draft_SoftDeletes()
    {
        var r = Existing(ReceiptState.Draft);

        await new DeleteReceiptCommandHandler(_receipts.Object, _uow.Object, _user.Object)
            .Handle(new DeleteReceiptCommand(Id) { VahedCode = Vahed }, default);

        Assert.True(r.ISDELETED);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Registered_Throws_AndNotDeleted()
    {
        var r = Existing(ReceiptState.Registered);

        await Assert.ThrowsAsync<TreasuryReceiptStateConflictException>(
            () => new DeleteReceiptCommandHandler(_receipts.Object, _uow.Object, _user.Object)
                .Handle(new DeleteReceiptCommand(Id) { VahedCode = Vahed }, default));

        Assert.False(r.ISDELETED);
    }

    [Fact]
    public async Task Delete_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(
            () => new DeleteReceiptCommandHandler(_receipts.Object, _uow.Object, _user.Object)
                .Handle(new DeleteReceiptCommand(Id) { VahedCode = Vahed }, default));

    [Fact]
    public async Task Register_Existing_RegistersInsideTransaction()
    {
        var r = Existing(ReceiptState.Draft);

        await new RegisterReceiptCommandHandler(_receipts.Object, _registration.Object, _uow.Object)
            .Handle(new RegisterReceiptCommand(Id) { VahedCode = Vahed }, default);

        _registration.Verify(s => s.RegisterAsync(r, Vahed, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_NotFound_ThrowsBeforeTransaction()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => new RegisterReceiptCommandHandler(_receipts.Object, _registration.Object, _uow.Object)
                .Handle(new RegisterReceiptCommand(Id) { VahedCode = Vahed }, default));

        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_ServiceThrows_RollsBack()
    {
        Existing(ReceiptState.Draft);
        _registration.Setup(s => s.RegisterAsync(It.IsAny<TB_TR_RECEIPT>(), Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryReceiptStateConflictException(Id, ReceiptState.Registered, "پیش‌نویس"));

        await Assert.ThrowsAsync<TreasuryReceiptStateConflictException>(
            () => new RegisterReceiptCommandHandler(_receipts.Object, _registration.Object, _uow.Object)
                .Handle(new RegisterReceiptCommand(Id) { VahedCode = Vahed }, default));

        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── ReceiptRegistrationService ──────────────────────────────────────────

    [Fact]
    public async Task RegistrationService_NotDraft_Throws()
    {
        var svc = NewRegistrationService(out _, out _, out _);

        await Assert.ThrowsAsync<TreasuryReceiptStateConflictException>(
            () => svc.RegisterAsync(new TB_TR_RECEIPT { ID = Id, STATE = ReceiptState.Registered }, Vahed));
    }

    [Fact]
    public async Task RegistrationService_NotTreasurer_Throws_BeforeVoucher()
    {
        var svc = NewRegistrationService(out var treasurer, out var voucher, out _);
        treasurer.Setup(t => t.EnsureTreasurerAsync(Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryTreasurerRoleRequiredException());

        await Assert.ThrowsAsync<TreasuryTreasurerRoleRequiredException>(
            () => svc.RegisterAsync(new TB_TR_RECEIPT { ID = Id, STATE = ReceiptState.Draft }, Vahed));

        voucher.Verify(v => v.BuildAndStageAsync(It.IsAny<TB_TR_RECEIPT>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrationService_Valid_MarksRegisteredAndMirrorsLinesIntoPayRecive()
    {
        var svc = NewRegistrationService(out _, out var voucher, out var payRecive);
        var voucherId = Guid.NewGuid();
        var tafsili = new Accounting.Application.Commands.Vouchers.Common.VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid());
        voucher.Setup(v => v.BuildAndStageAsync(It.IsAny<TB_TR_RECEIPT>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRequestVoucherBuildResult(voucherId, "9", "14050101", "1405", new[]
            {
                new PaymentRequestVoucherLineResult(Guid.NewGuid(), 1000m, 0m, new[] { tafsili }),
                new PaymentRequestVoucherLineResult(Guid.NewGuid(), 0m, 1000m, Array.Empty<Accounting.Application.Commands.Vouchers.Common.VoucherDetailTafsiliLinkInput>()),
            }));
        payRecive.Setup(p => p.GetNextCodeAsync(Vahed, "1405", It.IsAny<CancellationToken>())).ReturnsAsync(3);
        var details = new List<TB_PAYRECIVDETAIL>();
        TB_PAYRECIVHEAD? head = null;
        payRecive.Setup(p => p.AddAsync(It.IsAny<TB_PAYRECIVHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PAYRECIVHEAD, CancellationToken>((h, _) => head = h).Returns(Task.CompletedTask);
        payRecive.Setup(p => p.AddDetailAsync(It.IsAny<TB_PAYRECIVDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PAYRECIVDETAIL, CancellationToken>((d, _) => details.Add(d)).Returns(Task.CompletedTask);
        var receipt = new TB_TR_RECEIPT { ID = Id, STATE = ReceiptState.Draft, CODE = "RCV-1", PAYER_NAME = "x", RECEIPT_DATE = "14050101" };

        await svc.RegisterAsync(receipt, Vahed);

        Assert.Equal(ReceiptState.Registered, receipt.STATE);
        Assert.Equal(voucherId, receipt.VOUCHER_ID);
        Assert.Equal(head!.ID, receipt.PAYRECIVHEAD_ID);
        Assert.Equal("u1", receipt.REGISTERED_BY);
        Assert.Equal("00003", head.PAYRECIVCODE);
        Assert.Equal(PayRecivType.Recive, head.PAYRECIVTYPE);
        Assert.Equal(new[] { 1, 2 }, details.Select(d => d.RADIF).ToArray());
        Assert.Equal(1000m, details[0].DEBTOR);
        Assert.Equal(1000m, details[1].CREDITOR);
        payRecive.Verify(p => p.AddDetailTafsiliLinkAsync(It.IsAny<TB_PAYRECIVDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private ReceiptRegistrationService NewRegistrationService(
        out Mock<ITreasuryTreasurerAuthorizer> treasurer, out Mock<IReceiptVoucherBuilder> voucher, out Mock<IPayReciveHeadRepository> payRecive)
    {
        treasurer = new Mock<ITreasuryTreasurerAuthorizer>();
        voucher = new Mock<IReceiptVoucherBuilder>();
        payRecive = new Mock<IPayReciveHeadRepository>();
        return new ReceiptRegistrationService(treasurer.Object, voucher.Object, payRecive.Object, _user.Object);
    }

    // ── ReceiptPayerValidator ───────────────────────────────────────────────

    [Fact]
    public async Task PayerValidator_NoCustomerGroup_ThrowsSettingMissing()
    {
        var settings = new Mock<ITreasurySettingReadRepository>();
        settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(TreasuryTestData.Setting());
        var sut = new ReceiptPayerValidator(settings.Object, Mock.Of<ITreasuryBeneficiaryTafsiliReadRepository>(), Mock.Of<ITafsiliReadRepository>());

        await Assert.ThrowsAsync<TreasurySettingValueMissingException>(() => sut.EnsurePayerValidAsync(Payer, Vahed));
    }

    [Fact]
    public async Task PayerValidator_NotMember_Throws()
    {
        var groupId = Guid.NewGuid();
        var settings = new Mock<ITreasurySettingReadRepository>();
        settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TreasuryTestData.Setting() with { CustomerTafsilGroupId = groupId });
        var groups = new Mock<ITreasuryBeneficiaryTafsiliReadRepository>();
        groups.Setup(g => g.IsMemberOfGroupAsync(Payer, groupId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = new ReceiptPayerValidator(settings.Object, groups.Object, Mock.Of<ITafsiliReadRepository>());

        await Assert.ThrowsAsync<TreasuryTafsiliNotInGroupException>(() => sut.EnsurePayerValidAsync(Payer, Vahed));
    }

    [Fact]
    public async Task PayerValidator_Member_ReturnsTafsiliName()
    {
        var groupId = Guid.NewGuid();
        var settings = new Mock<ITreasurySettingReadRepository>();
        settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TreasuryTestData.Setting() with { CustomerTafsilGroupId = groupId });
        var groups = new Mock<ITreasuryBeneficiaryTafsiliReadRepository>();
        groups.Setup(g => g.IsMemberOfGroupAsync(Payer, groupId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var tafsilis = new Mock<ITafsiliReadRepository>();
        tafsilis.Setup(t => t.GetByIdAsync(Payer, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TafsiliDto(Payer, "1", "علی", null, null, null, null, null, Vahed, Array.Empty<Guid>(), null, null, null, null, false));
        var sut = new ReceiptPayerValidator(settings.Object, groups.Object, tafsilis.Object);

        Assert.Equal("علی", await sut.EnsurePayerValidAsync(Payer, Vahed));
    }

    [Fact]
    public async Task PayerValidator_TafsiliMissing_ThrowsNotFound()
    {
        var groupId = Guid.NewGuid();
        var settings = new Mock<ITreasurySettingReadRepository>();
        settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TreasuryTestData.Setting() with { CustomerTafsilGroupId = groupId });
        var groups = new Mock<ITreasuryBeneficiaryTafsiliReadRepository>();
        groups.Setup(g => g.IsMemberOfGroupAsync(Payer, groupId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = new ReceiptPayerValidator(settings.Object, groups.Object, Mock.Of<ITafsiliReadRepository>());

        await Assert.ThrowsAsync<NotFoundException>(() => sut.EnsurePayerValidAsync(Payer, Vahed));
    }

    // ── Validators ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateValidator_Valid_Passes()
        => Assert.True(new CreateReceiptCommandValidator().Validate(CreateCmd()).IsValid);

    [Fact]
    public void CreateValidator_RuleViolations_Fail()
    {
        var v = new CreateReceiptCommandValidator();
        Assert.False(v.Validate(CreateCmd() with { Amount = 0 }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { PayerTafsiliId = Guid.Empty }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { BankAccountId = Guid.Empty }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { ReceiptDate = "1405/01/01" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { BankReference = "" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { BankReference = new string('x', 101) }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { Year = "99" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { Description = new string('x', 1001) }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { ReceiptMethod = (TreasuryPaymentMethod)99 }).IsValid);
    }

    [Fact]
    public void UpdateValidator_ValidPasses_EmptyIdAndBadAmountFail()
    {
        var v = new UpdateReceiptCommandValidator();
        Assert.True(v.Validate(UpdateCmd()).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { Id = Guid.Empty }).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { Amount = -1 }).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { VahedCode = "" }).IsValid);
    }
}
