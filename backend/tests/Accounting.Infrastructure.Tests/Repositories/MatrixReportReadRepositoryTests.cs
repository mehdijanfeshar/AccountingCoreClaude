using Accounting.Application.Reports.MatrixReport;
using Accounting.Application.Reports.MatrixReport.GetMatrixReport;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// گزارش ماتریسی — aggregation, level pivoting, filtering and unit scoping over the
/// <c>VW_CONSOLIDATE_REPORT</c> view.
///
/// <para>
/// The view is created here as a plain SQLite table: SQLite has no equivalent of the Oracle view's
/// body, and none of it is what these tests are about. What they exercise is our own logic on top
/// of it — which column each level groups by, how balances are one-sided, and that no row escapes
/// the year/unit filter.
/// </para>
///
/// <para>
/// ⚠️ Standing limitation, and a recorded open risk (#25): SQLite cannot catch Oracle-specific
/// translation failures. It will not tell us whether this LINQ actually translates on Oracle —
/// only that the logic is right if it does.
/// </para>
/// </summary>
public sealed class MatrixReportReadRepositoryTests : IDisposable
{
    private const string CreateViewTableSql = """
        CREATE TABLE VW_CONSOLIDATE_REPORT (
            GROUPCODE TEXT, GROUPNAME TEXT,
            KOLCODE TEXT, KOLNAME TEXT,
            MOINCODE TEXT, MOINNAME TEXT,
            DEBTOR NUMERIC, CREDITOR NUMERIC,
            VOUCHERNUMBER TEXT, VOUCHERDATE TEXT,
            DOCLIFE INTEGER, SYSID TEXT,
            YEAR TEXT, VAHEDCODE TEXT, ISDELETED INTEGER,
            TAFSILICODE1 TEXT, TAFSILINAME1 TEXT,
            TAFSILICODE2 TEXT, TAFSILINAME2 TEXT,
            TAFSILICODE3 TEXT, TAFSILINAME3 TEXT,
            TAFSILICODE4 TEXT, TAFSILINAME4 TEXT,
            TAFSILICODE5 TEXT, TAFSILINAME5 TEXT,
            TAFSILICODE6 TEXT, TAFSILINAME6 TEXT,
            TAFSILICODE7 TEXT, TAFSILINAME7 TEXT
        );
        """;

    private const string Vahed = "0001";
    private const string Year = "1403";

    private readonly SqliteConnection _connection;

    public MatrixReportReadRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setup = CreateContext();
        setup.Database.ExecuteSqlRaw(CreateViewTableSql);
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LegacyDbContext(options);
    }

    private static VW_CONSOLIDATE_REPORT Row(
        decimal debtor,
        decimal creditor,
        string moinCode = "1010",
        string moinName = "بانک",
        string kolCode = "10",
        string kolName = "دارایی جاری",
        string groupCode = "1",
        string groupName = "دارایی",
        string? tafsili1Code = null,
        string? tafsili1Name = null,
        string vahedCode = Vahed,
        string year = Year,
        string voucherDate = "14030215",
        string voucherNumber = "000010",
        int? docLife = 2,
        int isDeleted = 0) => new()
        {
            GROUPCODE = groupCode,
            GROUPNAME = groupName,
            KOLCODE = kolCode,
            KOLNAME = kolName,
            MOINCODE = moinCode,
            MOINNAME = moinName,
            TAFSILICODE1 = tafsili1Code,
            TAFSILINAME1 = tafsili1Name,
            DEBTOR = debtor,
            CREDITOR = creditor,
            VOUCHERDATE = voucherDate,
            VOUCHERNUMBER = voucherNumber,
            DOCLIFE = docLife,
            YEAR = year,
            VAHEDCODE = vahedCode,
            ISDELETED = isDeleted,
        };

    /// <summary>
    /// Seeded with raw INSERTs, not <c>DbSet.AddRange</c>. The entity is keyless by design (it maps
    /// a view), and EF refuses to track a keyless type — "Only entity types with a primary key may
    /// be tracked". That refusal is correct and worth keeping: it is the same property that makes
    /// it impossible to accidentally write through this type in production code.
    /// </summary>
    private async Task SeedAsync(params VW_CONSOLIDATE_REPORT[] rows)
    {
        foreach (var row in rows)
        {
            await using var command = _connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO VW_CONSOLIDATE_REPORT
                    (GROUPCODE, GROUPNAME, KOLCODE, KOLNAME, MOINCODE, MOINNAME,
                     TAFSILICODE1, TAFSILINAME1, DEBTOR, CREDITOR,
                     VOUCHERNUMBER, VOUCHERDATE, DOCLIFE, YEAR, VAHEDCODE, ISDELETED)
                VALUES
                    ($groupCode, $groupName, $kolCode, $kolName, $moinCode, $moinName,
                     $tafsili1Code, $tafsili1Name, $debtor, $creditor,
                     $voucherNumber, $voucherDate, $docLife, $year, $vahedCode, $isDeleted)
                """;

            void Bind(string name, object? value)
                => command.Parameters.AddWithValue(name, value ?? DBNull.Value);

            Bind("$groupCode", row.GROUPCODE);
            Bind("$groupName", row.GROUPNAME);
            Bind("$kolCode", row.KOLCODE);
            Bind("$kolName", row.KOLNAME);
            Bind("$moinCode", row.MOINCODE);
            Bind("$moinName", row.MOINNAME);
            Bind("$tafsili1Code", row.TAFSILICODE1);
            Bind("$tafsili1Name", row.TAFSILINAME1);
            Bind("$debtor", row.DEBTOR);
            Bind("$creditor", row.CREDITOR);
            Bind("$voucherNumber", row.VOUCHERNUMBER);
            Bind("$voucherDate", row.VOUCHERDATE);
            Bind("$docLife", row.DOCLIFE);
            Bind("$year", row.YEAR);
            Bind("$vahedCode", row.VAHEDCODE);
            Bind("$isDeleted", row.ISDELETED ?? 0);

            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Returns just the rows, which is what most of these tests assert on. The drill-down tests
    /// below call <see cref="RunFullAsync"/> when they need the scope or the available levels too.
    /// </summary>
    private async Task<IReadOnlyList<MatrixReportRowDto>> RunAsync(
        MatrixReportLevel level,
        string? fromDate = null,
        string? toDate = null,
        string? fromVoucherNo = null,
        string? toVoucherNo = null,
        int? docLife = null,
        params MatrixReportScopeItem[] scope)
        => (await RunFullAsync(level, fromDate, toDate, fromVoucherNo, toVoucherNo, docLife, scope)).Rows;

    private async Task<MatrixReportResultDto> RunFullAsync(
        MatrixReportLevel level,
        string? fromDate = null,
        string? toDate = null,
        string? fromVoucherNo = null,
        string? toVoucherNo = null,
        int? docLife = null,
        params MatrixReportScopeItem[] scope)
    {
        using var context = CreateContext();
        var repository = new MatrixReportReadRepository(context);

        return await repository.GetAsync(new GetMatrixReportQuery(
            Year, level, scope, fromDate, toDate, fromVoucherNo, toVoucherNo, docLife, SystemTypeId: null)
        { VahedCode = Vahed });
    }

    private static MatrixReportScopeItem Step(MatrixReportLevel level, string code)
        => new() { Level = level, Code = code };

    [Fact]
    public async Task Groups_by_moin_and_sums_both_sides()
    {
        await SeedAsync(
            Row(debtor: 100, creditor: 0, moinCode: "1010"),
            Row(debtor: 50, creditor: 0, moinCode: "1010"),
            Row(debtor: 0, creditor: 70, moinCode: "1020", moinName: "صندوق"));

        var result = await RunAsync(MatrixReportLevel.Moin);

        Assert.Equal(2, result.Count);
        Assert.Equal("1010", result[0].Code);
        Assert.Equal(150, result[0].Debtor);
        Assert.Equal(0, result[0].Creditor);
        Assert.Equal("1020", result[1].Code);
        Assert.Equal(70, result[1].Creditor);
    }

    [Fact]
    public async Task The_same_rows_roll_up_when_the_level_changes()
    {
        // Two معین under one کل. The whole point of the report: the level is a pivot, not a filter.
        await SeedAsync(
            Row(debtor: 100, creditor: 0, moinCode: "1010", kolCode: "10"),
            Row(debtor: 40, creditor: 0, moinCode: "1020", kolCode: "10"));

        var byMoin = await RunAsync(MatrixReportLevel.Moin);
        var byKol = await RunAsync(MatrixReportLevel.Kol);
        var byGroup = await RunAsync(MatrixReportLevel.Group);

        Assert.Equal(2, byMoin.Count);
        Assert.Equal(140, Assert.Single(byKol).Debtor);
        Assert.Equal(140, Assert.Single(byGroup).Debtor);
    }

    [Fact]
    public async Task Balances_are_one_sided()
    {
        await SeedAsync(
            Row(debtor: 300, creditor: 120, moinCode: "1010"),
            Row(debtor: 20, creditor: 90, moinCode: "1020"));

        var result = await RunAsync(MatrixReportLevel.Moin);

        // Exactly one of the two balance columns is non-zero for any row — that is what lets the
        // report render as two columns instead of one signed number.
        Assert.Equal(180, result[0].DebtorBalance);
        Assert.Equal(0, result[0].CreditorBalance);
        Assert.Equal(0, result[1].DebtorBalance);
        Assert.Equal(70, result[1].CreditorBalance);
    }

    [Fact]
    public async Task Grouping_by_a_tafsili_level_uses_that_levels_column()
    {
        await SeedAsync(
            Row(debtor: 10, creditor: 0, tafsili1Code: "T1", tafsili1Name: "مرکز الف"),
            Row(debtor: 25, creditor: 0, tafsili1Code: "T1", tafsili1Name: "مرکز الف"),
            Row(debtor: 5, creditor: 0, tafsili1Code: "T2", tafsili1Name: "مرکز ب"));

        var result = await RunAsync(MatrixReportLevel.Tafsili1);

        Assert.Equal(2, result.Count);
        Assert.Equal("T1", result[0].Code);
        Assert.Equal("مرکز الف", result[0].Name);
        Assert.Equal(35, result[0].Debtor);
        // The label names the exact level, not the family. All seven تفصیلی levels used to share
        // one «تفصیلی» label, which is harmless in a flat report and actively confusing in a
        // drill-down, where «where am I» is the question the label exists to answer.
        Assert.Equal("تفصیلی ۱", result[0].LevelLabel);
    }

    [Fact]
    public async Task Lines_with_no_code_at_the_selected_level_are_excluded_not_grouped_as_blank()
    {
        // Every row here has a معین but only one has a تفصیلی at level 1. Grouping by that level
        // must not invent an empty-titled bucket for the rest.
        await SeedAsync(
            Row(debtor: 10, creditor: 0, tafsili1Code: "T1", tafsili1Name: "مرکز الف"),
            Row(debtor: 999, creditor: 0, tafsili1Code: null),
            Row(debtor: 999, creditor: 0, tafsili1Code: ""));

        var result = await RunAsync(MatrixReportLevel.Tafsili1);

        var only = Assert.Single(result);
        Assert.Equal("T1", only.Code);
        Assert.Equal(10, only.Debtor);
    }

    [Fact]
    public async Task Another_units_rows_are_never_included()
    {
        await SeedAsync(
            Row(debtor: 10, creditor: 0, moinCode: "1010"),
            Row(debtor: 5000, creditor: 0, moinCode: "1010", vahedCode: "9999"));

        var result = await RunAsync(MatrixReportLevel.Moin);

        Assert.Equal(10, Assert.Single(result).Debtor);
    }

    [Fact]
    public async Task Another_years_rows_are_never_included()
    {
        await SeedAsync(
            Row(debtor: 10, creditor: 0),
            Row(debtor: 5000, creditor: 0, year: "1402"));

        Assert.Equal(10, Assert.Single(await RunAsync(MatrixReportLevel.Moin)).Debtor);
    }

    [Fact]
    public async Task Deleted_rows_are_excluded()
    {
        await SeedAsync(
            Row(debtor: 10, creditor: 0),
            Row(debtor: 5000, creditor: 0, isDeleted: 1));

        Assert.Equal(10, Assert.Single(await RunAsync(MatrixReportLevel.Moin)).Debtor);
    }

    [Fact]
    public async Task The_date_range_is_inclusive_on_both_bounds()
    {
        await SeedAsync(
            Row(debtor: 1, creditor: 0, moinCode: "A", voucherDate: "14030101"),
            Row(debtor: 2, creditor: 0, moinCode: "B", voucherDate: "14030215"),
            Row(debtor: 4, creditor: 0, moinCode: "C", voucherDate: "14030331"),
            Row(debtor: 8, creditor: 0, moinCode: "D", voucherDate: "14030401"));

        var result = await RunAsync(MatrixReportLevel.Moin, fromDate: "14030215", toDate: "14030331");

        Assert.Equal(["B", "C"], result.Select(r => r.Code).ToArray());
    }

    [Fact]
    public async Task The_voucher_number_range_is_inclusive_on_both_bounds()
    {
        await SeedAsync(
            Row(debtor: 1, creditor: 0, moinCode: "A", voucherNumber: "000005"),
            Row(debtor: 2, creditor: 0, moinCode: "B", voucherNumber: "000010"),
            Row(debtor: 4, creditor: 0, moinCode: "C", voucherNumber: "000020"));

        var result = await RunAsync(MatrixReportLevel.Moin, fromVoucherNo: "000010", toVoucherNo: "000020");

        Assert.Equal(["B", "C"], result.Select(r => r.Code).ToArray());
    }

    [Fact]
    public async Task Doc_life_filters_to_an_exact_state()
    {
        await SeedAsync(
            Row(debtor: 1, creditor: 0, moinCode: "A", docLife: 1),
            Row(debtor: 2, creditor: 0, moinCode: "B", docLife: 4));

        var result = await RunAsync(MatrixReportLevel.Moin, docLife: 4);

        Assert.Equal("B", Assert.Single(result).Code);
    }

    [Fact]
    public async Task Rows_come_back_ordered_by_code()
    {
        await SeedAsync(
            Row(debtor: 1, creditor: 0, moinCode: "3030"),
            Row(debtor: 1, creditor: 0, moinCode: "1010"),
            Row(debtor: 1, creditor: 0, moinCode: "2020"));

        var result = await RunAsync(MatrixReportLevel.Moin);

        Assert.Equal(["1010", "2020", "3030"], result.Select(r => r.Code).ToArray());
    }

    [Fact]
    public async Task An_empty_result_is_an_empty_list_not_a_null()
    {
        var result = await RunAsync(MatrixReportLevel.Moin);

        Assert.Empty(result);
    }

    // ── Drill-down ────────────────────────────────────────────────────────────────────────────
    // These are what separate گزارش ماتریسی from a second تراز آزمایشی. Without a scope, "group
    // by معین" is a flat list; with one, it is «the معین rows inside the کل I opened».

    /// <summary>
    /// از کل به جزء: opening گروه ۱ and listing کل must show only the کل rows inside it, with
    /// figures summed from that group's lines alone.
    /// </summary>
    [Fact]
    public async Task Scoping_to_a_group_shows_only_the_kols_inside_it()
    {
        await SeedAsync(
            Row(debtor: 100, creditor: 0, groupCode: "1", kolCode: "10", moinCode: "1010"),
            Row(debtor: 40, creditor: 0, groupCode: "1", kolCode: "11", moinCode: "1110"),
            Row(debtor: 900, creditor: 0, groupCode: "2", kolCode: "20", moinCode: "2010"));

        var rows = await RunAsync(MatrixReportLevel.Kol, scope: Step(MatrixReportLevel.Group, "1"));

        Assert.Equal(new[] { "10", "11" }, rows.Select(r => r.Code));
        Assert.Equal(140m, rows.Sum(r => r.Debtor));
    }

    [Fact]
    public async Task Scope_steps_compose_so_a_deeper_path_narrows_further()
    {
        await SeedAsync(
            Row(debtor: 100, creditor: 0, groupCode: "1", kolCode: "10", moinCode: "1010"),
            Row(debtor: 200, creditor: 0, groupCode: "1", kolCode: "10", moinCode: "1020"),
            Row(debtor: 300, creditor: 0, groupCode: "1", kolCode: "11", moinCode: "1110"));

        var rows = await RunAsync(
            MatrixReportLevel.Moin,
            scope: new[] { Step(MatrixReportLevel.Group, "1"), Step(MatrixReportLevel.Kol, "10") });

        Assert.Equal(new[] { "1010", "1020" }, rows.Select(r => r.Code));
    }

    /// <summary>
    /// از جزء به کل is the same mechanism read backwards — dropping the deepest step widens the
    /// report. Asserting it explicitly pins that going up is not a separate code path that could
    /// drift from going down.
    /// </summary>
    [Fact]
    public async Task Dropping_the_last_scope_step_widens_the_report_again()
    {
        await SeedAsync(
            Row(debtor: 100, creditor: 0, groupCode: "1", kolCode: "10", moinCode: "1010"),
            Row(debtor: 300, creditor: 0, groupCode: "1", kolCode: "11", moinCode: "1110"));

        var narrow = await RunAsync(
            MatrixReportLevel.Moin,
            scope: new[] { Step(MatrixReportLevel.Group, "1"), Step(MatrixReportLevel.Kol, "10") });
        var wide = await RunAsync(MatrixReportLevel.Moin, scope: Step(MatrixReportLevel.Group, "1"));

        Assert.Single(narrow);
        Assert.Equal(2, wide.Count);
    }

    [Fact]
    public async Task Drilling_works_at_tafsili_levels_too()
    {
        await SeedAsync(
            Row(debtor: 100, creditor: 0, moinCode: "1010", tafsili1Code: "T1", tafsili1Name: "مرکز الف"),
            Row(debtor: 250, creditor: 0, moinCode: "1010", tafsili1Code: "T2", tafsili1Name: "مرکز ب"));

        var rows = await RunAsync(MatrixReportLevel.Tafsili1, scope: Step(MatrixReportLevel.Moin, "1010"));

        Assert.Equal(new[] { "T1", "T2" }, rows.Select(r => r.Code));
        Assert.Equal("مرکز الف", rows[0].Name);
    }

    /// <summary>
    /// A row only offers a drill-down when something is actually there. Offering one that lands on
    /// an empty table reads as a broken report, and a معین with no تفصیلی assignment is normal.
    /// </summary>
    [Fact]
    public async Task HasChildren_is_false_when_nothing_is_assigned_at_the_next_level()
    {
        await SeedAsync(
            Row(debtor: 100, creditor: 0, moinCode: "1010", tafsili1Code: "T1"),
            Row(debtor: 100, creditor: 0, moinCode: "1020", tafsili1Code: null));

        var rows = await RunAsync(MatrixReportLevel.Moin);

        Assert.True(rows.Single(r => r.Code == "1010").HasChildren);
        Assert.False(rows.Single(r => r.Code == "1020").HasChildren);
    }

    [Fact]
    public async Task The_deepest_tafsili_level_has_nothing_below_it()
    {
        await SeedAsync(Row(debtor: 100, creditor: 0, moinCode: "1010", tafsili1Code: "T1"));

        var rows = await RunAsync(MatrixReportLevel.Tafsili7);

        // No TAFSILICODE7 was seeded, so there is nothing to list — and nothing can claim depth
        // below the deepest level either.
        Assert.Empty(rows);
    }

    /// <summary>
    /// The answer to «کدام سطوح؟»: levels with no data inside the current scope are not offered.
    /// </summary>
    [Fact]
    public async Task Available_levels_lists_only_levels_that_carry_data_in_scope()
    {
        await SeedAsync(
            Row(debtor: 100, creditor: 0, groupCode: "1", kolCode: "10", moinCode: "1010", tafsili1Code: "T1"));

        var result = await RunFullAsync(MatrixReportLevel.Moin);

        Assert.Contains(MatrixReportLevel.Group, result.AvailableLevels);
        Assert.Contains(MatrixReportLevel.Moin, result.AvailableLevels);
        Assert.Contains(MatrixReportLevel.Tafsili1, result.AvailableLevels);
        Assert.DoesNotContain(MatrixReportLevel.Tafsili2, result.AvailableLevels);
    }

    [Fact]
    public async Task Available_levels_narrows_with_the_scope()
    {
        await SeedAsync(
            Row(debtor: 100, creditor: 0, groupCode: "1", kolCode: "10", moinCode: "1010", tafsili1Code: "T1"),
            Row(debtor: 100, creditor: 0, groupCode: "2", kolCode: "20", moinCode: "2010", tafsili1Code: null));

        var inGroupTwo = await RunFullAsync(MatrixReportLevel.Moin, scope: Step(MatrixReportLevel.Group, "2"));

        Assert.DoesNotContain(MatrixReportLevel.Tafsili1, inGroupTwo.AvailableLevels);
    }

    [Fact]
    public async Task Scope_comes_back_with_names_for_the_breadcrumb()
    {
        await SeedAsync(
            Row(
                debtor: 100,
                creditor: 0,
                groupCode: "1",
                groupName: "دارایی",
                kolCode: "10",
                kolName: "دارایی جاری",
                moinCode: "1010"));

        var result = await RunFullAsync(
            MatrixReportLevel.Moin,
            scope: new[] { Step(MatrixReportLevel.Kol, "10"), Step(MatrixReportLevel.Group, "1") });

        // Returned shallowest-first regardless of the order the caller sent, because a breadcrumb
        // that rendered گروه after کل would read as a different hierarchy than the data has.
        Assert.Equal(new[] { "1", "10" }, result.Scope.Select(s => s.Code));
        Assert.Equal(new[] { "دارایی", "دارایی جاری" }, result.Scope.Select(s => s.Name));
    }

    /// <summary>
    /// A mistyped code must still appear in the breadcrumb. Dropping it would make the path the
    /// user is looking at disagree with the path they asked for.
    /// </summary>
    [Fact]
    public async Task A_scope_code_that_matches_nothing_is_echoed_with_an_empty_name()
    {
        await SeedAsync(Row(debtor: 100, creditor: 0, groupCode: "1", moinCode: "1010"));

        var result = await RunFullAsync(MatrixReportLevel.Moin, scope: Step(MatrixReportLevel.Group, "9"));

        Assert.Empty(result.Rows);
        var step = Assert.Single(result.Scope);
        Assert.Equal("9", step.Code);
        Assert.Equal(string.Empty, step.Name);
    }

    [Fact]
    public async Task Scope_does_not_escape_the_unit_or_year_filter()
    {
        await SeedAsync(
            Row(debtor: 100, creditor: 0, groupCode: "1", moinCode: "1010"),
            Row(debtor: 500, creditor: 0, groupCode: "1", moinCode: "1010", vahedCode: "0002"),
            Row(debtor: 700, creditor: 0, groupCode: "1", moinCode: "1010", year: "1402"));

        var rows = await RunAsync(MatrixReportLevel.Moin, scope: Step(MatrixReportLevel.Group, "1"));

        Assert.Equal(100m, Assert.Single(rows).Debtor);
    }

    [Fact]
    public async Task Level_and_label_are_echoed_back()
    {
        await SeedAsync(Row(debtor: 100, creditor: 0, moinCode: "1010"));

        var result = await RunFullAsync(MatrixReportLevel.Moin);

        Assert.Equal(MatrixReportLevel.Moin, result.Level);
        Assert.Equal("معین", result.LevelLabel);
    }
}
