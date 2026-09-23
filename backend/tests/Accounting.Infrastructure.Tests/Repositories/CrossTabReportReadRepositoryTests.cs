using Accounting.Application.Reports.CrossTab;
using Accounting.Application.Reports.CrossTab.GetCrossTabReport;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// گزارش متقاطع — the two-axis pivot over <c>VW_CONSOLIDATE_REPORT</c>.
///
/// <para>
/// The view is created as a plain SQLite table, exactly as
/// <see cref="MatrixReportReadRepositoryTests"/> does: SQLite has no equivalent of the Oracle
/// view's body and none of it is what these tests are about. What they exercise is our own logic
/// on top of it — which column each axis groups by, that the dynamically-composed projection
/// really is translatable, how sparse cells and the column cap behave, and that no row escapes the
/// year/unit filter.
/// </para>
///
/// <para>
/// ⚠️ Standing limitation, and a recorded open risk (#25): SQLite cannot catch Oracle-specific
/// translation failures. It proves the logic, not the Oracle translation. The Oracle side of this
/// report was checked separately by running the equivalent aggregate against the live view.
/// </para>
/// </summary>
public sealed class CrossTabReportReadRepositoryTests : IDisposable
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

    public CrossTabReportReadRepositoryTests()
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

    /// <summary>
    /// Seeded with raw INSERTs, not <c>DbSet.AddRange</c>. The entity is keyless by design (it maps
    /// a view), and EF refuses to track a keyless type — "Only entity types with a primary key may
    /// be tracked". That refusal is correct and worth keeping: it is the same property that makes
    /// it impossible to accidentally write through this type in production code.
    ///
    /// <para>
    /// ⚠️ <c>ISDELETED</c> is bound as-is, including null. The sibling matrix-report seeder coerces
    /// null to 0, which would make <see cref="GetAsync_TreatsNullIsDeletedAsLive"/> pass for the
    /// wrong reason — it would never exercise a null at all.
    /// </para>
    /// </summary>
    private void Seed(params VW_CONSOLIDATE_REPORT[] rows)
    {
        foreach (var row in rows)
        {
            using var command = _connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO VW_CONSOLIDATE_REPORT
                    (GROUPCODE, GROUPNAME, KOLCODE, KOLNAME, MOINCODE, MOINNAME,
                     TAFSILICODE1, TAFSILINAME1, DEBTOR, CREDITOR,
                     VOUCHERDATE, DOCLIFE, YEAR, VAHEDCODE, ISDELETED)
                VALUES
                    ($groupCode, $groupName, $kolCode, $kolName, $moinCode, $moinName,
                     $tafsili1Code, $tafsili1Name, $debtor, $creditor,
                     $voucherDate, $docLife, $year, $vahedCode, $isDeleted)
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
            Bind("$voucherDate", row.VOUCHERDATE);
            Bind("$docLife", row.DOCLIFE);
            Bind("$year", row.YEAR);
            Bind("$vahedCode", row.VAHEDCODE);
            Bind("$isDeleted", row.ISDELETED);

            command.ExecuteNonQuery();
        }
    }

    private static VW_CONSOLIDATE_REPORT Row(
        string? tafsili1,
        string? moin,
        decimal debtor,
        decimal creditor,
        string? year = Year,
        string? vahed = Vahed,
        int? isDeleted = 0,
        string? voucherDate = "14030515",
        int? docLife = 1,
        string? kol = "11",
        string? group = "1")
        => new()
        {
            GROUPCODE = group,
            GROUPNAME = group is null ? null : $"گروه {group}",
            KOLCODE = kol,
            KOLNAME = kol is null ? null : $"کل {kol}",
            MOINCODE = moin,
            MOINNAME = moin is null ? null : $"معین {moin}",
            TAFSILICODE1 = tafsili1,
            TAFSILINAME1 = tafsili1 is null ? null : $"تفصیلی {tafsili1}",
            DEBTOR = debtor,
            CREDITOR = creditor,
            YEAR = year,
            VAHEDCODE = vahed,
            ISDELETED = isDeleted,
            VOUCHERDATE = voucherDate,
            DOCLIFE = docLife,
        };

    private static GetCrossTabReportQuery Query(
        CrossTabDimension row = CrossTabDimension.Tafsili1,
        CrossTabDimension column = CrossTabDimension.Moin,
        string? fromDate = null,
        string? toDate = null,
        int? docLife = null,
        string? rowFilter = null,
        string? columnFilter = null)
        => new(Year, row, column, fromDate, toDate, docLife, null, rowFilter, columnFilter)
        {
            VahedCode = Vahed,
        };

    private async Task<CrossTabResultDto> RunAsync(GetCrossTabReportQuery query)
    {
        using var context = CreateContext();
        return await new CrossTabReportReadRepository(context).GetAsync(query);
    }

    /// <summary>
    /// The core shape: two tafsili rows crossed against two معین columns, each intersection summed
    /// independently. This is the whole point of the report and is what گزارش ماتریسی cannot do.
    /// </summary>
    [Fact]
    public async Task GetAsync_CrossesTheTwoDimensions()
    {
        Seed(
            Row("T1", "110101", 100, 0),
            Row("T1", "110101", 50, 0),
            Row("T1", "220202", 0, 30),
            Row("T2", "110101", 7, 0));

        var result = await RunAsync(Query());

        Assert.Equal(["110101", "220202"], result.Columns.Select(c => c.Code));
        Assert.Equal(["T1", "T2"], result.Rows.Select(r => r.Code));

        var t1 = result.Rows.Single(r => r.Code == "T1");
        Assert.Equal(150m, t1.Cells.Single(c => c.ColumnCode == "110101").Debtor);
        Assert.Equal(30m, t1.Cells.Single(c => c.ColumnCode == "220202").Creditor);

        var t2 = result.Rows.Single(r => r.Code == "T2");
        Assert.Equal(7m, Assert.Single(t2.Cells).Debtor);
    }

    /// <summary>
    /// Empty intersections are omitted, not sent as zeroes. A cross-tab is nearly always sparse —
    /// on live data 8 rows × 8 columns held 15 populated cells — so materialising the full grid
    /// would spend most of the payload on values the client can infer.
    /// </summary>
    [Fact]
    public async Task GetAsync_OmitsEmptyIntersections()
    {
        Seed(Row("T1", "110101", 100, 0), Row("T2", "220202", 0, 40));

        var result = await RunAsync(Query());

        Assert.Equal(2, result.Columns.Count);
        Assert.All(result.Rows, r => Assert.Single(r.Cells));
    }

    /// <summary>
    /// Either axis can be any dimension — the projection is composed from the enum, not written
    /// out per pair. Crossing گروه against کل must group by those columns and nothing else.
    /// </summary>
    [Fact]
    public async Task GetAsync_SupportsAnyDimensionPair()
    {
        Seed(
            Row("T1", "110101", 10, 0, group: "1", kol: "11"),
            Row("T2", "220202", 20, 0, group: "1", kol: "12"),
            Row("T3", "330303", 5, 0, group: "2", kol: "21"));

        var result = await RunAsync(Query(CrossTabDimension.Group, CrossTabDimension.Kol));

        Assert.Equal("گروه", result.RowDimensionLabel);
        Assert.Equal("کل", result.ColumnDimensionLabel);
        Assert.Equal(["1", "2"], result.Rows.Select(r => r.Code));
        Assert.Equal(["11", "12", "21"], result.Columns.Select(c => c.Code));

        var group1 = result.Rows.Single(r => r.Code == "1");
        Assert.Equal(2, group1.Cells.Count);
        Assert.Equal(30m, group1.Debtor);
    }

    /// <summary>
    /// A line with no code on either axis is absence, not a category. With two axes an unfiltered
    /// null would create both a blank row and a blank column, so each side is filtered
    /// independently.
    /// </summary>
    [Fact]
    public async Task GetAsync_ExcludesRowsMissingACodeOnEitherAxis()
    {
        Seed(
            Row("T1", "110101", 100, 0),
            Row(null, "110101", 999, 0),
            Row("T2", null, 888, 0),
            Row("", "110101", 777, 0));

        var result = await RunAsync(Query());

        Assert.Equal("T1", Assert.Single(result.Rows).Code);
        Assert.Equal(100m, result.Debtor);
    }

    [Fact]
    public async Task GetAsync_ComputesRowColumnAndGrandTotals()
    {
        Seed(
            Row("T1", "110101", 100, 1),
            Row("T1", "220202", 20, 2),
            Row("T2", "110101", 5, 3));

        var result = await RunAsync(Query());

        Assert.Equal(125m, result.Debtor);
        Assert.Equal(6m, result.Creditor);

        Assert.Equal(120m, result.Rows.Single(r => r.Code == "T1").Debtor);
        Assert.Equal(105m, result.Columns.Single(c => c.Code == "110101").Debtor);
        // Both rows contribute to this column: 1 from T1 and 3 from T2.
        Assert.Equal(4m, result.Columns.Single(c => c.Code == "110101").Creditor);
    }

    [Fact]
    public async Task GetAsync_ExcludesOtherYearsUnitsAndDeletedRows()
    {
        Seed(
            Row("T1", "110101", 100, 0),
            Row("T9", "110101", 1, 0, year: "1402"),
            Row("T8", "110101", 1, 0, vahed: "9999"),
            Row("T7", "110101", 1, 0, isDeleted: 1));

        var result = await RunAsync(Query());

        Assert.Equal("T1", Assert.Single(result.Rows).Code);
        Assert.Equal(100m, result.Debtor);
    }

    /// <summary>
    /// <c>ISDELETED</c> is compared as the number it is; a NULL must count as "not deleted", which
    /// is also how the matrix report treats it.
    /// </summary>
    [Fact]
    public async Task GetAsync_TreatsNullIsDeletedAsLive()
    {
        Seed(Row("T1", "110101", 100, 0, isDeleted: null));

        var result = await RunAsync(Query());

        Assert.Equal(100m, result.Debtor);
    }

    [Fact]
    public async Task GetAsync_AppliesDateAndDocLifeFilters()
    {
        Seed(
            Row("T1", "110101", 10, 0, voucherDate: "14030101", docLife: 1),
            Row("T2", "110101", 20, 0, voucherDate: "14030615", docLife: 1),
            Row("T3", "110101", 40, 0, voucherDate: "14030615", docLife: 4));

        var byDate = await RunAsync(Query(fromDate: "14030201"));
        Assert.Equal(60m, byDate.Debtor);

        var byDocLife = await RunAsync(Query(docLife: 4));
        Assert.Equal(40m, byDocLife.Debtor);
    }

    /// <summary>
    /// The row/column «شروع با» narrowings are the intended way to tame a wide pivot — they must
    /// apply to the right axis and nothing else.
    /// </summary>
    [Fact]
    public async Task GetAsync_AppliesRowAndColumnCodeFilters()
    {
        Seed(
            Row("A1", "110101", 10, 0),
            Row("A2", "220202", 20, 0),
            Row("B1", "110101", 40, 0));

        var byRow = await RunAsync(Query(rowFilter: "A"));
        Assert.Equal(["A1", "A2"], byRow.Rows.Select(r => r.Code));

        var byColumn = await RunAsync(Query(columnFilter: "1101"));
        Assert.Equal(["110101"], byColumn.Columns.Select(c => c.Code));
        Assert.Equal(["A1", "B1"], byColumn.Rows.Select(r => r.Code));
    }

    /// <summary>
    /// When there are more columns than the cap allows, the widest are kept — if the grid cannot
    /// show everything, the columns worth keeping are the ones carrying the most turnover, not the
    /// ones that happen to sort first. The kept set is still presented in code order.
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenColumnsExceedTheCap_KeepsTheWidestAndFlagsTruncation()
    {
        var rows = Enumerable.Range(0, GetCrossTabReportQueryValidator.MaxColumns + 5)
            .Select(i => Row("T1", $"C{i:D4}", i + 1, 0))
            .ToArray();
        Seed(rows);

        var result = await RunAsync(Query());

        Assert.True(result.ColumnsTruncated);
        Assert.Equal(rows.Length, result.TotalColumnCount);
        Assert.Equal(GetCrossTabReportQueryValidator.MaxColumns, result.Columns.Count);

        // The five smallest were dropped, and what is kept is still in code order.
        Assert.DoesNotContain(result.Columns, c => c.Code == "C0000");
        Assert.Contains(result.Columns, c => c.Code == $"C{rows.Length - 1:D4}");
        Assert.Equal(result.Columns.Select(c => c.Code).Order(StringComparer.Ordinal), result.Columns.Select(c => c.Code));
    }

    /// <summary>
    /// The grand total must still describe the WHOLE filtered set when columns were trimmed. A
    /// total silently re-based onto the visible slice would disagree with the trial balance for the
    /// same period, and would make a truncated report look complete.
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenTruncated_GrandTotalStillCoversEverything()
    {
        var rows = Enumerable.Range(0, GetCrossTabReportQueryValidator.MaxColumns + 5)
            .Select(i => Row("T1", $"C{i:D4}", 1, 0))
            .ToArray();
        Seed(rows);

        var result = await RunAsync(Query());

        Assert.True(result.ColumnsTruncated);
        Assert.Equal(rows.Length, result.Debtor);
        Assert.Equal(rows.Length, result.Rows.Single().Debtor);
    }

    [Fact]
    public async Task GetAsync_WithNoMatchingRows_ReturnsEmptyGridNotAnError()
    {
        Seed(Row("T1", "110101", 100, 0, year: "1399"));

        var result = await RunAsync(Query());

        Assert.Empty(result.Rows);
        Assert.Empty(result.Columns);
        Assert.Equal(0m, result.Debtor);
        Assert.False(result.ColumnsTruncated);
    }
}
