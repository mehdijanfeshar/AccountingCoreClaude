using Accounting.Application.Reports.AccountJournal;
using Accounting.Application.Reports.AccountJournal.GetAccountJournal;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// دفتر روزنامه — one row per posting line, chronological, with the set-wide totals.
///
/// <para>
/// Two behaviours here are the reason this report does not read
/// <c>VW_ACCOUNTJOURNALREPORT</c>: <see cref="Excludes_lines_of_a_deleted_voucher"/> (the view has
/// no <c>isdeleted</c> predicate and does not even expose the column) and
/// <see cref="Two_lines_on_the_same_moin_stay_two_rows"/> (the view groups them into one and shows
/// the voucher's شرح instead of the line's).
/// </para>
///
/// <para>
/// ⚠️ Standing limitation, recorded as open risk #25: SQLite cannot catch Oracle-specific
/// translation failures.
/// </para>
/// </summary>
public sealed class AccountJournalReadRepositoryTests : IDisposable
{
    private const string CreateTablesSql = """
        CREATE TABLE TB_VOUCHERSHEAD (
            ID TEXT PRIMARY KEY, DOC_NUM TEXT, DATE_DOC TEXT, DOCLIFE INTEGER,
            HEAD_DESC TEXT, APENDIX TEXT, SYSTEM_TYPE TEXT, FLAG_STATE TEXT,
            CREATEDDATE TEXT, UPDATEDDATE TEXT, ADDUSERID TEXT, CHANGEUSERID TEXT,
            VAHEDCODE TEXT, YEAR TEXT, ISDELETED INTEGER, ATTACHFILE BLOB,
            ATTACHFILE_NAME TEXT, ATF_NUM TEXT, ISAUTOMATIC INTEGER,
            SNDVAHEDCODE TEXT, PARENTHEAD_ID TEXT, GLOBALNUMBER TEXT
        );
        CREATE TABLE TB_VOUCHERSDETAIL (
            ID TEXT PRIMARY KEY, ACCOUNT_ID TEXT, RECEIP_ID TEXT, CHECK_ID TEXT,
            LOWLEVELCODE_ID TEXT, ETEBAR_ID TEXT, DESCRIPTION TEXT, RADIF INTEGER,
            DEBTOR NUMERIC, CREDITOR NUMERIC, CREATEDDATE TEXT, UPDATEDDATE TEXT,
            ADDUSERID TEXT, CHANGEUSERID TEXT, VAHEDCODE TEXT, ISDELETED INTEGER,
            VOUCHERSHEAD_ID TEXT, YEAR TEXT
        );
        CREATE TABLE TB_ACCOUNTCODE (
            ID TEXT PRIMARY KEY, TYPECODE INTEGER, PARENTID TEXT, ACCCODE TEXT,
            ACCCODENAME TEXT, TYPEACTIVITY INTEGER, SOURCEANDCONSUME_ID TEXT,
            IDENTYGROUPS_ID TEXT, TYPEACCCODE INTEGER, CREATEDDATE TEXT,
            UPDATEDDATE TEXT, ADDUSERID TEXT, CHANGEUSERID TEXT, ISDELETED INTEGER,
            MOINFORCLOSE TEXT, TYPEACTION INTEGER, VAHEDCODE TEXT
        );
        """;

    private const string Vahed = "1155";
    private const string Year = "1404";

    private readonly SqliteConnection _connection;
    private readonly Dictionary<string, Guid> _accounts = new();

    public AccountJournalReadRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setup = CreateContext();
        setup.Database.ExecuteSqlRaw(CreateTablesSql);

        foreach (var (code, name) in new[]
        {
            ("110001", "صندوق"),
            ("110002", "بانک"),
            ("220001", "حقوق پرداختنی"),
        })
        {
            var id = Guid.NewGuid();
            _accounts[code] = id;
            setup.TB_ACCOUNTCODEs.Add(new TB_ACCOUNTCODE
            {
                ID = id,
                ACCCODE = code,
                ACCCODENAME = name,
                ISDELETED = false,
            });
        }

        setup.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LegacyDbContext(options);
    }

    private async Task SeedVoucherAsync(
        string docNum,
        string dateDoc = "14040215",
        DocLife? docLife = DocLife.Temporary,
        string vahedCode = Vahed,
        string year = Year,
        bool isDeleted = false,
        params (string AccountCode, decimal Debtor, decimal Creditor, string Description, bool IsDeleted)[] lines)
    {
        await using var context = CreateContext();

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = dateDoc,
            DOCLIFE = docLife,
            HEAD_DESC = "شرح سرسند",
            VAHEDCODE = vahedCode,
            YEAR = year,
            ISDELETED = isDeleted,
        };
        context.TB_VOUCHERSHEADs.Add(head);

        var radif = 1;
        foreach (var (accountCode, debtor, creditor, description, lineDeleted) in lines)
        {
            context.TB_VOUCHERSDETAILs.Add(new TB_VOUCHERSDETAIL
            {
                ID = Guid.NewGuid(),
                VOUCHERSHEAD_ID = head.ID,
                ACCOUNT_ID = _accounts[accountCode],
                RADIF = radif++,
                DEBTOR = debtor,
                CREDITOR = creditor,
                DESCRIPTION = description,
                ISDELETED = lineDeleted,
                VAHEDCODE = vahedCode,
                YEAR = year,
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task<AccountJournalResultDto> RunAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? fromVoucherNo = null,
        string? toVoucherNo = null,
        string? fromDate = null,
        string? toDate = null,
        string? fromAccountCode = null,
        string? toAccountCode = null,
        int? docLife = null,
        string? description = null,
        string vahedCode = Vahed)
    {
        await using var context = CreateContext();
        var repository = new AccountJournalReadRepository(context);

        return await repository.GetAsync(new GetAccountJournalQuery(
            pageNumber, pageSize, Year, fromVoucherNo, toVoucherNo, fromDate, toDate,
            fromAccountCode, toAccountCode, docLife, description)
        { VahedCode = vahedCode });
    }

    [Fact]
    public async Task Returns_one_row_per_line_with_its_account_and_own_description()
    {
        await SeedVoucherAsync("000010", lines:
        [
            ("110001", 500m, 0m, "دریافت نقدی", false),
            ("220001", 0m, 500m, "بابت حقوق", false),
        ]);

        var result = await RunAsync();

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("110001", result.Items[0].AccountCode);
        Assert.Equal("صندوق", result.Items[0].AccountName);
        Assert.Equal("دریافت نقدی", result.Items[0].Description);
        Assert.Equal(500m, result.Items[0].Debtor);
        Assert.Equal("بابت حقوق", result.Items[1].Description);
        Assert.Equal(500m, result.Items[1].Creditor);
    }

    /// <summary>
    /// The view collapses these into a single summed row carrying the voucher's شرح. A journal
    /// that merges postings is not a journal, so this asserts they stay apart.
    /// </summary>
    [Fact]
    public async Task Two_lines_on_the_same_moin_stay_two_rows()
    {
        await SeedVoucherAsync("000010", lines:
        [
            ("110001", 100m, 0m, "قسط اول", false),
            ("110001", 200m, 0m, "قسط دوم", false),
        ]);

        var result = await RunAsync();

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(new[] { "قسط اول", "قسط دوم" }, result.Items.Select(i => i.Description));
    }

    /// <summary>
    /// The view has no <c>isdeleted</c> predicate at all and does not expose the column, so a
    /// journal built on it would list deleted vouchers with no way to remove them.
    /// </summary>
    [Fact]
    public async Task Excludes_lines_of_a_deleted_voucher()
    {
        await SeedVoucherAsync("000010", lines: [("110001", 100m, 0m, "زنده", false)]);
        await SeedVoucherAsync("000020", isDeleted: true, lines: [("110001", 999m, 0m, "حذف‌شده", false)]);

        var result = await RunAsync();

        Assert.Equal("زنده", Assert.Single(result.Items).Description);
        Assert.Equal(100m, result.TotalDebtor);
    }

    [Fact]
    public async Task Excludes_individually_deleted_lines()
    {
        await SeedVoucherAsync("000010", lines:
        [
            ("110001", 100m, 0m, "زنده", false),
            ("110002", 999m, 0m, "حذف‌شده", true),
        ]);

        Assert.Equal("زنده", Assert.Single((await RunAsync()).Items).Description);
    }

    [Fact]
    public async Task Excludes_other_units_and_other_years()
    {
        await SeedVoucherAsync("000010", lines: [("110001", 100m, 0m, "خودی", false)]);
        await SeedVoucherAsync("000020", vahedCode: "0002", lines: [("110001", 1m, 0m, "واحد دیگر", false)]);
        await SeedVoucherAsync("000030", year: "1403", lines: [("110001", 1m, 0m, "سال دیگر", false)]);

        Assert.Equal("خودی", Assert.Single((await RunAsync()).Items).Description);
    }

    /// <summary>
    /// Oldest first — the opposite of مرور اسناد, and deliberately so: a journal is the ordered
    /// record of what happened, read forwards.
    /// </summary>
    [Fact]
    public async Task Oldest_voucher_comes_first_and_lines_keep_their_radif_order()
    {
        await SeedVoucherAsync("000020", dateDoc: "14040901", lines: [("110001", 1m, 0m, "متأخر", false)]);
        await SeedVoucherAsync("000010", dateDoc: "14040101", lines:
        [
            ("110001", 1m, 0m, "ردیف یک", false),
            ("110002", 1m, 0m, "ردیف دو", false),
        ]);

        var result = await RunAsync();

        Assert.Equal(
            new[] { "ردیف یک", "ردیف دو", "متأخر" },
            result.Items.Select(i => i.Description));
    }

    [Fact]
    public async Task Voucher_number_range_is_padded_before_comparison()
    {
        await SeedVoucherAsync("000005", dateDoc: "14040101", lines: [("110001", 1m, 0m, "پنج", false)]);
        await SeedVoucherAsync("000009", dateDoc: "14040102", lines: [("110001", 1m, 0m, "نه", false)]);
        await SeedVoucherAsync("000020", dateDoc: "14040103", lines: [("110001", 1m, 0m, "بیست", false)]);
        await SeedVoucherAsync("000021", dateDoc: "14040104", lines: [("110001", 1m, 0m, "بیست‌ویک", false)]);

        var result = await RunAsync(fromVoucherNo: "9", toVoucherNo: "20");

        Assert.Equal(new[] { "نه", "بیست" }, result.Items.Select(i => i.Description));
    }

    [Fact]
    public async Task Date_range_is_inclusive_on_both_ends()
    {
        await SeedVoucherAsync("000010", dateDoc: "14040101", lines: [("110001", 1m, 0m, "اول", false)]);
        await SeedVoucherAsync("000020", dateDoc: "14040215", lines: [("110001", 1m, 0m, "وسط", false)]);
        await SeedVoucherAsync("000030", dateDoc: "14040401", lines: [("110001", 1m, 0m, "آخر", false)]);

        var result = await RunAsync(fromDate: "14040101", toDate: "14040215");

        Assert.Equal(new[] { "اول", "وسط" }, result.Items.Select(i => i.Description));
    }

    /// <summary>
    /// «از کد ۱۱ تا کد ۲۲» must include everything coded under both, which is why the upper bound
    /// is padded with '9' rather than compared as typed — "220001" &gt; "22" ordinally, so an
    /// unpadded upper bound would silently drop the whole ۲۲ range.
    /// </summary>
    [Fact]
    public async Task Account_code_range_treats_a_short_upper_bound_as_a_prefix()
    {
        await SeedVoucherAsync("000010", lines:
        [
            ("110001", 1m, 0m, "یازده", false),
            ("220001", 1m, 0m, "بیست‌ودو", false),
        ]);

        var result = await RunAsync(fromAccountCode: "11", toAccountCode: "22");

        Assert.Equal(new[] { "یازده", "بیست‌ودو" }, result.Items.Select(i => i.Description));
    }

    [Fact]
    public async Task Account_code_lower_bound_excludes_codes_below_it()
    {
        await SeedVoucherAsync("000010", lines:
        [
            ("110001", 1m, 0m, "یازده", false),
            ("220001", 1m, 0m, "بیست‌ودو", false),
        ]);

        var result = await RunAsync(fromAccountCode: "22");

        Assert.Equal("بیست‌ودو", Assert.Single(result.Items).Description);
    }

    [Fact]
    public async Task Filters_by_doc_life_and_description()
    {
        await SeedVoucherAsync("000010", docLife: DocLife.Draft, lines: [("110001", 1m, 0m, "یادداشت", false)]);
        await SeedVoucherAsync("000020", docLife: DocLife.Accepted, lines: [("110001", 1m, 0m, "دائم", false)]);

        Assert.Equal("دائم", Assert.Single((await RunAsync(docLife: (int)DocLife.Accepted)).Items).Description);
        Assert.Equal("یادداشت", Assert.Single((await RunAsync(description: "یادداشت")).Items).Description);
    }

    [Fact]
    public async Task Totals_cover_the_whole_filtered_set_not_the_page()
    {
        await SeedVoucherAsync("000010", lines:
        [
            ("110001", 100m, 0m, "یک", false),
            ("110002", 200m, 0m, "دو", false),
            ("220001", 0m, 300m, "سه", false),
        ]);

        var result = await RunAsync(pageNumber: 1, pageSize: 1);

        Assert.Single(result.Items);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(300m, result.TotalDebtor);
        Assert.Equal(300m, result.TotalCreditor);
    }

    [Fact]
    public async Task Empty_result_reports_zero_totals_rather_than_failing()
    {
        var result = await RunAsync();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0m, result.TotalDebtor);
        Assert.Equal(0m, result.TotalCreditor);
    }

    [Fact]
    public async Task Second_page_skips_the_first()
    {
        await SeedVoucherAsync("000010", lines:
        [
            ("110001", 1m, 0m, "یک", false),
            ("110002", 2m, 0m, "دو", false),
            ("220001", 3m, 0m, "سه", false),
        ]);

        var result = await RunAsync(pageNumber: 2, pageSize: 1);

        Assert.Equal("دو", Assert.Single(result.Items).Description);
        Assert.Equal(3, result.TotalCount);
    }
}
