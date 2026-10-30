using System.Text;
using Accounting.Application.BankCards;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.BankCards;

/// <summary>
/// کارت حساب جاری (فاز ۴۴-ز): جهت مبالغ، قفل ردیف مغایرت‌گیری‌شده، ورود دیسکت رفاه بدون تکرار،
/// مغایرت‌گیری خودکار چک/فیش و برگرداندن آن.
/// </summary>
public class BankCardHandlersTests
{
    private const string Vahed = "0042";
    private const string Year = "1404";
    private const string AccountNo = "123456789";

    private static readonly Guid AccountCodeId = Guid.NewGuid();

    private readonly Mock<IBankCardRepository> _repo = new();
    private readonly Mock<ITreasuryBankAccountBalanceReadRepository> _balance = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly List<TB_BANKCARTDETAIL> _rows = new();
    private readonly List<TB_BANKCARTDETAIL> _added = new();
    private readonly BankCardAccount _account = new(Guid.NewGuid(), AccountNo, "دارنده", Guid.NewGuid(), Guid.NewGuid(), AccountCodeId, Array.Empty<Guid>());

    public BankCardHandlersTests()
    {
        _user.SetupGet(u => u.UserId).Returns("1234567890");
        _repo.Setup(r => r.GetAccountAsync(_account.Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(_account);
        _repo.Setup(r => r.GetRowsAsync(Vahed, Year, AccountNo, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string _, string _, string? month, CancellationToken _) =>
                _rows.Where(r => month == null || r.MONTH == month).ToList());
        _repo.Setup(r => r.AddRowAsync(It.IsAny<TB_BANKCARTDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_BANKCARTDETAIL, CancellationToken>((r, _) => _added.Add(r));
        _repo.Setup(r => r.FindChequesAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BankCardMatch>());
        _repo.Setup(r => r.FindReceiptsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BankCardMatch>());
    }

    private BankCardHandlers Handlers() => new(_repo.Object, _balance.Object, _uow.Object, _user.Object);

    private TB_BANKCARTDETAIL Row(string month, string? no, CheckReceiptType type, decimal debtor, decimal creditor, string? date = null)
    {
        var r = new TB_BANKCARTDETAIL
        {
            ID = Guid.NewGuid(),
            MONTH = month,
            RECIVDATE = date ?? $"{Year}{month}10",
            CHEQNO = no,
            CHECKRECEIPTTYPE = type,
            DEBTOR = debtor,
            CREDITOR = creditor,
            ISDELETED = false,
        };
        _rows.Add(r);
        _repo.Setup(x => x.GetRowForUpdateAsync(r.ID, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(r);
        return r;
    }

    [Theory]
    [InlineData("0001234", "1234")]
    [InlineData(" 77 ", "77")]
    [InlineData("000", null)]
    [InlineData("12a", null)]
    [InlineData("اعلا-01", null)]
    [InlineData(null, null)]
    public void Normalize_strips_leading_zeros_and_rejects_non_digits(string? input, string? expected)
        => Assert.Equal(expected, BankCardHandlers.Normalize(input));

    [Theory]
    [InlineData("1404", "01", "14040131")]
    [InlineData("1404", "07", "14040730")]
    [InlineData("1403", "12", "14031230")]
    [InlineData("1404", "12", "14041229")]
    public void MonthEnd_uses_persian_calendar(string year, string month, string expected)
        => Assert.Equal(expected, BankCardHandlers.MonthEnd(year, month));

    [Fact]
    public async Task Unknown_account_is_not_found()
        => await Assert.ThrowsAsync<NotFoundException>(() =>
            Handlers().Handle(new GetBankCardQuery(Year, Guid.NewGuid(), "05") { VahedCode = Vahed }, default));

    [Fact]
    public async Task Account_number_longer_than_13_conflicts()
    {
        var longAcc = _account with { Id = Guid.NewGuid(), AccountNumber = "12345678901234" };
        _repo.Setup(r => r.GetAccountAsync(longAcc.Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(longAcc);
        await Assert.ThrowsAsync<BankCardConflictException>(() =>
            Handlers().Handle(new GetBankCardQuery(Year, longAcc.Id, "05") { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Save_new_deposit_goes_to_creditor_and_month_from_date()
    {
        var id = await Handlers().Handle(new SaveBankCardRowCommand(null, Year, _account.Id, "14040512", " 0099 ", CheckReceiptType.Fish, true, 500m) { VahedCode = Vahed }, default);

        var r = Assert.Single(_added);
        Assert.Equal(id, r.ID);
        Assert.Equal("05", r.MONTH);
        Assert.Equal("0099", r.CHEQNO);
        Assert.Equal(500m, r.CREDITOR);
        Assert.Equal(0m, r.DEBTOR);
        Assert.Equal(AccountNo, r.ACCOUNTNUMBER);
        Assert.Equal(_account.BankId, r.BANK_ID);
    }

    [Fact]
    public async Task Save_withdrawal_goes_to_debtor()
    {
        await Handlers().Handle(new SaveBankCardRowCommand(null, Year, _account.Id, "14040512", null, CheckReceiptType.RealCheck, false, 300m) { VahedCode = Vahed }, default);
        var r = Assert.Single(_added);
        Assert.Equal(300m, r.DEBTOR);
        Assert.Equal(0m, r.CREDITOR);
        Assert.Null(r.CHEQNO);
    }

    [Fact]
    public async Task Edit_reconciled_row_conflicts()
    {
        var r = Row("05", "1", CheckReceiptType.Fish, 0, 10);
        r.RECEIP_ID = Guid.NewGuid();
        await Assert.ThrowsAsync<BankCardConflictException>(() =>
            Handlers().Handle(new SaveBankCardRowCommand(r.ID, Year, _account.Id, "14040512", "1", CheckReceiptType.Fish, true, 20m) { VahedCode = Vahed }, default));
        Assert.Equal(10m, r.CREDITOR);
    }

    [Fact]
    public async Task Edit_open_row_updates_in_place()
    {
        var r = Row("05", "1", CheckReceiptType.Fish, 0, 10);
        await Handlers().Handle(new SaveBankCardRowCommand(r.ID, Year, _account.Id, "14040612", "1", CheckReceiptType.Fish, false, 20m) { VahedCode = Vahed }, default);
        Assert.Empty(_added);
        Assert.Equal("06", r.MONTH);
        Assert.Equal(20m, r.DEBTOR);
        Assert.Equal(0m, r.CREDITOR);
    }

    [Fact]
    public async Task Delete_reconciled_row_conflicts()
    {
        var r = Row("05", "1", CheckReceiptType.RealCheck, 10, 0);
        r.CHECK_ID = Guid.NewGuid();
        await Assert.ThrowsAsync<BankCardConflictException>(() =>
            Handlers().Handle(new DeleteBankCardRowCommand(r.ID) { VahedCode = Vahed }, default));
        Assert.NotEqual(true, r.ISDELETED);
    }

    [Fact]
    public async Task Delete_open_row_soft_deletes()
    {
        var r = Row("05", "1", CheckReceiptType.RealCheck, 10, 0);
        await Handlers().Handle(new DeleteBankCardRowCommand(r.ID) { VahedCode = Vahed }, default);
        Assert.True(r.ISDELETED);
    }

    [Fact]
    public async Task GetCard_totals_and_orders_rows()
    {
        Row("05", "2", CheckReceiptType.RealCheck, 40, 0, "14040520");
        var reconciled = Row("05", "1", CheckReceiptType.Fish, 0, 100, "14040502");
        reconciled.RECEIP_ID = Guid.NewGuid();
        Row("06", "3", CheckReceiptType.Fish, 0, 999);

        var dto = await Handlers().Handle(new GetBankCardQuery(Year, _account.Id, "05") { VahedCode = Vahed }, default);

        Assert.Equal(2, dto.Rows.Count);
        Assert.Equal("14040502", dto.Rows[0].Date);
        Assert.Equal(100m, dto.TotalDeposit);
        Assert.Equal(40m, dto.TotalWithdrawal);
        Assert.Equal(1, dto.ReconciledCount);
    }

    [Fact]
    public async Task Reconcile_matches_cheque_by_number_and_debtor_amount()
    {
        var row = Row("05", "000123", CheckReceiptType.RealCheck, 700, 0, "14040515");
        var checkId = Guid.NewGuid();
        _repo.Setup(r => r.FindChequesAsync(_account.Id, Vahed, Year, It.Is<IReadOnlyCollection<string>>(n => n.Contains("123")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new BankCardMatch(checkId, "123", 700m) });

        var result = await Handlers().Handle(new ReconcileBankCardCommand(Year, _account.Id, "05") { VahedCode = Vahed }, default);

        Assert.Equal(1, result.Matched);
        Assert.Equal(0, result.Remaining);
        Assert.Equal(checkId, row.CHECK_ID);
        _repo.Verify(r => r.SetCheckReceivedDateAsync(checkId, "14040515", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Reconcile_skips_amount_mismatch_and_ambiguous_hits()
    {
        var mismatch = Row("05", "1", CheckReceiptType.RealCheck, 700, 0);
        var ambiguous = Row("05", "2", CheckReceiptType.RealCheck, 50, 0);
        _repo.Setup(r => r.FindChequesAsync(It.IsAny<Guid>(), Vahed, Year, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new BankCardMatch(Guid.NewGuid(), "1", 701m),
                new BankCardMatch(Guid.NewGuid(), "2", 50m),
                new BankCardMatch(Guid.NewGuid(), "02", 50m),
            });

        var result = await Handlers().Handle(new ReconcileBankCardCommand(Year, _account.Id, "05") { VahedCode = Vahed }, default);

        Assert.Equal(0, result.Matched);
        Assert.Equal(2, result.Remaining);
        Assert.Null(mismatch.CHECK_ID);
        Assert.Null(ambiguous.CHECK_ID);
    }

    [Fact]
    public async Task Reconcile_does_not_reuse_a_cheque_matched_earlier_in_the_year()
    {
        var checkId = Guid.NewGuid();
        var earlier = Row("04", "5", CheckReceiptType.RealCheck, 10, 0);
        earlier.CHECK_ID = checkId;
        var row = Row("05", "5", CheckReceiptType.RealCheck, 10, 0);
        _repo.Setup(r => r.FindChequesAsync(It.IsAny<Guid>(), Vahed, Year, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new BankCardMatch(checkId, "5", 10m) });

        var result = await Handlers().Handle(new ReconcileBankCardCommand(Year, _account.Id, "05") { VahedCode = Vahed }, default);

        Assert.Equal(0, result.Matched);
        Assert.Null(row.CHECK_ID);
    }

    [Fact]
    public async Task Reconcile_matches_fish_and_havale_by_creditor_into_receipt_id()
    {
        var fish = Row("05", "9", CheckReceiptType.Fish, 0, 300);
        var havale = Row("05", "8", CheckReceiptType.Havale, 0, 200);
        var f = Guid.NewGuid();
        var h = Guid.NewGuid();
        _repo.Setup(r => r.FindReceiptsAsync(AccountCodeId, Vahed, Year, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new BankCardMatch(f, "9", 300m), new BankCardMatch(h, "8", 200m) });

        var result = await Handlers().Handle(new ReconcileBankCardCommand(Year, _account.Id, "05") { VahedCode = Vahed }, default);

        Assert.Equal(2, result.Matched);
        Assert.Equal(f, fish.RECEIP_ID);
        Assert.Null(fish.CHECK_ID);
        Assert.Equal(h, havale.RECEIP_ID);
        _repo.Verify(r => r.FindChequesAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reconcile_without_linked_account_code_skips_receipts()
    {
        var noCode = _account with { Id = Guid.NewGuid(), AccountCodeId = null };
        _repo.Setup(r => r.GetAccountAsync(noCode.Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(noCode);
        Row("05", "9", CheckReceiptType.Fish, 0, 300);

        var result = await Handlers().Handle(new ReconcileBankCardCommand(Year, noCode.Id, "05") { VahedCode = Vahed }, default);

        Assert.Equal(0, result.Matched);
        _repo.Verify(r => r.FindReceiptsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unreconcile_clears_links_and_received_dates()
    {
        var r = Row("05", "1", CheckReceiptType.RealCheck, 10, 0);
        var c = Guid.NewGuid();
        r.CHECK_ID = c;

        await Handlers().Handle(new UnreconcileBankCardRowCommand(r.ID) { VahedCode = Vahed }, default);

        Assert.Null(r.CHECK_ID);
        _repo.Verify(x => x.SetCheckReceivedDateAsync(c, null, It.IsAny<CancellationToken>()));
        _repo.Verify(x => x.SetReceiptReceivedDateAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reconciliation_without_account_code_conflicts()
    {
        var noCode = _account with { Id = Guid.NewGuid(), AccountCodeId = null };
        _repo.Setup(r => r.GetAccountAsync(noCode.Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(noCode);
        await Assert.ThrowsAsync<BankCardConflictException>(() =>
            Handlers().Handle(new GetBankCardReconciliationQuery(Year, noCode.Id, "05") { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Reconciliation_lists_open_rows_up_to_month_split_by_direction()
    {
        Row("04", "1", CheckReceiptType.Fish, 0, 100);
        Row("05", "2", CheckReceiptType.RealCheck, 50, 0);
        Row("06", "3", CheckReceiptType.Fish, 0, 999);
        var done = Row("05", "4", CheckReceiptType.Fish, 0, 70);
        done.RECEIP_ID = Guid.NewGuid();
        _balance.Setup(b => b.GetBalanceAsync(AccountCodeId, It.IsAny<IReadOnlyCollection<Guid>>(), Vahed, Year, "14040531", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1234m);
        _repo.Setup(r => r.GetOutstandingBookItemsAsync(AccountCodeId, It.IsAny<IReadOnlyCollection<Guid>>(), Vahed, Year, "14040531", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BankCardBookItemDto>());

        var dto = await Handlers().Handle(new GetBankCardReconciliationQuery(Year, _account.Id, "05") { VahedCode = Vahed }, default);

        Assert.Equal(1234m, dto.BookBalance);
        Assert.Equal(100m, Assert.Single(dto.BankOnlyDeposits).Deposit);
        Assert.Equal(50m, Assert.Single(dto.BankOnlyWithdrawals).Withdrawal);
    }

    // --- دیسکت رفاه -------------------------------------------------------------------------------

    private static string DiskLine(string date, bool notice, char trans, long amount, string number = "")
    {
        var c = Enumerable.Repeat(' ', 139).ToArray();
        void Put(int at, string s) { for (var i = 0; i < s.Length; i++) c[at + i] = s[i]; }
        Put(1, date);
        Put(15, number.PadLeft(6, '0'));
        c[21] = trans;
        Put(22, amount.ToString().PadLeft(13, '0'));
        Put(59, AccountNo.PadLeft(18, '0'));
        c[137] = notice ? '0' : '1';
        return new string(c);
    }

    private ImportBankCardDiskCommand Import(params string[] lines)
        => new(Year, _account.Id, "05", "STM001.txt", Encoding.ASCII.GetBytes(string.Join("\r\n", lines))) { VahedCode = Vahed };

    [Fact]
    public async Task Import_maps_notices_and_fish_and_numbers_notices()
    {
        var result = await Handlers().Handle(Import(
            DiskLine("14040503", notice: true, trans: '1', amount: 1000),
            DiskLine("14040504", notice: true, trans: '0', amount: 200),
            DiskLine("14040505", notice: false, trans: '0', amount: 300, number: "4567")), default);

        Assert.Equal(new BankCardImportResult(3, 0), result);
        Assert.Equal(CheckReceiptType.SoriCheck, _added[0].CHECKRECEIPTTYPE);
        Assert.Equal("اعلا-01", _added[0].CHEQNO);
        Assert.Equal(1000m, _added[0].CREDITOR);
        Assert.Equal("اعلا-02", _added[1].CHEQNO);
        Assert.Equal(200m, _added[1].DEBTOR);
        Assert.Equal(CheckReceiptType.Fish, _added[2].CHECKRECEIPTTYPE);
        Assert.Equal("4567", _added[2].CHEQNO);
        Assert.Equal(300m, _added[2].CREDITOR);
        Assert.All(_added, r => Assert.Equal("05", r.MONTH));
    }

    [Fact]
    public async Task Import_twice_skips_existing_rows_including_renumbered_notices()
    {
        var lines = new[]
        {
            DiskLine("14040503", notice: true, trans: '1', amount: 1000),
            DiskLine("14040505", notice: false, trans: '0', amount: 300, number: "4567"),
        };
        await Handlers().Handle(Import(lines), default);
        _rows.AddRange(_added);
        _added.Clear();

        var again = await Handlers().Handle(Import(lines), default);

        Assert.Equal(new BankCardImportResult(0, 2), again);
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Import_rows_of_other_month_conflict()
    {
        await Assert.ThrowsAsync<BankCardConflictException>(() => Handlers().Handle(Import(
            DiskLine("14040603", notice: true, trans: '1', amount: 1000)), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Import_wrong_file_name_rejected()
    {
        var cmd = Import(DiskLine("14040503", notice: true, trans: '1', amount: 1)) with { FileName = "other.txt" };
        await Assert.ThrowsAsync<TreasuryBankStatementFileInvalidException>(() => Handlers().Handle(cmd, default));
    }

    [Fact]
    public void Save_validator_rejects_date_outside_year_and_zero_amount()
    {
        var v = new SaveBankCardRowCommandValidator();
        Assert.False(v.Validate(new SaveBankCardRowCommand(null, Year, _account.Id, "14030512", null, CheckReceiptType.Fish, true, 1m)).IsValid);
        Assert.False(v.Validate(new SaveBankCardRowCommand(null, Year, _account.Id, "14040512", null, CheckReceiptType.Fish, true, 0m)).IsValid);
        Assert.False(v.Validate(new SaveBankCardRowCommand(null, Year, _account.Id, "14040512", "123456789", CheckReceiptType.Fish, true, 1m)).IsValid);
        Assert.True(v.Validate(new SaveBankCardRowCommand(null, Year, _account.Id, "14040512", "12345678", CheckReceiptType.Fish, true, 1m)).IsValid);
    }
}
