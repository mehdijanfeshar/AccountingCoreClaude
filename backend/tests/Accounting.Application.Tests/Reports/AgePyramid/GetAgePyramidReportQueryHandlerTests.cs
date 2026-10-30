using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.AgePyramid;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Reports.AgePyramid;

/// <summary>
/// هرم سنی (تعریف صاحب پروژه ۲۰۲۶-۱۰-۲۹، نه گزارش یک‌سالهٔ مرجع): سالِ شروع هر شناسه (از رشتهٔ شناسه
/// برای معین‌های «تاریخ»، یا اولین سند برای معین‌های «عدد»)، سن در سال گزارش، فیلتر سن ≤۱۰ سال (تصحیح
/// صاحب پروژه روی تصمیم اولیهٔ «≥۱۰»)، و ماندهٔ تجمعی هر سال از شروع تا سال گزارش (حداکثر ۱۰ ستون).
/// </summary>
public class GetAgePyramidReportQueryHandlerTests
{
    private const string Vahed = "0042";
    private static readonly Guid AccountA = Guid.NewGuid();
    private static readonly Guid AccountB = Guid.NewGuid();

    private readonly Mock<IAgePyramidReadRepository> _repo = new();

    private GetAgePyramidReportQueryHandler Handler() => new(_repo.Object);

    private void Rows(params AgePyramidRawRow[] rows)
        => _repo.Setup(r => r.GetAllYearsAsync(Vahed, It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

    private static AgePyramidRawRow Row(Guid accountId, string accCode, AttribFlag? flag, string identifier, string year, decimal debtor, decimal creditor)
        => new(accountId, accCode, "حساب " + accCode, flag, identifier, year, debtor, creditor);

    private Task<IReadOnlyList<AgePyramidRowDto>> Run(string year, IReadOnlyList<Guid>? accountIds = null, string? from = null, string? to = null)
        => Handler().Handle(new GetAgePyramidReportQuery(year, accountIds, from, to) { VahedCode = Vahed }, default);

    [Fact]
    public async Task Date_type_identifier_four_digit_prefix_is_the_start_year()
    {
        Rows(Row(AccountA, "500001", AttribFlag.Date, "1397001", "1404", 100, 0));

        var row = Assert.Single(await Run("1404"));

        Assert.Equal("1397", row.StartYear);
        Assert.Equal(7, row.Age);
    }

    [Fact]
    public async Task Date_type_identifier_two_digit_prefix_prefers_the_century_closest_to_report_year()
    {
        // «۹۷» باید ۱۳۹۷ بشود (نه ۱۴۹۷ که از سال گزارش بزرگ‌تر است)
        Rows(Row(AccountA, "500001", AttribFlag.Date, "97001", "1404", 100, 0));

        var row = Assert.Single(await Run("1404"));

        Assert.Equal("1397", row.StartYear);
    }

    [Fact]
    public async Task Date_type_identifier_two_digit_prefix_in_the_1400s_uses_the_current_century()
    {
        // «۰۲» وقتی هر دو سدهٔ ۱۳ و ۱۴ از سال گزارش کوچک‌ترند، سدهٔ جاری (۱۴۰۲) باید انتخاب شود، نه ۱۳۰۲.
        Rows(Row(AccountA, "500001", AttribFlag.Date, "02001", "1404", 100, 0));

        var row = Assert.Single(await Run("1404"));

        Assert.Equal("1402", row.StartYear);
        Assert.Equal(2, row.Age);
    }

    [Fact]
    public async Task Date_type_with_unparseable_identifier_falls_back_to_first_voucher_year()
    {
        Rows(
            Row(AccountA, "500001", AttribFlag.Date, "XYZ", "1394", 50, 0),
            Row(AccountA, "500001", AttribFlag.Date, "XYZ", "1398", 10, 0));

        var row = Assert.Single(await Run("1404"));

        Assert.Equal("1394", row.StartYear);
        Assert.Equal(10, row.Age);
    }

    [Fact]
    public async Task Number_type_identifier_always_uses_first_voucher_year_even_if_it_looks_like_a_date()
    {
        Rows(Row(AccountA, "500001", AttribFlag.Number, "1397001", "1399", 100, 0));

        var row = Assert.Single(await Run("1409"));

        Assert.Equal("1399", row.StartYear);
        Assert.Equal(10, row.Age);
    }

    [Fact]
    public async Task No_flag_on_record_behaves_like_number_type()
    {
        Rows(Row(AccountA, "500001", null, "1390001", "1396", 1, 0));

        var row = Assert.Single(await Run("1404"));

        // با FLAG خالی، رشتهٔ شناسه («۱۳۹۰…») نادیده گرفته می‌شود؛ سالِ سندِ واقعی (۱۳۹۶) ملاک است.
        Assert.Equal("1396", row.StartYear);
        Assert.Equal(8, row.Age);
    }

    [Fact]
    public async Task Identifier_embedded_year_after_first_seen_voucher_is_overridden_by_the_evidence()
    {
        // رشتهٔ شناسه می‌گوید ۱۴۰۰ شروع شده، ولی قدیمی‌ترین سند واقعی آن ۱۳۹۶ است — شواهد واقعی برنده است.
        Rows(
            Row(AccountA, "500001", AttribFlag.Date, "1400001", "1396", 5, 0),
            Row(AccountA, "500001", AttribFlag.Date, "1400001", "1404", 5, 0));

        var row = Assert.Single(await Run("1404"));

        Assert.Equal("1396", row.StartYear);
    }

    [Fact]
    public async Task Age_over_ten_is_excluded()
    {
        // تصحیح صاحب پروژه (۲۰۲۶-۱۰-۲۹): فیلتر «≤۱۰» است، نه «≥۱۰» — شناسهٔ سن ۱۲ باید حذف شود.
        Rows(Row(AccountA, "500001", AttribFlag.Date, "1392001", "1404", 100, 0));
        Assert.Empty(await Run("1404"));
    }

    [Fact]
    public async Task Age_exactly_ten_is_included()
    {
        Rows(Row(AccountA, "500001", AttribFlag.Date, "1394001", "1404", 100, 0));
        var row = Assert.Single(await Run("1404"));
        Assert.Equal(10, row.Age);
    }

    [Fact]
    public async Task Age_well_under_ten_is_included()
    {
        // پیش از تصحیح، «سن کم» حذف می‌شد (فیلتر اشتباهِ ≥۱۰)؛ حالا باید در خروجی باشد.
        Rows(Row(AccountA, "500001", AttribFlag.Date, "1401001", "1404", 100, 0));
        var row = Assert.Single(await Run("1404"));
        Assert.Equal(3, row.Age);
    }

    [Fact]
    public async Task Cumulative_balance_is_running_total_per_year_from_start_to_report_year()
    {
        Rows(
            Row(AccountA, "500001", AttribFlag.Date, "1396001", "1398", 100, 0),  // +100
            Row(AccountA, "500001", AttribFlag.Date, "1396001", "1400", 0, 30),   // -30
            Row(AccountA, "500001", AttribFlag.Date, "1396001", "1404", 50, 0));  // +50

        var row = Assert.Single(await Run("1404"));

        // دامنهٔ ۱۳۹۶..۱۴۰۴ (۹ سال، زیر سقف ۱۰ستونی) — باید کامل نمایش داده شود.
        Assert.Equal(9, row.Yearly.Count);
        Assert.Equal("1396", row.Yearly[0].Year);
        Assert.Equal(0m, row.Yearly[0].Balance); // پیش از اولین سند واقعی (۱۳۹۸) هنوز گردشی نبوده
        Assert.Equal(100m, row.Yearly.Single(y => y.Year == "1398").Balance);
        Assert.Equal(100m, row.Yearly.Single(y => y.Year == "1399").Balance); // بدون گردش، ماندهٔ قبلی تکرار می‌شود
        Assert.Equal(70m, row.Yearly.Single(y => y.Year == "1400").Balance);
        Assert.Equal("1404", row.Yearly[^1].Year);
        Assert.Equal(120m, row.Yearly[^1].Balance);
    }

    [Fact]
    public async Task Future_years_beyond_report_year_are_ignored()
    {
        Rows(
            Row(AccountA, "500001", AttribFlag.Date, "1394001", "1404", 100, 0),
            Row(AccountA, "500001", AttribFlag.Date, "1394001", "1405", 999, 0)); // سال بعد از گزارش

        var row = Assert.Single(await Run("1404"));

        Assert.Equal("1404", row.Yearly[^1].Year);
        Assert.Equal(100m, row.Yearly[^1].Balance);
        Assert.DoesNotContain(row.Yearly, y => y.Year == "1405");
    }

    [Fact]
    public async Task Yearly_breakdown_trims_to_ten_columns_even_at_the_eligibility_boundary()
    {
        // سن دقیقاً ۱۰ (مرز پذیرش) یعنی دامنهٔ واقعی ۱۱ سال (۱۳۹۴..۱۴۰۴)؛ نمایش باید به آخرین ۱۰ سال
        // (۱۳۹۵..۱۴۰۴) محدود شود — تنها حالتی که سقف ۱۰ستونی اصلاً اثر می‌کند، چون فیلترِ سن دیگر
        // اجازهٔ دامنهٔ بزرگ‌تر از ۱۱ سال را نمی‌دهد.
        Rows(Row(AccountA, "500001", AttribFlag.Date, "1394001", "1404", 500, 0));

        var row = Assert.Single(await Run("1404"));

        Assert.Equal(10, row.Age);
        Assert.Equal(10, row.Yearly.Count);
        Assert.Equal("1395", row.Yearly[0].Year);
        Assert.Equal("1404", row.Yearly[^1].Year);
        Assert.Equal(500m, row.Yearly[^1].Balance);
    }

    [Fact]
    public async Task Distinct_identifiers_of_the_same_account_are_separate_rows()
    {
        Rows(
            Row(AccountA, "500001", AttribFlag.Date, "1398001", "1404", 10, 0),
            Row(AccountA, "500001", AttribFlag.Date, "1399002", "1404", 20, 0));

        var rows = await Run("1404");

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "1398001", "1399002" }, rows.Select(r => r.Identifier).OrderBy(x => x));
    }

    [Fact]
    public async Task Filters_by_identifier_range_before_resolving_age()
    {
        Rows(
            Row(AccountA, "500001", AttribFlag.Date, "1398001", "1404", 10, 0),
            Row(AccountA, "500001", AttribFlag.Date, "1399002", "1404", 20, 0));

        var rows = await Run("1404", from: "1399000", to: "1399999");

        var row = Assert.Single(rows);
        Assert.Equal("1399002", row.Identifier);
    }

    [Fact]
    public async Task Repository_called_with_the_requested_account_filter()
    {
        var ids = new[] { AccountA, AccountB };
        Rows();
        await Run("1404", accountIds: ids);
        _repo.Verify(r => r.GetAllYearsAsync(Vahed, ids, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Results_ordered_by_account_code_then_identifier()
    {
        Rows(
            Row(AccountB, "600000", AttribFlag.Date, "1398001", "1404", 1, 0),
            Row(AccountA, "500001", AttribFlag.Date, "1398002", "1404", 1, 0),
            Row(AccountA, "500001", AttribFlag.Date, "1398001", "1404", 1, 0));

        var rows = await Run("1404");

        Assert.Equal(new[] { ("500001", "1398001"), ("500001", "1398002"), ("600000", "1398001") },
            rows.Select(r => (r.AccCode, r.Identifier)));
    }

    [Fact]
    public void Validator_requires_four_digit_year()
    {
        var v = new GetAgePyramidReportQueryValidator();
        Assert.False(v.Validate(new GetAgePyramidReportQuery("14", null, null, null)).IsValid);
        Assert.True(v.Validate(new GetAgePyramidReportQuery("1404", null, null, null)).IsValid);
    }
}
