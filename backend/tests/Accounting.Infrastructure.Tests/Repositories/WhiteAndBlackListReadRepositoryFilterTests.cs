using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) repository tests for the «دسترسی کدینگ حسابداری» list, exercising the actual
/// EF Core LINQ translation. The mocked handler tests in <c>Accounting.Application.Tests</c> only
/// prove the handler hands a filter down — none of the filtering, joining or ordering runs there.
///
/// Three things specifically need a real query to mean anything:
/// <list type="number">
///   <item>the معین code/name and unit-type name come from JOINs through the
///   <c>ACCOUNTCODE</c>/<c>VAHEDTYPE</c> navigations, and the latter must be a LEFT join because
///   <c>VAHEDTYPE_ID</c> is nullable;</item>
///   <item>the four date bounds go through <see cref="string.Compare(string, string)"/>, which is
///   only useful if the provider translates it to SQL instead of evaluating it client-side; and</item>
///   <item><c>GetExistingKeysAsync</c> projects into a <c>record struct</c>, which not every
///   projection shape supports.</item>
/// </list>
///
/// Provider choice (SQLite in-memory, NOT EF Core InMemory) follows
/// <see cref="AttribForAccountCodeReadRepositoryFilterTests"/>: only a relational provider
/// translates comparisons and joins the way Oracle will. Tables are created by hand because the
/// model declares Oracle-specific artifacts SQLite cannot build.
///
/// ⚠️ <b>What this does NOT prove</b> (recorded risk #25): SQLite is blind to Oracle-specific
/// translation faults. It has bitten this project twice — <c>NUMBER(1)</c>⇒<c>bool</c> in phase 25
/// and the boolean-literal <c>ORA-00904</c> in phases 37/40. Nothing here projects or compares a
/// boolean literal, so the known failure mode does not apply, but "SQLite is green" is not the
/// same statement as "Oracle is green".
/// </summary>
public sealed class WhiteAndBlackListReadRepositoryFilterTests : IDisposable
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

        CREATE TABLE TB_VAHED_TYPE (
            ID TEXT PRIMARY KEY,
            TYPECODE TEXT,
            TYPENAME TEXT,
            PARENTTYPECODE TEXT
        );

        CREATE TABLE TB_WHITEANDBLACKLIST (
            ID TEXT PRIMARY KEY,
            ACCOUNTCODE_ID TEXT,
            VAHEDTYPE_ID TEXT,
            CREATEDDATE TEXT,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER,
            FROMAUTHORIZEDDATE TEXT,
            TOAUTHORIZEDDATE TEXT,
            FROMLIMITATIONDATE TEXT,
            TOLIMITATIONDATE TEXT,
            STATE INTEGER
        );
        """;

    private readonly SqliteConnection _connection;

    /// <summary>Two unit types in different «بخش» buckets, matching the live data's shape.</summary>
    private readonly Guid _hospitalTypeId = Guid.NewGuid();

    private readonly Guid _treasuryTypeId = Guid.NewGuid();

    private int _createdDateTick;

    public WhiteAndBlackListReadRepositoryFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateTablesSql);

        setupContext.TB_VAHED_TYPEs.AddRange(
            new TB_VAHED_TYPE { ID = _hospitalTypeId, TYPECODE = "3", TYPENAME = "بيمارستان", PARENTTYPECODE = "2" },
            new TB_VAHED_TYPE { ID = _treasuryTypeId, TYPECODE = "12", TYPENAME = "خزانه", PARENTTYPECODE = "3" });
        setupContext.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LegacyDbContext(options);
    }

    private Guid SeedAccount(string accCode, string accName)
    {
        using var context = CreateContext();
        var id = Guid.NewGuid();
        context.TB_ACCOUNTCODEs.Add(new TB_ACCOUNTCODE
        {
            ID = id,
            ACCCODE = accCode,
            ACCCODENAME = accName,
            ISDELETED = false,
        });
        context.SaveChanges();
        return id;
    }

    private Guid SeedPermission(
        Guid accountCodeId,
        Guid? vahedTypeId,
        WhiteBlackListState state = WhiteBlackListState.Allowed,
        string? fromAuthorized = "14040101",
        string? toAuthorized = "14041229",
        string? fromLimitation = null,
        string? toLimitation = null,
        bool? isDeleted = false)
    {
        using var context = CreateContext();
        var id = Guid.NewGuid();
        context.TB_WHITEANDBLACKLISTs.Add(new TB_WHITEANDBLACKLIST
        {
            ID = id,
            ACCOUNTCODE_ID = accountCodeId,
            VAHEDTYPE_ID = vahedTypeId,
            // Distinct, increasing timestamps so the ORDER BY is deterministic and assertions on
            // ordering are not accidentally passing on a tie-breaker.
            CREATEDDATE = new DateTime(2025, 1, 1).AddMinutes(_createdDateTick++),
            ADDUSERID = "tester",
            ISDELETED = isDeleted,
            FROMAUTHORIZEDDATE = fromAuthorized,
            TOAUTHORIZEDDATE = toAuthorized,
            FROMLIMITATIONDATE = fromLimitation,
            TOLIMITATIONDATE = toLimitation,
            STATE = state,
        });
        context.SaveChanges();
        return id;
    }

    private async Task<IReadOnlyList<WhiteAndBlackListDto>> QueryAsync(WhiteAndBlackListFilter filter)
    {
        using var context = CreateContext();
        var repository = new WhiteAndBlackListReadRepository(context);

        var result = await repository.GetPagedAsync(1, 50, filter);

        return result.Items.ToList();
    }

    [Fact]
    public async Task GetPagedAsync_ProjectsAccountAndUnitTypeDisplayFields()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(accountId, _hospitalTypeId);

        var row = Assert.Single(await QueryAsync(WhiteAndBlackListFilter.None));

        Assert.Equal("110101", row.AccCode);
        Assert.Equal("بانک ملت", row.AccCodeName);
        Assert.Equal("3", row.VahedTypeCode);
        Assert.Equal("بيمارستان", row.VahedTypeName);
        Assert.Equal("2", row.VahedTypeParentCode);
    }

    /// <summary>
    /// <c>VAHEDTYPE_ID</c> is nullable, so the unit-type join must be a LEFT join. If it were an
    /// inner join the row would vanish from the grid entirely rather than showing an empty
    /// «نوع واحد» cell — a silent data loss, not a visible gap.
    /// </summary>
    [Fact]
    public async Task GetPagedAsync_KeepsRowsWithNoUnitType_AndLeavesTheirTypeFieldsNull()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(accountId, vahedTypeId: null);

        var row = Assert.Single(await QueryAsync(WhiteAndBlackListFilter.None));

        Assert.Equal("110101", row.AccCode);
        Assert.Null(row.VahedTypeId);
        Assert.Null(row.VahedTypeName);
        Assert.Null(row.VahedTypeParentCode);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByAccountCodeId()
    {
        var wanted = SeedAccount("110101", "بانک ملت");
        var other = SeedAccount("303030", "اسناد دريافتني");
        SeedPermission(wanted, _hospitalTypeId);
        SeedPermission(other, _hospitalTypeId);

        var rows = await QueryAsync(new WhiteAndBlackListFilter(AccountCodeId: wanted));

        Assert.Equal("110101", Assert.Single(rows).AccCode);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByVahedTypeId()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(accountId, _hospitalTypeId);
        SeedPermission(accountId, _treasuryTypeId);

        var rows = await QueryAsync(new WhiteAndBlackListFilter(VahedTypeId: _treasuryTypeId));

        Assert.Equal("خزانه", Assert.Single(rows).VahedTypeName);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByState()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(accountId, _hospitalTypeId, WhiteBlackListState.Allowed);
        SeedPermission(accountId, _treasuryTypeId, WhiteBlackListState.Blacklisted, fromAuthorized: null, toAuthorized: null);

        var rows = await QueryAsync(new WhiteAndBlackListFilter(State: WhiteBlackListState.Blacklisted));

        Assert.Equal(WhiteBlackListState.Blacklisted, Assert.Single(rows).State);
    }

    /// <summary>
    /// The columns hold zero-padded <c>YYYYMMDD</c> Jalali text, so a lexicographic comparison is a
    /// chronological one. This asserts the bound is inclusive and really runs in SQL.
    /// </summary>
    [Fact]
    public async Task GetPagedAsync_FromAuthorizedDate_IsAnInclusiveLowerBound()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(accountId, _hospitalTypeId, fromAuthorized: "14030101", toAuthorized: "14031229");
        SeedPermission(accountId, _treasuryTypeId, fromAuthorized: "14040101", toAuthorized: "14041229");

        var rows = await QueryAsync(new WhiteAndBlackListFilter(FromAuthorizedDate: "14040101"));

        Assert.Equal("14040101", Assert.Single(rows).FromAuthorizedDate);
    }

    [Fact]
    public async Task GetPagedAsync_ToAuthorizedDate_IsAnInclusiveUpperBound()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(accountId, _hospitalTypeId, fromAuthorized: "14030101", toAuthorized: "14031229");
        SeedPermission(accountId, _treasuryTypeId, fromAuthorized: "14040101", toAuthorized: "14041229");

        var rows = await QueryAsync(new WhiteAndBlackListFilter(ToAuthorizedDate: "14031229"));

        Assert.Equal("14031229", Assert.Single(rows).ToAuthorizedDate);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByLimitationDateRange()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(
            accountId, _hospitalTypeId, WhiteBlackListState.SystemOnly,
            fromAuthorized: null, toAuthorized: null, fromLimitation: "14040101", toLimitation: "14040131");
        SeedPermission(
            accountId, _treasuryTypeId, WhiteBlackListState.SystemOnly,
            fromAuthorized: null, toAuthorized: null, fromLimitation: "14050101", toLimitation: "14050131");

        var lower = await QueryAsync(new WhiteAndBlackListFilter(FromLimitationDate: "14050101"));
        Assert.Equal("14050101", Assert.Single(lower).FromLimitationDate);

        var upper = await QueryAsync(new WhiteAndBlackListFilter(ToLimitationDate: "14040131"));
        Assert.Equal("14040131", Assert.Single(upper).ToLimitationDate);
    }

    /// <summary>
    /// A row whose bounded column is NULL must not slip through a bound. A permission with no
    /// authorized-from date does not fall inside an authorized-from range, and showing it would
    /// tell the user the filter found something it did not.
    /// </summary>
    [Fact]
    public async Task GetPagedAsync_DateBounds_ExcludeRowsWhoseColumnIsNull()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(
            accountId, _hospitalTypeId, WhiteBlackListState.Blacklisted,
            fromAuthorized: null, toAuthorized: null);

        Assert.Empty(await QueryAsync(new WhiteAndBlackListFilter(FromAuthorizedDate: "14040101")));
        Assert.Empty(await QueryAsync(new WhiteAndBlackListFilter(ToAuthorizedDate: "14041229")));
    }

    [Fact]
    public async Task GetPagedAsync_CombinesFiltersWithAnd()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        var otherAccountId = SeedAccount("303030", "اسناد دريافتني");
        var wanted = SeedPermission(accountId, _treasuryTypeId);
        SeedPermission(accountId, _hospitalTypeId);
        SeedPermission(otherAccountId, _treasuryTypeId);

        var rows = await QueryAsync(new WhiteAndBlackListFilter(
            AccountCodeId: accountId,
            VahedTypeId: _treasuryTypeId));

        Assert.Equal(wanted, Assert.Single(rows).Id);
    }

    [Fact]
    public async Task GetPagedAsync_ExcludesSoftDeletedRows()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(accountId, _hospitalTypeId, isDeleted: true);

        Assert.Empty(await QueryAsync(WhiteAndBlackListFilter.None));
    }

    /// <summary>
    /// <c>ISDELETED</c> is <c>bool?</c>, and legacy rows can hold NULL. The read filter is
    /// <c>!= true</c> precisely so those stay visible; <c>== false</c> would hide them.
    /// </summary>
    [Fact]
    public async Task GetPagedAsync_KeepsRowsWithNullIsDeleted()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(accountId, _hospitalTypeId, isDeleted: null);

        Assert.Single(await QueryAsync(WhiteAndBlackListFilter.None));
    }

    [Fact]
    public async Task GetExistingKeysAsync_ReturnsTheUniqueKeyOfEveryLiveRowForTheGivenAccounts()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        var otherAccountId = SeedAccount("303030", "اسناد دريافتني");
        SeedPermission(accountId, _hospitalTypeId, fromAuthorized: "14040101", toAuthorized: "14041229");
        SeedPermission(otherAccountId, _treasuryTypeId);

        using var context = CreateContext();
        var repository = new WhiteAndBlackListReadRepository(context);

        var keys = await repository.GetExistingKeysAsync([accountId]);

        var key = Assert.Single(keys);
        Assert.Equal(new WhiteAndBlackListKey(accountId, _hospitalTypeId, "14040101", "14041229"), key);
    }

    /// <summary>
    /// A soft-deleted row must not suppress a re-grant: the UNIQUE index still covers it in
    /// Oracle, but the product decision is that a deleted permission is gone, and returning its
    /// key here would make the combination permanently un-grantable through the UI.
    ///
    /// ⚠️ This is the one place where the pre-check and the database can genuinely disagree: a
    /// re-grant over a soft-deleted row is staged, and Oracle answers with a unique-key violation
    /// that the central mapping turns into 409. Recorded in <c>docs/open-decisions.md</c>.
    /// </summary>
    [Fact]
    public async Task GetExistingKeysAsync_IgnoresSoftDeletedRows()
    {
        var accountId = SeedAccount("110101", "بانک ملت");
        SeedPermission(accountId, _hospitalTypeId, isDeleted: true);

        using var context = CreateContext();
        var repository = new WhiteAndBlackListReadRepository(context);

        Assert.Empty(await repository.GetExistingKeysAsync([accountId]));
    }

    [Fact]
    public async Task GetExistingKeysAsync_WithNoAccounts_ReturnsEmptyWithoutQuerying()
    {
        using var context = CreateContext();
        var repository = new WhiteAndBlackListReadRepository(context);

        Assert.Empty(await repository.GetExistingKeysAsync([]));
    }
}
