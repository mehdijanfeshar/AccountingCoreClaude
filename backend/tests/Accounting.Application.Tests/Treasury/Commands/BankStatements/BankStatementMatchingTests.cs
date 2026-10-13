using Accounting.Application.Commands.Treasury.AutoMatchBankStatement;
using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Commands.Treasury.MatchBankStatementLine;
using Accounting.Application.Commands.Treasury.ResolveBankStatementLine;
using Accounting.Application.Commands.Treasury.UnmatchBankStatementLine;
using Accounting.Application.Commands.Treasury.UnresolveBankStatementLine;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.BankStatements;

/// <summary>تطبیق دستی/خودکار و «حل» ردیف‌های صورت‌حساب بانکی — handlerها و سرویس‌های تطبیق/حل.</summary>
public sealed class BankStatementMatchingTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid LineId = Guid.NewGuid();
    private static readonly Guid BankId = Guid.NewGuid();
    private static readonly Guid BankAccountCode = Guid.NewGuid();

    private readonly Mock<ITreasuryBankStatementRepository> _statements = new();
    private readonly Mock<ITreasuryBankStatementLineRepository> _lines = new();
    private readonly Mock<ITreasuryRoleAuthorizer> _roles = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public BankStatementMatchingTests()
    {
        _user.SetupGet(u => u.UserId).Returns("acc1");
    }

    private TB_TR_BANK_STATEMENT Statement(BankStatementState state = BankStatementState.Open)
    {
        var s = new TB_TR_BANK_STATEMENT { ID = Id, STATE = state, BANK_ACCOUNT_ID = BankId, YEAR = "1405" };
        _statements.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(s);
        return s;
    }

    private TB_TR_BANK_STATEMENT_LINE Line(BankStatementLineMatchState state = BankStatementLineMatchState.Unmatched, decimal deposit = 0m, decimal withdrawal = 0m)
    {
        var l = new TB_TR_BANK_STATEMENT_LINE
        {
            ID = LineId, STATEMENT_ID = Id, MATCH_STATE = state, DEPOSIT = deposit, WITHDRAWAL = withdrawal,
            VAHEDCODE = Vahed, LINE_DATE = "14050110",
        };
        _lines.Setup(r => r.GetForUpdateAsync(LineId, Id, It.IsAny<CancellationToken>())).ReturnsAsync(l);
        return l;
    }

    private void DenyRole()
        => _roles.Setup(r => r.EnsureHasRoleAsync(Vahed, It.IsAny<IReadOnlyCollection<TreasuryRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryRoleRequiredException(Vahed, TreasuryRole.Treasurer));

    // ── Handlers: Match / Unmatch ───────────────────────────────────────────

    [Fact]
    public async Task MatchHandler_Valid_DelegatesToServiceAndSaves()
    {
        var statement = Statement();
        var line = Line();
        var service = new Mock<IBankStatementManualMatchService>();
        var detailId = Guid.NewGuid();

        await new MatchBankStatementLineCommandHandler(_statements.Object, _lines.Object, service.Object, _roles.Object, _uow.Object)
            .Handle(new MatchBankStatementLineCommand(Id, LineId, detailId) { VahedCode = Vahed }, default);

        service.Verify(s => s.MatchAsync(statement, line, detailId, Vahed, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MatchHandler_ClosedStatement_ThrowsBeforeService()
    {
        Statement(BankStatementState.Closed);
        var service = new Mock<IBankStatementManualMatchService>();

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(
            () => new MatchBankStatementLineCommandHandler(_statements.Object, _lines.Object, service.Object, _roles.Object, _uow.Object)
                .Handle(new MatchBankStatementLineCommand(Id, LineId, Guid.NewGuid()) { VahedCode = Vahed }, default));

        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MatchHandler_RoleDeniedOrMissingEntities_Throw()
    {
        var service = new Mock<IBankStatementManualMatchService>();
        var sut = new MatchBankStatementLineCommandHandler(_statements.Object, _lines.Object, service.Object, _roles.Object, _uow.Object);
        var cmd = new MatchBankStatementLineCommand(Id, LineId, Guid.NewGuid()) { VahedCode = Vahed };

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, default));   // statement missing

        Statement();
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, default));   // line missing

        DenyRole();
        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(() => sut.Handle(cmd, default));
    }

    [Fact]
    public async Task UnmatchHandler_Valid_DelegatesToService()
    {
        Statement();
        var line = Line(BankStatementLineMatchState.ManualMatched);
        var service = new Mock<IBankStatementManualMatchService>();

        await new UnmatchBankStatementLineCommandHandler(_statements.Object, _lines.Object, service.Object, _roles.Object, _uow.Object)
            .Handle(new UnmatchBankStatementLineCommand(Id, LineId) { VahedCode = Vahed }, default);

        service.Verify(s => s.UnmatchAsync(line, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnmatchHandler_ClosedStatement_Throws()
    {
        Statement(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(
            () => new UnmatchBankStatementLineCommandHandler(_statements.Object, _lines.Object, Mock.Of<IBankStatementManualMatchService>(), _roles.Object, _uow.Object)
                .Handle(new UnmatchBankStatementLineCommand(Id, LineId) { VahedCode = Vahed }, default));
    }

    // ── Handlers: Resolve / Unresolve ───────────────────────────────────────

    [Fact]
    public async Task ResolveHandler_Valid_RunsInTransaction()
    {
        var statement = Statement();
        var line = Line();
        var service = new Mock<IBankStatementLineResolutionService>();
        var receipt = Guid.NewGuid();

        await new ResolveBankStatementLineCommandHandler(_statements.Object, _lines.Object, service.Object, _roles.Object, _uow.Object)
            .Handle(new ResolveBankStatementLineCommand(Id, LineId, BankStatementLineResolutionType.LinkedReceipt, receipt, "n") { VahedCode = Vahed }, default);

        service.Verify(s => s.ResolveAsync(statement, line, BankStatementLineResolutionType.LinkedReceipt, receipt, "n", Vahed, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveHandler_ServiceThrows_RollsBack()
    {
        Statement();
        Line();
        var service = new Mock<IBankStatementLineResolutionService>();
        service.Setup(s => s.ResolveAsync(It.IsAny<TB_TR_BANK_STATEMENT>(), It.IsAny<TB_TR_BANK_STATEMENT_LINE>(), It.IsAny<BankStatementLineResolutionType>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryBankStatementResolutionInvalidException("x"));

        await Assert.ThrowsAsync<TreasuryBankStatementResolutionInvalidException>(
            () => new ResolveBankStatementLineCommandHandler(_statements.Object, _lines.Object, service.Object, _roles.Object, _uow.Object)
                .Handle(new ResolveBankStatementLineCommand(Id, LineId, BankStatementLineResolutionType.Ignored, null, "n") { VahedCode = Vahed }, default));

        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnresolveHandler_Valid_RunsInTransaction()
    {
        Statement();
        var line = Line(BankStatementLineMatchState.Resolved);
        var service = new Mock<IBankStatementLineResolutionService>();

        await new UnresolveBankStatementLineCommandHandler(_statements.Object, _lines.Object, service.Object, _roles.Object, _uow.Object)
            .Handle(new UnresolveBankStatementLineCommand(Id, LineId) { VahedCode = Vahed }, default);

        service.Verify(s => s.UnresolveAsync(line, Vahed, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnresolveHandler_ClosedStatement_ThrowsBeforeTransaction()
    {
        Statement(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(
            () => new UnresolveBankStatementLineCommandHandler(_statements.Object, _lines.Object, Mock.Of<IBankStatementLineResolutionService>(), _roles.Object, _uow.Object)
                .Handle(new UnresolveBankStatementLineCommand(Id, LineId) { VahedCode = Vahed }, default));

        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveHandler_LineMissing_ThrowsNotFound()
    {
        Statement();

        await Assert.ThrowsAsync<NotFoundException>(
            () => new ResolveBankStatementLineCommandHandler(_statements.Object, _lines.Object, Mock.Of<IBankStatementLineResolutionService>(), _roles.Object, _uow.Object)
                .Handle(new ResolveBankStatementLineCommand(Id, LineId, BankStatementLineResolutionType.Ignored, null, "n") { VahedCode = Vahed }, default));
    }

    // ── AutoMatch handler ───────────────────────────────────────────────────

    [Fact]
    public async Task AutoMatchHandler_Valid_ReturnsServiceResultAndSaves()
    {
        var statement = Statement();
        var service = new Mock<IBankStatementAutoMatchService>();
        service.Setup(s => s.AutoMatchAsync(statement, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new BankStatementAutoMatchResult(3, 2));

        var result = await new AutoMatchBankStatementCommandHandler(_statements.Object, service.Object, _roles.Object, _uow.Object)
            .Handle(new AutoMatchBankStatementCommand(Id) { VahedCode = Vahed }, default);

        Assert.Equal(3, result.MatchedCount);
        Assert.Equal(2, result.UnmatchedCount);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoMatchHandler_ClosedStatement_Throws()
    {
        Statement(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(
            () => new AutoMatchBankStatementCommandHandler(_statements.Object, Mock.Of<IBankStatementAutoMatchService>(), _roles.Object, _uow.Object)
                .Handle(new AutoMatchBankStatementCommand(Id) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task AutoMatchHandler_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(
            () => new AutoMatchBankStatementCommandHandler(_statements.Object, Mock.Of<IBankStatementAutoMatchService>(), _roles.Object, _uow.Object)
                .Handle(new AutoMatchBankStatementCommand(Id) { VahedCode = Vahed }, default));

    // ── BankStatementManualMatchService ─────────────────────────────────────

    private readonly Mock<IVoucherDetailRepository> _details = new();
    private readonly Mock<IBankAccountReadRepository> _banks = new();

    private BankStatementManualMatchService MatchService()
    {
        _banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(
            new BankAccountDto(BankId, "1", "h", null, null, null, null, null, null, BankAccountCode, Vahed, null, null, null, null, null, false,
                new List<BankAccountTafsiliLinkDto>()));
        _lines.Setup(l => l.GetMatchedVoucherDetailIdsAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid>());
        return new BankStatementManualMatchService(_details.Object, _banks.Object, _lines.Object, _user.Object);
    }

    private TB_VOUCHERSDETAIL Detail(Guid? account = null, decimal? debtor = null, decimal? creditor = null)
    {
        var d = new TB_VOUCHERSDETAIL { ID = Guid.NewGuid(), ACCOUNT_ID = account ?? BankAccountCode, DEBTOR = debtor, CREDITOR = creditor };
        _details.Setup(r => r.GetForUpdateAsync(d.ID, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(d);
        return d;
    }

    [Fact]
    public async Task MatchService_DepositMatchesDebitLine_SetsManualMatched()
    {
        var statement = Statement();
        var line = Line(deposit: 500m);
        var detail = Detail(debtor: 500m);

        await MatchService().MatchAsync(statement, line, detail.ID, Vahed);

        Assert.Equal(BankStatementLineMatchState.ManualMatched, line.MATCH_STATE);
        Assert.Equal(detail.ID, line.MATCHED_VOUCHERDETAIL_ID);
        Assert.Equal("acc1", line.CHANGEUSERID);
    }

    [Fact]
    public async Task MatchService_WithdrawalMatchesCreditLine()
    {
        var statement = Statement();
        var line = Line(withdrawal: 300m);
        var detail = Detail(creditor: 300m);

        await MatchService().MatchAsync(statement, line, detail.ID, Vahed);

        Assert.Equal(BankStatementLineMatchState.ManualMatched, line.MATCH_STATE);
    }

    [Fact]
    public async Task MatchService_AlreadyMatchedLine_Throws()
    {
        var statement = Statement();
        var line = Line(BankStatementLineMatchState.AutoMatched, deposit: 5m);

        await Assert.ThrowsAsync<TreasuryBankStatementLineStateConflictException>(
            () => MatchService().MatchAsync(statement, line, Guid.NewGuid(), Vahed));
    }

    [Fact]
    public async Task MatchService_DetailMissing_ThrowsNotFound()
    {
        var statement = Statement();
        var line = Line(deposit: 5m);

        await Assert.ThrowsAsync<NotFoundException>(() => MatchService().MatchAsync(statement, line, Guid.NewGuid(), Vahed));
    }

    [Fact]
    public async Task MatchService_DetailOnOtherAccount_ThrowsMismatch()
    {
        var statement = Statement();
        var line = Line(deposit: 500m);
        var detail = Detail(account: Guid.NewGuid(), debtor: 500m);

        await Assert.ThrowsAsync<TreasuryBankStatementMatchMismatchException>(() => MatchService().MatchAsync(statement, line, detail.ID, Vahed));
    }

    [Fact]
    public async Task MatchService_DepositAgainstCreditLine_ThrowsMismatch()
    {
        var statement = Statement();
        var line = Line(deposit: 500m);
        var detail = Detail(creditor: 500m);

        await Assert.ThrowsAsync<TreasuryBankStatementMatchMismatchException>(() => MatchService().MatchAsync(statement, line, detail.ID, Vahed));
    }

    [Fact]
    public async Task MatchService_AmountDiffers_ThrowsMismatch()
    {
        var statement = Statement();
        var line = Line(deposit: 500m);
        var detail = Detail(debtor: 499m);

        await Assert.ThrowsAsync<TreasuryBankStatementMatchMismatchException>(() => MatchService().MatchAsync(statement, line, detail.ID, Vahed));
    }

    [Fact]
    public async Task MatchService_DetailAlreadyClaimedByAnotherLine_ThrowsMismatch()
    {
        var statement = Statement();
        var line = Line(deposit: 500m);
        var detail = Detail(debtor: 500m);
        var svc = MatchService();
        _lines.Setup(l => l.GetMatchedVoucherDetailIdsAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid> { detail.ID });

        await Assert.ThrowsAsync<TreasuryBankStatementMatchMismatchException>(() => svc.MatchAsync(statement, line, detail.ID, Vahed));

        Assert.Equal(BankStatementLineMatchState.Unmatched, line.MATCH_STATE);
    }

    [Fact]
    public async Task MatchService_Unmatch_ResetsLine()
    {
        var line = Line(BankStatementLineMatchState.ManualMatched);
        line.MATCHED_VOUCHERDETAIL_ID = Guid.NewGuid();

        await MatchService().UnmatchAsync(line);

        Assert.Equal(BankStatementLineMatchState.Unmatched, line.MATCH_STATE);
        Assert.Null(line.MATCHED_VOUCHERDETAIL_ID);
    }

    [Theory]
    [InlineData(BankStatementLineMatchState.Unmatched)]
    [InlineData(BankStatementLineMatchState.Resolved)]
    public async Task MatchService_Unmatch_WrongState_Throws(BankStatementLineMatchState state)
    {
        var line = Line(state);

        await Assert.ThrowsAsync<TreasuryBankStatementLineStateConflictException>(() => MatchService().UnmatchAsync(line));
    }

    // ── BankStatementLineResolutionService ──────────────────────────────────

    private readonly Mock<IBankFeeVoucherBuilder> _feeBuilder = new();
    private readonly Mock<ITreasuryReceiptRepository> _receipts = new();
    private readonly Mock<IVoucherHeadRepository> _heads = new();

    private BankStatementLineResolutionService ResolutionService()
        => new(_feeBuilder.Object, _receipts.Object, _heads.Object, _user.Object);

    [Fact]
    public async Task Resolve_BankFee_OnWithdrawal_StagesVoucherAndResolves()
    {
        var statement = Statement();
        var line = Line(withdrawal: 100m);
        var voucherId = Guid.NewGuid();
        _feeBuilder.Setup(b => b.BuildAndStageAsync(line, BankId, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRequestVoucherBuildResult(voucherId, "1", "14050110", "1405", Array.Empty<PaymentRequestVoucherLineResult>()));

        await ResolutionService().ResolveAsync(statement, line, BankStatementLineResolutionType.BankFeeVoucher, null, "fee", Vahed);

        Assert.Equal(BankStatementLineMatchState.Resolved, line.MATCH_STATE);
        Assert.Equal(voucherId, line.RESOLUTION_VOUCHER_ID);
        Assert.Equal(BankStatementLineResolutionType.BankFeeVoucher, line.RESOLUTION_TYPE);
        Assert.Equal("fee", line.RESOLUTION_NOTE);
    }

    [Fact]
    public async Task Resolve_BankFee_OnDeposit_ThrowsInvalid()
    {
        var statement = Statement();
        var line = Line(deposit: 100m);

        await Assert.ThrowsAsync<TreasuryBankStatementResolutionInvalidException>(
            () => ResolutionService().ResolveAsync(statement, line, BankStatementLineResolutionType.BankFeeVoucher, null, null, Vahed));
    }

    [Fact]
    public async Task Resolve_AlreadyMatchedLine_ThrowsStateConflict()
    {
        var statement = Statement();
        var line = Line(BankStatementLineMatchState.AutoMatched, withdrawal: 1m);

        await Assert.ThrowsAsync<TreasuryBankStatementLineStateConflictException>(
            () => ResolutionService().ResolveAsync(statement, line, BankStatementLineResolutionType.Ignored, null, "n", Vahed));
    }

    private TB_TR_RECEIPT Receipt(ReceiptState state = ReceiptState.Registered, decimal amount = 100m, Guid? bank = null)
    {
        var r = new TB_TR_RECEIPT { ID = Guid.NewGuid(), STATE = state, AMOUNT = amount, BANK_ACCOUNT_ID = bank ?? BankId };
        _receipts.Setup(x => x.GetForUpdateAsync(r.ID, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(r);
        return r;
    }

    [Fact]
    public async Task Resolve_LinkedReceipt_Valid_LinksReceipt()
    {
        var statement = Statement();
        var line = Line(deposit: 100m);
        var receipt = Receipt();

        await ResolutionService().ResolveAsync(statement, line, BankStatementLineResolutionType.LinkedReceipt, receipt.ID, null, Vahed);

        Assert.Equal(receipt.ID, line.RESOLUTION_RECEIPT_ID);
        Assert.Equal(BankStatementLineMatchState.Resolved, line.MATCH_STATE);
    }

    [Fact]
    public async Task Resolve_LinkedReceipt_Invalid_Variants_Throw()
    {
        var statement = Statement();

        // ردیف برداشتی
        await Assert.ThrowsAsync<TreasuryBankStatementResolutionInvalidException>(() => ResolutionService().ResolveAsync(
            statement, Line(withdrawal: 100m), BankStatementLineResolutionType.LinkedReceipt, Receipt().ID, null, Vahed));

        // بدون شناسهٔ دریافت
        await Assert.ThrowsAsync<TreasuryBankStatementResolutionInvalidException>(() => ResolutionService().ResolveAsync(
            statement, Line(deposit: 100m), BankStatementLineResolutionType.LinkedReceipt, null, null, Vahed));

        // دریافت ثبت‌نشده
        await Assert.ThrowsAsync<TreasuryBankStatementResolutionInvalidException>(() => ResolutionService().ResolveAsync(
            statement, Line(deposit: 100m), BankStatementLineResolutionType.LinkedReceipt, Receipt(ReceiptState.Draft).ID, null, Vahed));

        // حساب بانکی دیگر
        await Assert.ThrowsAsync<TreasuryBankStatementResolutionInvalidException>(() => ResolutionService().ResolveAsync(
            statement, Line(deposit: 100m), BankStatementLineResolutionType.LinkedReceipt, Receipt(bank: Guid.NewGuid()).ID, null, Vahed));

        // مبلغ نابرابر
        await Assert.ThrowsAsync<TreasuryBankStatementResolutionInvalidException>(() => ResolutionService().ResolveAsync(
            statement, Line(deposit: 100m), BankStatementLineResolutionType.LinkedReceipt, Receipt(amount: 99m).ID, null, Vahed));

        // دریافت وجود ندارد
        await Assert.ThrowsAsync<NotFoundException>(() => ResolutionService().ResolveAsync(
            statement, Line(deposit: 100m), BankStatementLineResolutionType.LinkedReceipt, Guid.NewGuid(), null, Vahed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Resolve_Ignored_WithoutNote_Throws(string? note)
    {
        var statement = Statement();
        var line = Line(deposit: 5m);

        await Assert.ThrowsAsync<TreasuryBankStatementResolutionInvalidException>(
            () => ResolutionService().ResolveAsync(statement, line, BankStatementLineResolutionType.Ignored, null, note, Vahed));
    }

    [Fact]
    public async Task Resolve_Ignored_WithNote_Resolves()
    {
        var statement = Statement();
        var line = Line(deposit: 5m);

        await ResolutionService().ResolveAsync(statement, line, BankStatementLineResolutionType.Ignored, null, "تکراری", Vahed);

        Assert.Equal(BankStatementLineMatchState.Resolved, line.MATCH_STATE);
    }

    [Fact]
    public async Task Resolve_UnknownType_ThrowsInvalid()
    {
        var statement = Statement();
        var line = Line(deposit: 5m);

        await Assert.ThrowsAsync<TreasuryBankStatementResolutionInvalidException>(
            () => ResolutionService().ResolveAsync(statement, line, (BankStatementLineResolutionType)99, null, "n", Vahed));
    }

    [Fact]
    public async Task Unresolve_NotResolved_Throws()
    {
        var line = Line(BankStatementLineMatchState.Unmatched);

        await Assert.ThrowsAsync<TreasuryBankStatementLineStateConflictException>(() => ResolutionService().UnresolveAsync(line, Vahed));
    }

    [Fact]
    public async Task Unresolve_BankFee_TemporaryVoucher_SoftDeletesVoucherAndClearsLine()
    {
        var voucherId = Guid.NewGuid();
        var voucher = new TB_VOUCHERSHEAD { ID = voucherId, DOCLIFE = DocLife.Temporary };
        _heads.Setup(h => h.GetForUpdateAsync(voucherId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(voucher);
        var line = Line(BankStatementLineMatchState.Resolved);
        line.RESOLUTION_TYPE = BankStatementLineResolutionType.BankFeeVoucher;
        line.RESOLUTION_VOUCHER_ID = voucherId;

        await ResolutionService().UnresolveAsync(line, Vahed);

        Assert.True(voucher.ISDELETED);
        _heads.Verify(h => h.SoftDeleteDetailTreeAsync(voucherId, "acc1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(BankStatementLineMatchState.Unmatched, line.MATCH_STATE);
        Assert.Null(line.RESOLUTION_VOUCHER_ID);
        Assert.Null(line.RESOLUTION_TYPE);
    }

    [Fact]
    public async Task Unresolve_BankFee_NonTemporaryVoucher_Throws_LineStaysResolved()
    {
        var voucherId = Guid.NewGuid();
        _heads.Setup(h => h.GetForUpdateAsync(voucherId, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_VOUCHERSHEAD { ID = voucherId, DOCLIFE = DocLife.Accepted });
        var line = Line(BankStatementLineMatchState.Resolved);
        line.RESOLUTION_TYPE = BankStatementLineResolutionType.BankFeeVoucher;
        line.RESOLUTION_VOUCHER_ID = voucherId;

        await Assert.ThrowsAsync<TreasuryBankStatementUnresolveNotAllowedException>(() => ResolutionService().UnresolveAsync(line, Vahed));

        Assert.Equal(BankStatementLineMatchState.Resolved, line.MATCH_STATE);
    }

    [Fact]
    public async Task Unresolve_BankFee_VoucherAlreadyDeleted_JustClearsLine()
    {
        var voucherId = Guid.NewGuid();
        _heads.Setup(h => h.GetForUpdateAsync(voucherId, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_VOUCHERSHEAD { ID = voucherId, ISDELETED = true });
        var line = Line(BankStatementLineMatchState.Resolved);
        line.RESOLUTION_TYPE = BankStatementLineResolutionType.BankFeeVoucher;
        line.RESOLUTION_VOUCHER_ID = voucherId;

        await ResolutionService().UnresolveAsync(line, Vahed);

        Assert.Equal(BankStatementLineMatchState.Unmatched, line.MATCH_STATE);
        _heads.Verify(h => h.SoftDeleteDetailTreeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unresolve_Ignored_ClearsWithoutTouchingVouchers()
    {
        var line = Line(BankStatementLineMatchState.Resolved);
        line.RESOLUTION_TYPE = BankStatementLineResolutionType.Ignored;
        line.RESOLUTION_NOTE = "x";

        await ResolutionService().UnresolveAsync(line, Vahed);

        Assert.Equal(BankStatementLineMatchState.Unmatched, line.MATCH_STATE);
        Assert.Null(line.RESOLUTION_NOTE);
        _heads.VerifyNoOtherCalls();
    }

    // ── Validators ──────────────────────────────────────────────────────────

    [Fact]
    public void ResolveValidator_Rules()
    {
        var v = new ResolveBankStatementLineCommandValidator();
        var ok = new ResolveBankStatementLineCommand(Id, LineId, BankStatementLineResolutionType.Ignored, null, "n") { VahedCode = Vahed };
        Assert.True(v.Validate(ok).IsValid);
        Assert.False(v.Validate(ok with { StatementId = Guid.Empty }).IsValid);
        Assert.False(v.Validate(ok with { LineId = Guid.Empty }).IsValid);
        Assert.False(v.Validate(ok with { Type = (BankStatementLineResolutionType)42 }).IsValid);
        Assert.False(v.Validate(ok with { Note = new string('x', 1001) }).IsValid);
    }

    [Fact]
    public void MatchValidator_RequiresAllIds()
    {
        var v = new MatchBankStatementLineCommandValidator();
        var ok = new MatchBankStatementLineCommand(Id, LineId, Guid.NewGuid()) { VahedCode = Vahed };
        Assert.True(v.Validate(ok).IsValid);
        Assert.False(v.Validate(ok with { VoucherDetailId = Guid.Empty }).IsValid);
        Assert.False(v.Validate(ok with { LineId = Guid.Empty }).IsValid);
    }
}
