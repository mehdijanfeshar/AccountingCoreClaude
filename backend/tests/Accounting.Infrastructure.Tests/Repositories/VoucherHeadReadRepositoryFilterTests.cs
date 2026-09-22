using Accounting.Application.Vouchers.Queries.GetVoucherHeads;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) repository-level tests for the <see cref="VoucherHeadFilter"/> handling in
/// <see cref="VoucherHeadReadRepository"/>, exercising the actual EF Core LINQ translation. The
/// mocked handler tests in <c>Accounting.Application.Tests</c> only prove the handler passes a
/// filter down; none of the filtering logic runs there.
///
/// The <c>DOC_NUM</c> range is the reason this file exists: that column is a *string* holding
/// numbers of mixed width (live Oracle data has both <c>"054354"</c> and <c>"20"</c>), so a
/// naive lexicographic range silently returns the wrong rows.
///
/// Provider choice (SQLite in-memory, NOT EF Core InMemory) and the hand-written <c>CREATE
/// TABLE</c> workaround follow <c>TafsiliLookupReadRepositoryTests</c> verbatim in rationale.
/// Same standing limitation: SQLite, never live Oracle.
/// </summary>
public sealed class VoucherHeadReadRepositoryFilterTests : IDisposable
{
    private const string CreateVoucherHeadTableSql = """
        CREATE TABLE TB_VOUCHERSHEAD (
            ID TEXT PRIMARY KEY,
            DOC_NUM TEXT,
            DATE_DOC TEXT,
            DOCLIFE INTEGER,
            HEAD_DESC TEXT,
            APENDIX TEXT,
            SYSTEM_TYPE TEXT,
            FLAG_STATE TEXT,
            CREATEDDATE TEXT,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT,
            CHANGEUSERID TEXT,
            VAHEDCODE TEXT,
            YEAR TEXT,
            ISDELETED INTEGER,
            ATTACHFILE BLOB,
            ATTACHFILE_NAME TEXT,
            ATF_NUM TEXT,
            ISAUTOMATIC INTEGER,
            SNDVAHEDCODE TEXT,
            PARENTHEAD_ID TEXT,
            GLOBALNUMBER TEXT
        );
        """;

    private const string Vahed = "1155";

    private readonly SqliteConnection _connection;

    public VoucherHeadReadRepositoryFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateVoucherHeadTableSql);
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LegacyDbContext(options);
    }

    private static TB_VOUCHERSHEAD Head(
        string docNum,
        string dateDoc,
        string year = "1404",
        string vahedCode = Vahed,
        Guid? systemType = null,
        bool isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        DOC_NUM = docNum,
        DATE_DOC = dateDoc,
        YEAR = year,
        VAHEDCODE = vahedCode,
        SYSTEM_TYPE = systemType,
        ISDELETED = isDeleted,
    };

    private async Task SeedAsync(params TB_VOUCHERSHEAD[] rows)
    {
        await using var context = CreateContext();
        context.TB_VOUCHERSHEADs.AddRange(rows);
        await context.SaveChangesAsync();
    }

    private async Task<IReadOnlyList<string?>> DocNumsAsync(VoucherHeadFilter filter)
    {
        await using var context = CreateContext();
        var repository = new VoucherHeadReadRepository(context);
        var result = await repository.GetPagedAsync(1, 50, filter, Vahed);
        return result.Items.Select(i => i.DocNum).ToList();
    }

    [Fact]
    public async Task NoFilter_ReturnsEveryNonDeletedRowOfTheCallersUnit()
    {
        await SeedAsync(
            Head("000001", "14040101"),
            Head("000002", "14040102"),
            Head("000003", "14040103", isDeleted: true),
            Head("000004", "14040104", vahedCode: "0000"));

        var docNums = await DocNumsAsync(new VoucherHeadFilter());

        // Newest first (by DATE_DOC) — see Ordering_PutsTheNewestVoucherFirst below.
        Assert.Equal(new[] { "000002", "000001" }, docNums);
    }

    [Fact]
    public async Task DateDocRange_IsInclusiveOnBothBounds()
    {
        await SeedAsync(
            Head("000001", "14040101"),
            Head("000002", "14040215"),
            Head("000003", "14040331"),
            Head("000004", "14040401"));

        var docNums = await DocNumsAsync(new VoucherHeadFilter(DateDocFrom: "14040215", DateDocTo: "14040331"));

        // Both bounds are included; the pair comes back newest-first.
        Assert.Equal(new[] { "000003", "000002" }, docNums);
    }

    [Fact]
    public async Task Ordering_PutsTheNewestVoucherFirst()
    {
        // Seeded deliberately out of order, and with DOC_NUM running opposite to DATE_DOC, so a
        // result ordered by number would be visibly different from one ordered by date.
        await SeedAsync(
            Head("000001", "14040310"),
            Head("000002", "14040115"),
            Head("000003", "14040220"));

        var docNums = await DocNumsAsync(new VoucherHeadFilter());

        // The cartable is a work queue: the voucher someone needs is almost always one of the
        // most recent, never 000001 from last Farvardin.
        Assert.Equal(new[] { "000001", "000003", "000002" }, docNums);
    }

    [Fact]
    public async Task Ordering_BreaksTiesOnTheSameDateByDocNumDescending()
    {
        await SeedAsync(
            Head("000007", "14040101"),
            Head("000009", "14040101"),
            Head("000008", "14040101"));

        var docNums = await DocNumsAsync(new VoucherHeadFilter());

        Assert.Equal(new[] { "000009", "000008", "000007" }, docNums);
    }

    [Fact]
    public async Task DocNumRange_OrdersNumerically_NotAlphabetically()
    {
        // "20" is lexicographically AFTER "054354" but numerically far below it. A naive string
        // range (DOC_NUM >= "9" AND DOC_NUM <= "100") would include "20" and exclude "054354";
        // the length-then-lexicographic comparison must do the opposite.
        await SeedAsync(
            Head("9", "14040101"),
            Head("20", "14040102"),
            Head("100", "14040103"),
            Head("054354", "14040104"));

        var docNums = await DocNumsAsync(new VoucherHeadFilter(DocNumFrom: "9", DocNumTo: "100"));

        // Membership is what this test is about; row ORDER is the repository's fixed
        // YEAR/VAHEDCODE/DOC_NUM sort, which is documented as alphabetical and unchanged here.
        Assert.Equal(3, docNums.Count);
        Assert.Contains("9", docNums);
        Assert.Contains("20", docNums);
        Assert.Contains("100", docNums);
        Assert.DoesNotContain("054354", docNums);
    }

    [Fact]
    public async Task DocNumRange_MatchesZeroPaddedValuesOfTheSameWidth()
    {
        await SeedAsync(
            Head("054334", "14040101"),
            Head("054350", "14040102"),
            Head("054354", "14040103"));

        var docNums = await DocNumsAsync(new VoucherHeadFilter(DocNumFrom: "054340", DocNumTo: "054353"));

        Assert.Equal(new[] { "054350" }, docNums);
    }

    [Fact]
    public async Task SystemTypeId_FiltersOnExactMatch()
    {
        var accounting = Guid.NewGuid();
        var payRecive = Guid.NewGuid();
        await SeedAsync(
            Head("000001", "14040101", systemType: accounting),
            Head("000002", "14040102", systemType: payRecive),
            Head("000003", "14040103", systemType: null));

        var docNums = await DocNumsAsync(new VoucherHeadFilter(SystemTypeId: accounting));

        Assert.Equal(new[] { "000001" }, docNums);
    }

    [Fact]
    public async Task Filters_Combine_AsAnd()
    {
        var accounting = Guid.NewGuid();
        await SeedAsync(
            Head("000001", "14040101", year: "1404", systemType: accounting),
            Head("000002", "14040102", year: "1404", systemType: accounting),
            Head("000003", "14040102", year: "1405", systemType: accounting),
            Head("000004", "14040102", year: "1404", systemType: Guid.NewGuid()));

        var docNums = await DocNumsAsync(new VoucherHeadFilter(
            Year: "1404",
            DocNumFrom: "000002",
            DateDocFrom: "14040102",
            SystemTypeId: accounting));

        Assert.Equal(new[] { "000002" }, docNums);
    }
}
