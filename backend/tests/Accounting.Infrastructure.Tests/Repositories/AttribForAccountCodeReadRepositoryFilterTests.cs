using Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) repository-level tests for the "حساب‌های شناسه‌دار" list filters, exercising
/// the actual EF Core LINQ translation. The mocked handler tests in
/// <c>Accounting.Application.Tests</c> only prove the handler passes a filter down; none of the
/// filtering, joining or ordering logic runs there.
///
/// Two things specifically need a real query to be worth anything:
/// <list type="number">
///   <item>the معین code/name come from a JOIN through the <c>ACCOUNTCODE</c> navigation, and</item>
///   <item>the same navigation is used for both the range filter and the ORDER BY.</item>
/// </list>
///
/// Provider choice (SQLite in-memory, NOT EF Core InMemory) follows
/// <see cref="VoucherHeadReadRepositoryFilterTests"/>: only a relational provider translates the
/// string comparison and the join the same way Oracle will. The tables are created by hand rather
/// than with <c>EnsureCreated()</c> because the model declares Oracle-specific artifacts SQLite
/// cannot build.
/// </summary>
public sealed class AttribForAccountCodeReadRepositoryFilterTests : IDisposable
{
    private const string CreateTablesSql = """
        CREATE TABLE TB_ACCOUNTCODE (
            ID TEXT PRIMARY KEY,
            TYPECODE INTEGER,
            PARENTID TEXT,
            ACCCODE TEXT,
            ACCCODENAME TEXT,
            TYPEACTIVITY INTEGER,
            SOURCEANDCONSUME_ID TEXT,
            IDENTYGROUPS_ID TEXT,
            TYPEACCCODE INTEGER,
            CREATEDDATE TEXT,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER,
            MOINFORCLOSE TEXT,
            TYPEACTION INTEGER
        );

        CREATE TABLE TB_ATTRIBFORACCOUNTCODE (
            ID TEXT PRIMARY KEY,
            ACCOUNTCODE_ID TEXT,
            ATTRIBBOXNO INTEGER,
            FLAG INTEGER,
            LENATR INTEGER,
            ATTRIBSUM INTEGER,
            CONTROLID INTEGER,
            VAHEDCODE TEXT,
            YEAR TEXT,
            CREATEDDATE TEXT,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER
        );
        """;

    private const string Vahed = "1155";
    private const string OtherVahed = "9999";

    private readonly SqliteConnection _connection;

    public AttribForAccountCodeReadRepositoryFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateTablesSql);
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
    /// Seeds one account plus one attrib row pointing at it, and returns the attrib row's id.
    /// </summary>
    private Guid Seed(
        string accCode,
        string accName = "حساب تستی",
        AttribSum attribSum = AttribSum.UnSummable,
        AttribFlag flag = AttribFlag.Date,
        string year = "1404",
        string vahedCode = Vahed,
        bool isDeleted = false)
    {
        using var context = CreateContext();

        var accountId = Guid.NewGuid();
        var attribId = Guid.NewGuid();

        context.TB_ACCOUNTCODEs.Add(new TB_ACCOUNTCODE
        {
            ID = accountId,
            ACCCODE = accCode,
            ACCCODENAME = accName,
            ISDELETED = false,
        });

        context.TB_ATTRIBFORACCOUNTCODEs.Add(new TB_ATTRIBFORACCOUNTCODE
        {
            ID = attribId,
            ACCOUNTCODE_ID = accountId,
            ATTRIBBOXNO = 1,
            FLAG = flag,
            LENATR = 4,
            ATTRIBSUM = attribSum,
            // Must be set explicitly, even though the CLR property is nullable and no test here
            // cares about its value: the Fluent mapping declares CONTROLID as IsRequired() with
            // HasDefaultValueSql("null "), so EF treats it as store-generated and tries to read
            // the generated value back after INSERT. SQLite returns NULL for it and the insert
            // fails with "The data is NULL at ordinal 0".
            CONTROLID = AttribControl.NotZero,
            VAHEDCODE = vahedCode,
            YEAR = year,
            CREATEDDATE = new DateTime(2025, 1, 1),
            ADDUSERID = "tester",
            ISDELETED = isDeleted,
        });

        context.SaveChanges();
        return attribId;
    }

    private async Task<IReadOnlyList<string?>> MoinCodesAsync(AttribForAccountCodeFilter? filter)
    {
        using var context = CreateContext();
        var repository = new AttribForAccountCodeReadRepository(context);

        var result = await repository.GetPagedAsync(1, 50, Vahed, filter);

        return result.Items.Select(i => i.MoinCode).ToList();
    }

    [Fact]
    public async Task Projection_ReturnsMoinCodeAndName_FromTheJoinedAccount()
    {
        Seed("110101", "بانک ملت");

        using var context = CreateContext();
        var repository = new AttribForAccountCodeReadRepository(context);

        var result = await repository.GetPagedAsync(1, 50, Vahed);

        var row = Assert.Single(result.Items);
        Assert.Equal("110101", row.MoinCode);
        Assert.Equal("بانک ملت", row.MoinName);
    }

    [Fact]
    public async Task NoFilter_ReturnsEveryRowOfTheCallersUnit()
    {
        Seed("110101");
        Seed("220202");

        var codes = await MoinCodesAsync(null);

        Assert.Equal(new[] { "110101", "220202" }, codes);
    }

    [Fact]
    public async Task Rows_AreOrderedByMoinCode_NotInsertionOrder()
    {
        Seed("330303");
        Seed("110101");
        Seed("220202");

        var codes = await MoinCodesAsync(null);

        Assert.Equal(new[] { "110101", "220202", "330303" }, codes);
    }

    [Fact]
    public async Task MoinCodeRange_IsInclusiveOnBothEnds()
    {
        Seed("110101");
        Seed("220202");
        Seed("330303");

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(
            MoinCodeFrom: "110101",
            MoinCodeTo: "220202"));

        Assert.Equal(new[] { "110101", "220202" }, codes);
    }

    [Fact]
    public async Task MoinCodeFrom_AloneLeavesTheUpperEndOpen()
    {
        Seed("110101");
        Seed("220202");

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(MoinCodeFrom: "200000"));

        Assert.Equal(new[] { "220202" }, codes);
    }

    [Fact]
    public async Task MoinCodeTo_AloneLeavesTheLowerEndOpen()
    {
        Seed("110101");
        Seed("220202");

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(MoinCodeTo: "200000"));

        Assert.Equal(new[] { "110101" }, codes);
    }

    /// <summary>
    /// All معین codes are exactly 6 digits, so a plain lexicographic range is correct — this pins
    /// that the ordering of same-width numeric strings behaves as numeric ordering, which is the
    /// assumption the repository's comment relies on.
    /// </summary>
    [Fact]
    public async Task MoinCodeRange_OnSameWidthCodes_BehavesNumerically()
    {
        Seed("090909");
        Seed("100000");
        Seed("990000");

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(
            MoinCodeFrom: "100000",
            MoinCodeTo: "989999"));

        Assert.Equal(new[] { "100000" }, codes);
    }

    [Fact]
    public async Task AttribSumFilter_MatchesExactly()
    {
        Seed("110101", attribSum: AttribSum.Summable);
        Seed("220202", attribSum: AttribSum.UnSummable);

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(AttribSum: AttribSum.Summable));

        Assert.Equal(new[] { "110101" }, codes);
    }

    [Fact]
    public async Task FlagFilter_MatchesExactly()
    {
        Seed("110101", flag: AttribFlag.Date);
        Seed("220202", flag: AttribFlag.Number);

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(Flag: AttribFlag.Number));

        Assert.Equal(new[] { "220202" }, codes);
    }

    [Fact]
    public async Task YearFilter_MatchesExactly()
    {
        Seed("110101", year: "1403");
        Seed("220202", year: "1404");

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(Year: "1404"));

        Assert.Equal(new[] { "220202" }, codes);
    }

    [Fact]
    public async Task Filters_Combine_AsAnd()
    {
        Seed("110101", attribSum: AttribSum.Summable, year: "1404");
        Seed("220202", attribSum: AttribSum.Summable, year: "1403");
        Seed("330303", attribSum: AttribSum.UnSummable, year: "1404");

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(
            MoinCodeFrom: "100000",
            AttribSum: AttribSum.Summable,
            Year: "1404"));

        Assert.Equal(new[] { "110101" }, codes);
    }

    /// <summary>
    /// An all-null filter must behave exactly like no filter at all — "not supplied" narrows
    /// nothing. This is the case that would regress if a future edit rewrote the guards as
    /// unconditional <c>Where</c> clauses.
    /// </summary>
    [Fact]
    public async Task EmptyFilter_NarrowsNothing()
    {
        Seed("110101");
        Seed("220202");

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter());

        Assert.Equal(new[] { "110101", "220202" }, codes);
    }

    /// <summary>
    /// The unit scope is not a filter and must survive every filter combination — including one
    /// that would otherwise match the other unit's row.
    /// </summary>
    [Fact]
    public async Task UnitScope_StillApplies_WhenFiltersAreSupplied()
    {
        Seed("110101", vahedCode: Vahed);
        Seed("220202", vahedCode: OtherVahed);

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(
            MoinCodeFrom: "000000",
            MoinCodeTo: "999999"));

        Assert.Equal(new[] { "110101" }, codes);
    }

    [Fact]
    public async Task DeletedRows_AreExcluded_RegardlessOfFilters()
    {
        Seed("110101");
        Seed("220202", isDeleted: true);

        var codes = await MoinCodesAsync(new AttribForAccountCodeFilter(MoinCodeFrom: "000000"));

        Assert.Equal(new[] { "110101" }, codes);
    }

    [Fact]
    public async Task TotalCount_ReflectsTheFilter_NotThePage()
    {
        Seed("110101");
        Seed("220202");
        Seed("330303");

        using var context = CreateContext();
        var repository = new AttribForAccountCodeReadRepository(context);

        var result = await repository.GetPagedAsync(
            1, 1, Vahed, new AttribForAccountCodeFilter(MoinCodeTo: "220202"));

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
    }
}
