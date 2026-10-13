using Accounting.Application.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.AccountCodes.GetTafsiliLevelItems;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Application.Queries.PettyCash;
using Accounting.Application.Queries.Treasury;
using Accounting.Application.Queries.Treasury.GetApprovalCartable;
using Accounting.Application.Queries.Treasury.GetBankAccountBalance;
using Accounting.Application.Queries.Treasury.GetBankStatementById;
using Accounting.Application.Queries.Treasury.GetBankStatementBookCandidates;
using Accounting.Application.Queries.Treasury.GetBeneficiaryTafsilis;
using Accounting.Application.Queries.Treasury.GetCustomerTafsilis;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Queries;

/// <summary>Queryهای خزانه‌داری با منطق: کارتابل تأیید، مانده بانک، صورت‌حساب (خلاصهٔ مغایرت)، کاندیدای تطبیق، جستجوی تفصیلی.</summary>
public sealed class TreasuryQueryHandlerTests
{
    private const string Vahed = "V1";
    private static readonly Guid BankId = Guid.NewGuid();
    private static readonly Guid AccountCode = Guid.NewGuid();

    private static BankAccountDto Bank(Guid? accountCode, params Guid[] tafsilis) => new(
        BankId, "1", "h", null, null, null, null, null, null, accountCode, Vahed, null, null, null, null, null, false,
        tafsilis.Select(t => new BankAccountTafsiliLinkDto(t, Guid.NewGuid())).ToList());

    // ── GetApprovalCartable ─────────────────────────────────────────────────

    private readonly Mock<IPaymentRequestReadRepository> _payments = new();
    private readonly Mock<IPettyCashReplenishmentReadRepository> _replenishments = new();
    private readonly Mock<IPettyCashFundReviewerReadRepository> _reviewers = new();
    private readonly Mock<ITreasuryRoleRepository> _roles = new();
    private readonly Mock<ITreasuryTransferReadRepository> _transfers = new();
    private readonly Mock<ICurrentUser> _user = new();

    private GetApprovalCartableQueryHandler CartableSut(
        IEnumerable<TreasuryRole>? roles = null,
        IEnumerable<PaymentRequestListItemDto>? payments = null,
        IEnumerable<TransferListItemDto>? transfers = null,
        IEnumerable<PettyCashReplenishmentListItemDto>? replenishments = null)
    {
        _user.SetupGet(u => u.UserId).Returns("me");
        _roles.Setup(r => r.GetActiveRolesAsync(Vahed, "me", It.IsAny<CancellationToken>())).ReturnsAsync((roles ?? Array.Empty<TreasuryRole>()).ToList());
        _payments.Setup(p => p.GetPendingForCartableAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((payments ?? Array.Empty<PaymentRequestListItemDto>()).ToList());
        _transfers.Setup(t => t.GetPendingForCartableAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((transfers ?? Array.Empty<TransferListItemDto>()).ToList());
        var reps = (replenishments ?? Array.Empty<PettyCashReplenishmentListItemDto>()).ToList();
        _replenishments.Setup(r => r.GetPagedAsync(1, 500, null, PettyCashReplenishmentState.PendingTreasurer, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PettyCashReplenishmentListItemDto> { Items = reps, PageNumber = 1, PageSize = 500, TotalCount = reps.Count });
        return new GetApprovalCartableQueryHandler(_payments.Object, _replenishments.Object, _reviewers.Object, _roles.Object, _transfers.Object, _user.Object);
    }

    private static PaymentRequestListItemDto Pay(PaymentRequestState state, string creator = "other", int ageDays = 1, decimal net = 100m)
        => new(Guid.NewGuid(), "PAY-1", "ذینفع", TreasuryPaymentType.SupplierOrContractor, net, state, "14990101",
            DateTime.UtcNow.AddDays(-ageDays), DateTime.UtcNow.AddDays(-ageDays - 1), creator);

    private static TransferListItemDto Transfer(string creator = "other", int ageDays = 1)
        => new(Guid.NewGuid(), "TRF-1", Guid.NewGuid(), Guid.NewGuid(), 500m, TransferState.PendingTreasurer, "14050101",
            DateTime.UtcNow.AddDays(-ageDays), creator);

    [Fact]
    public async Task Cartable_PaymentPendingForMe_OnlyWhenRoleMatchesStageAndNotCreator()
    {
        var mine = Pay(PaymentRequestState.PendingFinanceManager);
        var wrongRole = Pay(PaymentRequestState.PendingCeo);
        var ownRequest = Pay(PaymentRequestState.PendingFinanceManager, creator: "me");
        var sut = CartableSut(roles: new[] { TreasuryRole.FinanceManager }, payments: new[] { mine, wrongRole, ownRequest });

        var result = await sut.Handle(new GetApprovalCartableQuery(1, 50) { VahedCode = Vahed }, default);

        Assert.Equal(3, result.TotalCount);
        Assert.True(result.Items.Single(i => i.Id == mine.Id).PendingForMe);
        Assert.False(result.Items.Single(i => i.Id == wrongRole.Id).PendingForMe);
        Assert.False(result.Items.Single(i => i.Id == ownRequest.Id).PendingForMe);
        Assert.All(result.Items, i => Assert.Equal("payment", i.Nature));
    }

    [Fact]
    public async Task Cartable_TransferPendingForTreasurerOnly_AndNotCreator()
    {
        var theirs = Transfer(creator: "other");
        var mine = Transfer(creator: "me");

        var asTreasurer = await CartableSut(roles: new[] { TreasuryRole.Treasurer }, transfers: new[] { theirs, mine })
            .Handle(new GetApprovalCartableQuery(1, 50) { VahedCode = Vahed }, default);
        Assert.True(asTreasurer.Items.Single(i => i.Id == theirs.Id).PendingForMe);
        Assert.False(asTreasurer.Items.Single(i => i.Id == mine.Id).PendingForMe);
        Assert.All(asTreasurer.Items, i => Assert.Equal("transfer", i.Nature));

        var asOther = await CartableSut(roles: new[] { TreasuryRole.FinanceManager }, transfers: new[] { theirs })
            .Handle(new GetApprovalCartableQuery(1, 50) { VahedCode = Vahed }, default);
        Assert.False(asOther.Items.Single().PendingForMe);
    }

    [Fact]
    public async Task Cartable_Replenishment_PendingForMeOnlyForFundTreasurerReviewerNotCreator()
    {
        var fundId = Guid.NewGuid();
        var rep = new PettyCashReplenishmentListItemDto(Guid.NewGuid(), "R-1", fundId, "تنخواه", PettyCashPaymentMethod.BankTransferToCustodian,
            PettyCashReplenishmentState.PendingTreasurer, 900m, 2, DateTime.UtcNow.AddDays(-2), null, "other");
        var sut = CartableSut(replenishments: new[] { rep });
        _reviewers.Setup(r => r.GetByFundIdAsync(fundId, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PettyCashFundReviewerDto> { new(Guid.NewGuid(), fundId, "me", null, PettyCashRole.Treasurer) });

        var result = await sut.Handle(new GetApprovalCartableQuery(1, 50) { VahedCode = Vahed }, default);

        var item = Assert.Single(result.Items);
        Assert.Equal("replenishment", item.Nature);
        Assert.True(item.PendingForMe);
        Assert.Equal("در انتظار اقدام خزانه‌دار", item.StateLabel);
    }

    [Fact]
    public async Task Cartable_ReviewerLookupCachedPerFund()
    {
        var fundId = Guid.NewGuid();
        PettyCashReplenishmentListItemDto Rep() => new(Guid.NewGuid(), "R", fundId, "f", PettyCashPaymentMethod.BankTransferToCustodian,
            PettyCashReplenishmentState.PendingTreasurer, 1m, 1, DateTime.UtcNow, null, "x");
        var sut = CartableSut(replenishments: new[] { Rep(), Rep(), Rep() });
        _reviewers.Setup(r => r.GetByFundIdAsync(fundId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PettyCashFundReviewerDto>());

        await sut.Handle(new GetApprovalCartableQuery(1, 50) { VahedCode = Vahed }, default);

        _reviewers.Verify(r => r.GetByFundIdAsync(fundId, Vahed, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cartable_OrderedByAgeDescendingAndPaged()
    {
        var oldest = Pay(PaymentRequestState.PendingUnitManager, ageDays: 10);
        var middle = Transfer(ageDays: 5);
        var newest = Pay(PaymentRequestState.PendingUnitManager, ageDays: 1);
        var sut = CartableSut(payments: new[] { newest, oldest }, transfers: new[] { middle });

        var page1 = await sut.Handle(new GetApprovalCartableQuery(1, 2) { VahedCode = Vahed }, default);
        var page2 = await sut.Handle(new GetApprovalCartableQuery(2, 2) { VahedCode = Vahed }, default);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(new[] { oldest.Id, middle.Id }, page1.Items.Select(i => i.Id).ToArray());
        Assert.Equal(newest.Id, Assert.Single(page2.Items).Id);
        Assert.Equal(2, page2.PageNumber);
    }

    [Fact]
    public async Task Cartable_NonPendingStatePayment_NeverPendingForMe()
    {
        var sut = CartableSut(roles: new[] { TreasuryRole.UnitManager, TreasuryRole.FinanceManager, TreasuryRole.Ceo },
            payments: new[] { Pay(PaymentRequestState.ReadyForExecution) });

        var result = await sut.Handle(new GetApprovalCartableQuery(1, 50) { VahedCode = Vahed }, default);

        Assert.False(Assert.Single(result.Items).PendingForMe);
    }

    [Fact]
    public void CartableValidator_PagingBounds()
    {
        var v = new GetApprovalCartableQueryValidator();
        Assert.True(v.Validate(new GetApprovalCartableQuery(1, 200)).IsValid);
        Assert.False(v.Validate(new GetApprovalCartableQuery(0, 10)).IsValid);
        Assert.False(v.Validate(new GetApprovalCartableQuery(1, 0)).IsValid);
        Assert.False(v.Validate(new GetApprovalCartableQuery(1, 201)).IsValid);
    }

    // ── GetBankAccountBalance ───────────────────────────────────────────────

    [Fact]
    public async Task Balance_BankMissing_ReturnsNull()
    {
        var banks = new Mock<IBankAccountReadRepository>();
        var sut = new GetBankAccountBalanceQueryHandler(banks.Object, Mock.Of<ITreasuryBankAccountBalanceReadRepository>());

        Assert.Null(await sut.Handle(new GetBankAccountBalanceQuery(BankId) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Balance_NoAccountCode_ThrowsNotFound()
    {
        var banks = new Mock<IBankAccountReadRepository>();
        banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Bank(null));
        var sut = new GetBankAccountBalanceQueryHandler(banks.Object, Mock.Of<ITreasuryBankAccountBalanceReadRepository>());

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new GetBankAccountBalanceQuery(BankId) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Balance_Valid_ReturnsBalanceForLinkedTafsilis()
    {
        var t1 = Guid.NewGuid();
        var banks = new Mock<IBankAccountReadRepository>();
        banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Bank(AccountCode, t1));
        var balances = new Mock<ITreasuryBankAccountBalanceReadRepository>();
        balances.Setup(b => b.GetBalanceAsync(AccountCode, It.Is<IReadOnlyCollection<Guid>>(c => c.Contains(t1)), Vahed, It.Is<string>(y => y.Length == 4), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(12_345m);
        var sut = new GetBankAccountBalanceQueryHandler(banks.Object, balances.Object);

        var dto = await sut.Handle(new GetBankAccountBalanceQuery(BankId) { VahedCode = Vahed }, default);

        Assert.Equal(BankId, dto!.BankAccountId);
        Assert.Equal(12_345m, dto.Balance);
    }

    // ── GetBankStatementById ────────────────────────────────────────────────

    private readonly Mock<ITreasuryBankStatementReadRepository> _statements = new();
    private readonly Mock<IBankAccountReadRepository> _banks = new();
    private readonly Mock<ITreasuryBankAccountBalanceReadRepository> _balances = new();
    private readonly Mock<IBankStatementBookCandidateReadRepository> _candidates = new();
    private readonly Mock<ITreasuryBankStatementLineRepository> _lineRepo = new();
    private static readonly Guid StatementId = Guid.NewGuid();

    private static BankStatementHeaderDto Header() => new(
        StatementId, "BST-1", BankId, "14050101", "14050130", 1_000m, BankStatementSource.Manual, BankStatementState.Open, null, "u", DateTime.UtcNow);

    private static BankStatementLineDto LineDto(BankStatementLineMatchState state, Guid? matched = null, decimal deposit = 0m, decimal withdrawal = 0m)
        => new(Guid.NewGuid(), StatementId, "14050110", null, null, withdrawal, deposit, null, state, matched, null, null, null, null, null, null, null, "u", DateTime.UtcNow);

    private GetBankStatementByIdQueryHandler StatementSut(params BankStatementLineDto[] lines)
    {
        _statements.Setup(s => s.GetHeaderAsync(StatementId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Header());
        _statements.Setup(s => s.GetLinesAsync(StatementId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(lines);
        _banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Bank(AccountCode));
        _balances.Setup(b => b.GetBalanceAsync(AccountCode, It.IsAny<IReadOnlyCollection<Guid>>(), Vahed, "1405", "14050130", It.IsAny<CancellationToken>()))
            .ReturnsAsync(700m);
        _lineRepo.Setup(l => l.GetMatchedVoucherDetailIdsAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid>());
        _lineRepo.Setup(l => l.GetResolutionVoucherHeadIdsAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid>());
        _candidates.Setup(c => c.GetCandidatesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>(), Vahed, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankStatementBookLineDto>());
        return new GetBankStatementByIdQueryHandler(_statements.Object, _banks.Object, _balances.Object, _candidates.Object, _lineRepo.Object);
    }

    [Fact]
    public async Task Statement_NotFound_ReturnsNull()
    {
        var sut = new GetBankStatementByIdQueryHandler(_statements.Object, _banks.Object, _balances.Object, _candidates.Object, _lineRepo.Object);

        Assert.Null(await sut.Handle(new GetBankStatementByIdQuery(StatementId) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Statement_Summary_ComputesDifferenceAndStateCounts()
    {
        var sut = StatementSut(
            LineDto(BankStatementLineMatchState.Unmatched),
            LineDto(BankStatementLineMatchState.Unmatched),
            LineDto(BankStatementLineMatchState.AutoMatched, Guid.NewGuid()),
            LineDto(BankStatementLineMatchState.ManualMatched, Guid.NewGuid()),
            LineDto(BankStatementLineMatchState.Resolved));

        var dto = (await sut.Handle(new GetBankStatementByIdQuery(StatementId) { VahedCode = Vahed }, default))!;

        Assert.Equal(1_000m, dto.Summary.ClosingBalance);
        Assert.Equal(700m, dto.Summary.BookBalance);
        Assert.Equal(300m, dto.Summary.Difference);
        Assert.Equal(2, dto.Summary.UnmatchedCount);
        Assert.Equal(1, dto.Summary.AutoMatchedCount);
        Assert.Equal(1, dto.Summary.ManualMatchedCount);
        Assert.Equal(1, dto.Summary.ResolvedCount);
        Assert.Equal(5, dto.Lines.Count);
    }

    [Fact]
    public async Task Statement_BookOnly_ExcludesResolvedVouchers_AndIsSortedByDate()
    {
        var sut = StatementSut();
        var resolvedHead = Guid.NewGuid();
        var later = new BankStatementBookLineDto(Guid.NewGuid(), Guid.NewGuid(), "2", "14050120", 10m, 0m, null, null);
        var earlier = new BankStatementBookLineDto(Guid.NewGuid(), Guid.NewGuid(), "1", "14050105", 0m, 20m, null, null);
        var explained = new BankStatementBookLineDto(Guid.NewGuid(), resolvedHead, "3", "14050110", 0m, 5m, null, null);
        _candidates.Setup(c => c.GetCandidatesAsync(AccountCode, It.IsAny<IReadOnlyCollection<Guid>>(), true, "14050101", "14050130", Vahed, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankStatementBookLineDto> { later });
        _candidates.Setup(c => c.GetCandidatesAsync(AccountCode, It.IsAny<IReadOnlyCollection<Guid>>(), false, "14050101", "14050130", Vahed, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankStatementBookLineDto> { explained, earlier });
        _lineRepo.Setup(l => l.GetResolutionVoucherHeadIdsAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid> { resolvedHead });

        var dto = (await sut.Handle(new GetBankStatementByIdQuery(StatementId) { VahedCode = Vahed }, default))!;

        Assert.Equal(new[] { earlier.VoucherDetailId, later.VoucherDetailId }, dto.BookOnly.Select(b => b.VoucherDetailId).ToArray());
    }

    [Fact]
    public async Task Statement_BookOnly_ExcludesDetailsMatchedOnThisAndOtherStatements()
    {
        var onThis = Guid.NewGuid();
        var onOther = Guid.NewGuid();
        var sut = StatementSut(LineDto(BankStatementLineMatchState.ManualMatched, onThis));
        _lineRepo.Setup(l => l.GetMatchedVoucherDetailIdsAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid> { onOther, onThis });

        await sut.Handle(new GetBankStatementByIdQuery(StatementId) { VahedCode = Vahed }, default);

        _candidates.Verify(c => c.GetCandidatesAsync(AccountCode, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<bool>(), "14050101", "14050130", Vahed,
            It.Is<IReadOnlyCollection<Guid>>(ex => ex.Contains(onThis) && ex.Contains(onOther) && ex.Count == 2), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Statement_BankAccountMissingOrWithoutCode_ThrowsNotFound()
    {
        var sut = StatementSut();
        _banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new GetBankStatementByIdQuery(StatementId) { VahedCode = Vahed }, default));

        _banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Bank(null));
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new GetBankStatementByIdQuery(StatementId) { VahedCode = Vahed }, default));
    }

    // ── GetBankStatementBookCandidates ──────────────────────────────────────

    private GetBankStatementBookCandidatesQueryHandler CandidatesSut(BankStatementLineDto? line, BankAccountDto? bank)
    {
        _statements.Setup(s => s.GetHeaderAsync(StatementId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Header());
        _statements.Setup(s => s.GetLinesAsync(StatementId, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line is null ? new List<BankStatementLineDto>() : new List<BankStatementLineDto> { line });
        _banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(bank);
        _lineRepo.Setup(l => l.GetMatchedVoucherDetailIdsAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid>());
        return new GetBankStatementBookCandidatesQueryHandler(_statements.Object, _banks.Object, _candidates.Object, _lineRepo.Object);
    }

    [Fact]
    public async Task Candidates_Deposit_QueriesDebitSideWithinPlusMinusThreeDays()
    {
        var line = LineDto(BankStatementLineMatchState.Unmatched, deposit: 500m);
        var sut = CandidatesSut(line, Bank(AccountCode));
        var expected = new List<BankStatementBookLineDto> { new(Guid.NewGuid(), Guid.NewGuid(), "1", "14050111", 500m, 0m, null, null) };
        _candidates.Setup(c => c.GetCandidatesAsync(AccountCode, It.IsAny<IReadOnlyCollection<Guid>>(), true, It.IsAny<string>(), It.IsAny<string>(), Vahed, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await sut.Handle(new GetBankStatementBookCandidatesQuery(StatementId, line.Id) { VahedCode = Vahed }, default);

        Assert.Same(expected, result);
        // خط ۱۴۰۵۰۱۱۰ ⇒ بازهٔ ±۳ روز، تقویم شمسی: ۱۴۰۵۰۱۰۷ تا ۱۴۰۵۰۱۱۳
        _candidates.Verify(c => c.GetCandidatesAsync(AccountCode, It.IsAny<IReadOnlyCollection<Guid>>(), true, "14050107", "14050113", Vahed, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Candidates_Withdrawal_QueriesCreditSide()
    {
        var line = LineDto(BankStatementLineMatchState.Unmatched, withdrawal: 500m);
        var sut = CandidatesSut(line, Bank(AccountCode));
        _candidates.Setup(c => c.GetCandidatesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), false, It.IsAny<string>(), It.IsAny<string>(), Vahed, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankStatementBookLineDto>());

        await sut.Handle(new GetBankStatementBookCandidatesQuery(StatementId, line.Id) { VahedCode = Vahed }, default);

        _candidates.Verify(c => c.GetCandidatesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), false, It.IsAny<string>(), It.IsAny<string>(), Vahed, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Candidates_MissingStatementLineOrBank_ThrowNotFound()
    {
        var line = LineDto(BankStatementLineMatchState.Unmatched, deposit: 1m);

        var noStatement = new GetBankStatementBookCandidatesQueryHandler(_statements.Object, _banks.Object, _candidates.Object, _lineRepo.Object);
        await Assert.ThrowsAsync<NotFoundException>(() => noStatement.Handle(new GetBankStatementBookCandidatesQuery(StatementId, line.Id) { VahedCode = Vahed }, default));

        await Assert.ThrowsAsync<NotFoundException>(() => CandidatesSut(null, Bank(AccountCode))
            .Handle(new GetBankStatementBookCandidatesQuery(StatementId, line.Id) { VahedCode = Vahed }, default));

        await Assert.ThrowsAsync<NotFoundException>(() => CandidatesSut(line, null)
            .Handle(new GetBankStatementBookCandidatesQuery(StatementId, line.Id) { VahedCode = Vahed }, default));

        await Assert.ThrowsAsync<NotFoundException>(() => CandidatesSut(line, Bank(null))
            .Handle(new GetBankStatementBookCandidatesQuery(StatementId, line.Id) { VahedCode = Vahed }, default));
    }

    // ── Beneficiary / customer tafsili lookups ──────────────────────────────

    [Fact]
    public async Task Beneficiaries_GroupNotConfigured_ReturnsEmptyPageWithoutQuerying()
    {
        var settings = new Mock<ITreasurySettingReadRepository>();
        settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(TreasuryTestData.Setting());
        var members = new Mock<ITreasuryBeneficiaryTafsiliReadRepository>();
        var sut = new GetBeneficiaryTafsilisQueryHandler(settings.Object, members.Object);

        var page = await sut.Handle(new GetBeneficiaryTafsilisQuery("x", 2, 25) { VahedCode = Vahed }, default);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(25, page.PageSize);
        members.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Beneficiaries_GroupConfigured_DelegatesToRepository()
    {
        var group = Guid.NewGuid();
        var settings = new Mock<ITreasurySettingReadRepository>();
        settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(TreasuryTestData.Setting(beneficiaryGroupId: group));
        var expected = new PagedResult<TafsiliLookupItemDto> { PageNumber = 1, PageSize = 10, TotalCount = 7 };
        var members = new Mock<ITreasuryBeneficiaryTafsiliReadRepository>();
        members.Setup(m => m.GetPagedAsync(group, "ali", 1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var sut = new GetBeneficiaryTafsilisQueryHandler(settings.Object, members.Object);

        var page = await sut.Handle(new GetBeneficiaryTafsilisQuery("ali", 1, 10) { VahedCode = Vahed }, default);

        Assert.Same(expected, page);
    }

    [Fact]
    public void LookupValidators_PagingAndSearchBounds()
    {
        var b = new GetBeneficiaryTafsilisQueryValidator();
        Assert.True(b.Validate(new GetBeneficiaryTafsilisQuery(null, 1, 200)).IsValid);
        Assert.False(b.Validate(new GetBeneficiaryTafsilisQuery(null, 0, 10)).IsValid);
        Assert.False(b.Validate(new GetBeneficiaryTafsilisQuery(null, 1, 201)).IsValid);
        Assert.False(b.Validate(new GetBeneficiaryTafsilisQuery(new string('x', 201), 1, 10)).IsValid);

        var c = new GetCustomerTafsilisQueryValidator();
        Assert.True(c.Validate(new GetCustomerTafsilisQuery(null, 1, 200)).IsValid);
        Assert.False(c.Validate(new GetCustomerTafsilisQuery(null, 1, 0)).IsValid);
        Assert.False(c.Validate(new GetCustomerTafsilisQuery(new string('x', 201), 1, 10)).IsValid);
    }
}
