using Accounting.Application.Reports.VoucherReview;
using Accounting.Application.Reports.VoucherReview.GetVoucherReview;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// مرور اسناد — filtering, ordering, paging, unit scoping, and the set-wide figures.
///
/// <para>
/// The two tests that matter most are <see cref="Totals_cover_the_whole_filtered_set_not_the_page"/>
/// and <see cref="Many_lines_on_one_voucher_are_summed_once_each"/>. The second one is the reason
/// this report does not read <c>VW_VOUCHERREVIEW</c> at all: that view fans each line out across
/// its تفصیلی links before summing, so it reports inflated — and unequal — sides for a voucher
/// that actually balances. Here the sums come from the lines themselves and cannot be inflated.
/// </para>
///
/// <para>
/// ⚠️ Standing limitation, recorded as open risk #25: SQLite cannot catch Oracle-specific
/// translation failures. It proves the logic, not that the LINQ translates on Oracle.
/// </para>
/// </summary>
public sealed class VoucherReviewReadRepositoryTests : IDisposable
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
        CREATE TABLE TB_SYSTYPE (
            ID TEXT PRIMARY KEY, SYS_COD TEXT, SYS_NAME TEXT
        );
        """;

    private const string Vahed = "1155";
    private const string Year = "1404";

    private readonly SqliteConnection _connection;
    private readonly Guid _systemTypeId = Guid.NewGuid();

    public VoucherReviewReadRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setup = CreateContext();
        setup.Database.ExecuteSqlRaw(CreateTablesSql);
        setup.TB_SYSTYPEs.Add(new TB_SYSTYPE
        {
            ID = _systemTypeId,
            SYS_COD = "01",
            SYS_NAME = "حسابداری مالی",
        });
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

    /// <summary>
    /// Seeds one voucher with a single balanced line, which is the uninteresting default; the
    /// tests that care about the line level pass <paramref name="lines"/> explicitly.
    /// </summary>
    private async Task SeedVoucherAsync(
        string docNum,
        string dateDoc = "14040215",
        decimal debtor = 1000m,
        decimal creditor = 1000m,
        string atfNum = "A100",
        string headDesc = "شرح آزمایشی",
        DocLife? docLife = DocLife.Temporary,
        string vahedCode = Vahed,
        string year = Year,
        bool isDeleted = false,
        Guid? systemType = null,
        (decimal Debtor, decimal Creditor, bool IsDeleted)[]? lines = null)
    {
        await using var context = CreateContext();

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = dateDoc,
            DOCLIFE = docLife,
            HEAD_DESC = headDesc,
            ATF_NUM = atfNum,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ISDELETED = isDeleted,
            SYSTEM_TYPE = systemType ?? _systemTypeId,
        };
        context.TB_VOUCHERSHEADs.Add(head);

        var rows = lines ?? new[] { (debtor, creditor, false) };
        var radif = 1;
        foreach (var (lineDebtor, lineCreditor, lineDeleted) in rows)
        {
            context.TB_VOUCHERSDETAILs.Add(new TB_VOUCHERSDETAIL
            {
                ID = Guid.NewGuid(),
                VOUCHERSHEAD_ID = head.ID,
                RADIF = radif++,
                DEBTOR = lineDebtor,
                CREDITOR = lineCreditor,
                ISDELETED = lineDeleted,
                VAHEDCODE = vahedCode,
                YEAR = year,
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task<VoucherReviewResultDto> RunAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? fromVoucherNo = null,
        string? toVoucherNo = null,
        string? fromDate = null,
        string? toDate = null,
        string? fromAtfNo = null,
        string? toAtfNo = null,
        int? docLife = null,
        Guid? systemTypeId = null,
        string? description = null,
        string vahedCode = Vahed)
    {
        await using var context = CreateContext();
        var repository = new VoucherReviewReadRepository(context);

        return await repository.GetAsync(new GetVoucherReviewQuery(
            pageNumber, pageSize, Year, fromVoucherNo, toVoucherNo, fromDate, toDate,
            fromAtfNo, toAtfNo, docLife, systemTypeId, description)
        { VahedCode = vahedCode });
    }

    [Fact]
    public async Task Returns_the_voucher_with_both_sides_summed_and_its_system_name()
    {
        await SeedVoucherAsync("000010", debtor: 500m, creditor: 500m);

        var result = await RunAsync();

        var row = Assert.Single(result.Items);
        Assert.Equal("000010", row.VoucherNumber);
        Assert.Equal(500m, row.Debtor);
        Assert.Equal(500m, row.Creditor);
        Assert.Equal("حسابداری مالی", row.SystemName);
        Assert.Equal((int)DocLife.Temporary, row.DocLife);
    }

    /// <summary>
    /// The defect that disqualified <c>VW_VOUCHERREVIEW</c>, stated as a test: three debit lines
    /// and one credit line of equal total must report 300/300, each line counted exactly once.
    /// The view would multiply each line by its number of تفصیلی links first.
    /// </summary>
    [Fact]
    public async Task Many_lines_on_one_voucher_are_summed_once_each()
    {
        await SeedVoucherAsync("000010", lines: new[]
        {
            (100m, 0m, false),
            (100m, 0m, false),
            (100m, 0m, false),
            (0m, 300m, false),
        });

        var row = Assert.Single((await RunAsync()).Items);

        Assert.Equal(300m, row.Debtor);
        Assert.Equal(300m, row.Creditor);
    }

    [Fact]
    public async Task Deleted_lines_do_not_count_towards_the_voucher_totals()
    {
        await SeedVoucherAsync("000010", lines: new[]
        {
            (100m, 0m, false),
            (999m, 0m, true),
            (0m, 100m, false),
        });

        var row = Assert.Single((await RunAsync()).Items);

        Assert.Equal(100m, row.Debtor);
        Assert.Equal(100m, row.Creditor);
    }

    [Fact]
    public async Task Excludes_another_units_vouchers()
    {
        await SeedVoucherAsync("000010");
        await SeedVoucherAsync("000011", vahedCode: "0002");

        Assert.Equal("000010", Assert.Single((await RunAsync()).Items).VoucherNumber);
    }

    [Fact]
    public async Task Excludes_another_years_vouchers()
    {
        await SeedVoucherAsync("000010");
        await SeedVoucherAsync("000011", year: "1403");

        Assert.Equal("000010", Assert.Single((await RunAsync()).Items).VoucherNumber);
    }

    [Fact]
    public async Task Excludes_deleted_vouchers()
    {
        await SeedVoucherAsync("000010");
        await SeedVoucherAsync("000011", isDeleted: true);

        Assert.Equal("000010", Assert.Single((await RunAsync()).Items).VoucherNumber);
    }

    [Fact]
    public async Task Newest_voucher_comes_first()
    {
        await SeedVoucherAsync("000010", dateDoc: "14040101");
        await SeedVoucherAsync("000030", dateDoc: "14040901");
        await SeedVoucherAsync("000020", dateDoc: "14040501");

        var result = await RunAsync();

        Assert.Equal(
            new[] { "000030", "000020", "000010" },
            result.Items.Select(i => i.VoucherNumber));
    }

    /// <summary>
    /// Why the range bounds are padded. A user types «۹» and «۲۰», but the column holds
    /// <c>000009</c> and <c>000020</c> — compared unpadded, "9" is ordinally greater than "000020"
    /// and the range would return nothing at all.
    /// </summary>
    [Fact]
    public async Task Voucher_number_range_is_padded_before_comparison()
    {
        await SeedVoucherAsync("000005", dateDoc: "14040101");
        await SeedVoucherAsync("000009", dateDoc: "14040102");
        await SeedVoucherAsync("000020", dateDoc: "14040103");
        await SeedVoucherAsync("000021", dateDoc: "14040104");

        var result = await RunAsync(fromVoucherNo: "9", toVoucherNo: "20");

        Assert.Equal(
            new[] { "000020", "000009" },
            result.Items.Select(i => i.VoucherNumber));
    }

    [Fact]
    public async Task Date_range_is_inclusive_on_both_ends()
    {
        await SeedVoucherAsync("000010", dateDoc: "14040101");
        await SeedVoucherAsync("000020", dateDoc: "14040215");
        await SeedVoucherAsync("000030", dateDoc: "14040401");

        var result = await RunAsync(fromDate: "14040101", toDate: "14040215");

        Assert.Equal(
            new[] { "000020", "000010" },
            result.Items.Select(i => i.VoucherNumber));
    }

    [Fact]
    public async Task Filters_by_atf_number_range()
    {
        await SeedVoucherAsync("000010", atfNum: "A100");
        await SeedVoucherAsync("000020", atfNum: "A300");
        await SeedVoucherAsync("000030", atfNum: "A500");

        var result = await RunAsync(fromAtfNo: "A200", toAtfNo: "A400");

        Assert.Equal("000020", Assert.Single(result.Items).VoucherNumber);
    }

    [Fact]
    public async Task Filters_by_doc_life()
    {
        await SeedVoucherAsync("000010", docLife: DocLife.Draft);
        await SeedVoucherAsync("000020", docLife: DocLife.Accepted);

        var result = await RunAsync(docLife: (int)DocLife.Accepted);

        Assert.Equal("000020", Assert.Single(result.Items).VoucherNumber);
    }

    [Fact]
    public async Task Filters_by_system_type_and_description_substring()
    {
        var otherSystem = Guid.NewGuid();
        await using (var context = CreateContext())
        {
            context.TB_SYSTYPEs.Add(new TB_SYSTYPE { ID = otherSystem, SYS_COD = "02", SYS_NAME = "حقوق و دستمزد" });
            await context.SaveChangesAsync();
        }

        await SeedVoucherAsync("000010", systemType: otherSystem, headDesc: "پرداخت فروردین");
        await SeedVoucherAsync("000020", headDesc: "خرید ملزومات");

        Assert.Equal("000010", Assert.Single((await RunAsync(systemTypeId: otherSystem)).Items).VoucherNumber);
        Assert.Equal("000020", Assert.Single((await RunAsync(description: "ملزومات")).Items).VoucherNumber);
    }

    /// <summary>
    /// The single most important behaviour of this repository. A total computed over the returned
    /// page would be arithmetic about an arbitrary slice — a number that looks authoritative and
    /// means nothing. Page 1 of 3 here must still report all three vouchers.
    /// </summary>
    [Fact]
    public async Task Totals_cover_the_whole_filtered_set_not_the_page()
    {
        await SeedVoucherAsync("000010", debtor: 100m, creditor: 100m, dateDoc: "14040101");
        await SeedVoucherAsync("000020", debtor: 200m, creditor: 200m, dateDoc: "14040102");
        await SeedVoucherAsync("000030", debtor: 300m, creditor: 300m, dateDoc: "14040103");

        var result = await RunAsync(pageNumber: 1, pageSize: 1);

        Assert.Single(result.Items);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(600m, result.TotalDebtor);
        Assert.Equal(600m, result.TotalCreditor);
    }

    [Fact]
    public async Task Totals_exclude_rows_the_filter_removed()
    {
        await SeedVoucherAsync("000010", debtor: 100m, creditor: 100m);
        await SeedVoucherAsync("000020", debtor: 200m, creditor: 200m, vahedCode: "0002");
        await SeedVoucherAsync("000030", debtor: 300m, creditor: 300m, isDeleted: true);

        var result = await RunAsync();

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(100m, result.TotalDebtor);
    }

    /// <summary>
    /// Nothing in this system rejects an unbalanced voucher (risk #3), so counting them across the
    /// whole selection is the only control there is — and it has to be a count of the selection,
    /// not of the page, to answer "do any of my vouchers not balance?".
    /// </summary>
    [Fact]
    public async Task Counts_unbalanced_vouchers_across_the_whole_set()
    {
        await SeedVoucherAsync("000010", debtor: 100m, creditor: 100m, dateDoc: "14040101");
        await SeedVoucherAsync("000020", debtor: 900m, creditor: 100m, dateDoc: "14040102");
        await SeedVoucherAsync("000030", debtor: 300m, creditor: 300m, dateDoc: "14040103");

        var result = await RunAsync(pageNumber: 1, pageSize: 1);

        Assert.Equal(1, result.UnbalancedCount);
    }

    [Fact]
    public async Task An_unbalanced_voucher_shows_both_sides_as_they_are()
    {
        await SeedVoucherAsync("000010", debtor: 900m, creditor: 100m);

        var row = Assert.Single((await RunAsync()).Items);

        Assert.Equal(900m, row.Debtor);
        Assert.Equal(100m, row.Creditor);
    }

    [Fact]
    public async Task A_voucher_with_no_lines_reports_zero_rather_than_disappearing()
    {
        await SeedVoucherAsync("000010", lines: Array.Empty<(decimal, decimal, bool)>());

        var row = Assert.Single((await RunAsync()).Items);

        Assert.Equal(0m, row.Debtor);
        Assert.Equal(0m, row.Creditor);
    }

    [Fact]
    public async Task Empty_result_reports_zero_figures_rather_than_failing()
    {
        var result = await RunAsync();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0m, result.TotalDebtor);
        Assert.Equal(0m, result.TotalCreditor);
        Assert.Equal(0, result.UnbalancedCount);
    }

    [Fact]
    public async Task Second_page_skips_the_first()
    {
        await SeedVoucherAsync("000010", dateDoc: "14040101");
        await SeedVoucherAsync("000020", dateDoc: "14040201");
        await SeedVoucherAsync("000030", dateDoc: "14040301");

        var result = await RunAsync(pageNumber: 2, pageSize: 1);

        Assert.Equal("000020", Assert.Single(result.Items).VoucherNumber);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(3, result.TotalCount);
    }
}
