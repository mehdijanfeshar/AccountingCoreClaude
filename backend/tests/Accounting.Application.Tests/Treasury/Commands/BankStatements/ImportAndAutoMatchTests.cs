using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Commands.Treasury.ImportBankStatement;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Application.Queries.Treasury;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.BankStatements;

/// <summary>ورود فایل صورت‌حساب بانکی (بازه، تکراری‌ها، parser) و تطبیق خودکار (گام شماره/مبلغ‌وتاریخ).</summary>
public sealed class ImportAndAutoMatchTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid BankId = Guid.NewGuid();
    private static readonly Guid AccountCode = Guid.NewGuid();

    private readonly Mock<ITreasuryBankStatementRepository> _statements = new();
    private readonly Mock<ITreasuryBankStatementLineRepository> _lines = new();
    private readonly Mock<ITreasuryRoleAuthorizer> _roles = new();
    private readonly Mock<IBankStatementFileParser> _parser = new();
    private readonly Mock<IServiceProvider> _services = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public ImportAndAutoMatchTests()
    {
        _user.SetupGet(u => u.UserId).Returns("acc1");
        _services.Setup(s => s.GetService(typeof(IBankStatementFileParser))).Returns(_parser.Object);
        _lines.Setup(l => l.GetActiveByStatementAsync(Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_TR_BANK_STATEMENT_LINE>());
        _lines.Setup(l => l.GetMatchedVoucherDetailIdsAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid>());
    }

    private TB_TR_BANK_STATEMENT Statement(BankStatementState state = BankStatementState.Open)
    {
        var s = new TB_TR_BANK_STATEMENT
        {
            ID = Id, STATE = state, BANK_ACCOUNT_ID = BankId, YEAR = "1405", FROM_DATE = "14050101", TO_DATE = "14050130", SOURCE = BankStatementSource.Manual,
        };
        _statements.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(s);
        return s;
    }

    private ImportBankStatementCommandHandler ImportSut(IServiceProvider? services = null)
        => new(_statements.Object, _lines.Object, _roles.Object, services ?? _services.Object, _uow.Object, _user.Object);

    private static ImportBankStatementCommand ImportCmd() => new(Id, new byte[] { 1, 2, 3 }, "a.txt") { VahedCode = Vahed };

    private void ParserReturns(params ParsedBankStatementLine[] lines)
        => _parser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), BankId, "a.txt", It.IsAny<CancellationToken>())).ReturnsAsync(lines);

    private static ParsedBankStatementLine Parsed(string date, string? reference, decimal withdrawal = 0m, decimal deposit = 100m)
        => new(date, reference, "d", withdrawal, deposit, null);

    // ── Import ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Import_Valid_AddsUnmatchedLinesAndMarksSourceImport()
    {
        var s = Statement();
        ParserReturns(Parsed("14050105", "R1"), Parsed("14050106", "R2", 50m, 0m));
        var added = new List<TB_TR_BANK_STATEMENT_LINE>();
        _lines.Setup(l => l.AddAsync(It.IsAny<TB_TR_BANK_STATEMENT_LINE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_BANK_STATEMENT_LINE, CancellationToken>((l, _) => added.Add(l)).Returns(Task.CompletedTask);

        var count = await ImportSut().Handle(ImportCmd(), default);

        Assert.Equal(2, count);
        Assert.All(added, l =>
        {
            Assert.Equal(BankStatementLineMatchState.Unmatched, l.MATCH_STATE);
            Assert.Equal(Id, l.STATEMENT_ID);
            Assert.Equal("1405", l.YEAR);
            Assert.Equal(Vahed, l.VAHEDCODE);
        });
        Assert.Equal(BankStatementSource.Import, s.SOURCE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Import_ParserNotRegistered_Throws()
    {
        var empty = new Mock<IServiceProvider>();

        await Assert.ThrowsAsync<TreasuryBankStatementParserNotConfiguredException>(() => ImportSut(empty.Object).Handle(ImportCmd(), default));
    }

    [Fact]
    public async Task Import_LineOutsideStatementRange_ThrowsFileInvalid_NothingAdded()
    {
        Statement();
        ParserReturns(Parsed("14050105", "R1"), Parsed("14050215", "R2"));

        await Assert.ThrowsAsync<TreasuryBankStatementFileInvalidException>(() => ImportSut().Handle(ImportCmd(), default));

        _lines.Verify(l => l.AddAsync(It.IsAny<TB_TR_BANK_STATEMENT_LINE>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Import_RangeBoundariesAreInclusive()
    {
        Statement();
        ParserReturns(Parsed("14050101", "A"), Parsed("14050130", "B"));

        Assert.Equal(2, await ImportSut().Handle(ImportCmd(), default));
    }

    [Fact]
    public async Task Import_SameFileTwice_SkipsExistingLines()
    {
        Statement();
        ParserReturns(Parsed("14050105", "R1"), Parsed("14050106", "R2"));
        _lines.Setup(l => l.GetActiveByStatementAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_TR_BANK_STATEMENT_LINE>
            {
                new() { LINE_DATE = "14050105", BANK_REFERENCE = "R1", DEPOSIT = 100m, WITHDRAWAL = 0m },
            });

        var count = await ImportSut().Handle(ImportCmd(), default);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Import_ClosedStatement_Throws()
    {
        Statement(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(() => ImportSut().Handle(ImportCmd(), default));
    }

    [Fact]
    public async Task Import_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() => ImportSut().Handle(ImportCmd(), default));

    [Fact]
    public async Task Import_RoleDenied_ThrowsBeforeParsing()
    {
        _roles.Setup(r => r.EnsureHasRoleAsync(Vahed, It.IsAny<IReadOnlyCollection<TreasuryRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryRoleRequiredException(Vahed, TreasuryRole.Treasurer));

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(() => ImportSut().Handle(ImportCmd(), default));

        _parser.VerifyNoOtherCalls();
    }

    [Fact]
    public void ImportValidator_Rules()
    {
        var v = new ImportBankStatementCommandValidator();
        Assert.True(v.Validate(ImportCmd()).IsValid);
        Assert.False(v.Validate(ImportCmd() with { Content = Array.Empty<byte>() }).IsValid);
        Assert.False(v.Validate(ImportCmd() with { StatementId = Guid.Empty }).IsValid);
    }

    // ── AutoMatch service ───────────────────────────────────────────────────

    private readonly Mock<IBankAccountReadRepository> _banks = new();
    private readonly Mock<IBankStatementBookCandidateReadRepository> _candidates = new();

    private BankStatementAutoMatchService AutoSut(Guid? accountCode = null)
    {
        _banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(
            new BankAccountDto(BankId, "1", "h", null, null, null, null, null, null, accountCode ?? AccountCode, Vahed, null, null, null, null, null, false,
                new List<BankAccountTafsiliLinkDto>()));
        return new BankStatementAutoMatchService(_lines.Object, _banks.Object, _candidates.Object, _user.Object);
    }

    private void Lines(params TB_TR_BANK_STATEMENT_LINE[] lines)
        => _lines.Setup(l => l.GetActiveByStatementAsync(Id, It.IsAny<CancellationToken>())).ReturnsAsync(lines);

    private static TB_TR_BANK_STATEMENT_LINE StmtLine(string date, decimal deposit = 0m, decimal withdrawal = 0m, string? reference = null,
        BankStatementLineMatchState state = BankStatementLineMatchState.Unmatched)
        => new() { ID = Guid.NewGuid(), LINE_DATE = date, DEPOSIT = deposit, WITHDRAWAL = withdrawal, BANK_REFERENCE = reference, MATCH_STATE = state, VAHEDCODE = Vahed };

    private static BankStatementBookLineDto Book(string date, decimal debit = 0m, decimal credit = 0m, string? sourceRef = null, string? docNo = null)
        => new(Guid.NewGuid(), Guid.NewGuid(), "1", date, debit, credit, null, sourceRef, docNo);

    private void CandidatesFor(bool debitSide, params BankStatementBookLineDto[] items)
        => _candidates.Setup(c => c.GetCandidatesAsync(AccountCode, It.IsAny<IReadOnlyCollection<Guid>>(), debitSide,
                It.IsAny<string>(), It.IsAny<string>(), Vahed, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

    private void NoCandidates()
    {
        CandidatesFor(true);
        CandidatesFor(false);
    }

    [Fact]
    public async Task Auto_BankAccountMissingOrWithoutAccountCode_ThrowsNotFound()
    {
        var s = Statement();
        var sut = AutoSut();
        _banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.AutoMatchAsync(s, Vahed));

        _banks.Setup(b => b.GetByIdAsync(BankId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(
            new BankAccountDto(BankId, "1", "h", null, null, null, null, null, null, null, Vahed, null, null, null, null, null, false, new List<BankAccountTafsiliLinkDto>()));
        await Assert.ThrowsAsync<NotFoundException>(() => sut.AutoMatchAsync(s, Vahed));
    }

    [Fact]
    public async Task Auto_DepositWithSingleAmountMatchInWindow_MatchesToDebit()
    {
        var s = Statement();
        var line = StmtLine("14050110", deposit: 500m);
        Lines(line);
        var book = Book("14050112", debit: 500m);
        NoCandidates();
        CandidatesFor(true, book);

        var result = await AutoSut().AutoMatchAsync(s, Vahed);

        Assert.Equal(new BankStatementAutoMatchResult(1, 0), result);
        Assert.Equal(BankStatementLineMatchState.AutoMatched, line.MATCH_STATE);
        Assert.Equal(book.VoucherDetailId, line.MATCHED_VOUCHERDETAIL_ID);
        Assert.Equal("acc1", line.CHANGEUSERID);
    }

    [Fact]
    public async Task Auto_WithdrawalUsesCreditSide()
    {
        var s = Statement();
        var line = StmtLine("14050110", withdrawal: 300m);
        Lines(line);
        var book = Book("14050110", credit: 300m);
        NoCandidates();
        CandidatesFor(false, book);

        var result = await AutoSut().AutoMatchAsync(s, Vahed);

        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(book.VoucherDetailId, line.MATCHED_VOUCHERDETAIL_ID);
    }

    [Fact]
    public async Task Auto_AmbiguousAmountMatches_DisambiguatedByExactDate()
    {
        var s = Statement();
        var line = StmtLine("14050110", deposit: 500m);
        Lines(line);
        var exact = Book("14050110", debit: 500m);
        NoCandidates();
        CandidatesFor(true, exact, Book("14050112", debit: 500m));

        await AutoSut().AutoMatchAsync(s, Vahed);

        Assert.Equal(exact.VoucherDetailId, line.MATCHED_VOUCHERDETAIL_ID);
    }

    [Fact]
    public async Task Auto_AmbiguousEvenByDate_LeavesUnmatched()
    {
        var s = Statement();
        var line = StmtLine("14050110", deposit: 500m);
        Lines(line);
        NoCandidates();
        CandidatesFor(true, Book("14050110", debit: 500m), Book("14050110", debit: 500m));

        var result = await AutoSut().AutoMatchAsync(s, Vahed);

        Assert.Equal(new BankStatementAutoMatchResult(0, 1), result);
        Assert.Equal(BankStatementLineMatchState.Unmatched, line.MATCH_STATE);
    }

    [Fact]
    public async Task Auto_NoAmountMatch_LeavesUnmatched()
    {
        var s = Statement();
        var line = StmtLine("14050110", deposit: 500m);
        Lines(line);
        NoCandidates();
        CandidatesFor(true, Book("14050110", debit: 499m));

        var result = await AutoSut().AutoMatchAsync(s, Vahed);

        Assert.Equal(0, result.MatchedCount);
    }

    [Fact]
    public async Task Auto_NumericReference_MatchesByDocumentNumberIgnoringLeadingZeros()
    {
        var s = Statement();
        var line = StmtLine("14050110", withdrawal: 700m, reference: "000123");
        Lines(line);
        var book = Book("14050125", credit: 700m, docNo: "123");
        NoCandidates();
        CandidatesFor(false, book);

        var result = await AutoSut().AutoMatchAsync(s, Vahed);

        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(book.VoucherDetailId, line.MATCHED_VOUCHERDETAIL_ID);
    }

    [Fact]
    public async Task Auto_SameCandidateNotUsedForTwoLines()
    {
        var s = Statement();
        var first = StmtLine("14050110", deposit: 500m);
        var second = StmtLine("14050110", deposit: 500m);
        Lines(first, second);
        NoCandidates();
        var only = Book("14050110", debit: 500m);
        // مخزن واقعی ردیف‌های «claimed» را کنار می‌گذارد؛ شبیه‌سازی همان رفتار
        _candidates.Setup(c => c.GetCandidatesAsync(AccountCode, It.IsAny<IReadOnlyCollection<Guid>>(), true,
                It.IsAny<string>(), It.IsAny<string>(), Vahed, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> _, bool _, string _, string _, string _, IReadOnlyCollection<Guid> excluded, CancellationToken _)
                => excluded.Contains(only.VoucherDetailId) ? new List<BankStatementBookLineDto>() : new List<BankStatementBookLineDto> { only });

        var result = await AutoSut().AutoMatchAsync(s, Vahed);

        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(1, result.UnmatchedCount);
        Assert.Equal(BankStatementLineMatchState.AutoMatched, first.MATCH_STATE);
        Assert.Equal(BankStatementLineMatchState.Unmatched, second.MATCH_STATE);
    }

    [Fact]
    public async Task Auto_AlreadyMatchedLinesAreIgnored()
    {
        var s = Statement();
        var matched = StmtLine("14050110", deposit: 500m, state: BankStatementLineMatchState.ManualMatched);
        Lines(matched);
        NoCandidates();

        var result = await AutoSut().AutoMatchAsync(s, Vahed);

        Assert.Equal(new BankStatementAutoMatchResult(0, 0), result);
        _candidates.Verify(c => c.GetCandidatesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
