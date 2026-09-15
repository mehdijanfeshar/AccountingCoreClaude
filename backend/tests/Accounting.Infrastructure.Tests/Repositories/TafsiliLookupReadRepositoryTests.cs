using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) repository-level tests for <see cref="TafsiliLookupReadRepository"/>,
/// exercising the actual EF Core LINQ query/SQL translation for Rule A (active level semantics),
/// Rule B (تفصیلی visibility scoping), the digit-vs-name search heuristic, cross-group
/// de-duplication and caller-category derivation (<c>TB_VAHED_INFO → TB_VAHED_TYPE</c>). The
/// mocked handler tests in <c>Accounting.Application.Tests</c> only prove the handlers CALL this
/// repository with the right arguments; none of that project-logic actually runs there.
///
/// Provider choice (SQLite in-memory, NOT EF Core InMemory) and the hand-written <c>CREATE
/// TABLE</c> workaround (because <c>EnsureCreated()</c> fails against the full
/// <see cref="LegacyDbContext"/> model's <c>HasSequence</c>) are copied verbatim in rationale from
/// <c>VoucherHeadRepositorySoftDeleteDetailLinesTests</c> — see that file's class-level XML doc.
/// Same standing limitation as every other test in this project: SQLite, never live Oracle.
/// </summary>
public sealed class TafsiliLookupReadRepositoryTests : IDisposable
{
    private const string CreateAccountLinkLevelTableSql = """
        CREATE TABLE TB_ACCOUNT_LINK_LEVEL (
            ID TEXT PRIMARY KEY,
            ACCOUNT_ID TEXT NOT NULL,
            LEVEL_ID TEXT NOT NULL,
            CREATEDDATE TEXT NOT NULL,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT NOT NULL,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER NOT NULL
        );
        """;

    private const string CreateLevelTafsilTableSql = """
        CREATE TABLE TB_LEVEL_TAFSIL (
            ID TEXT PRIMARY KEY,
            LEVEL_CODE TEXT NOT NULL,
            LEVEL_NAME TEXT NOT NULL,
            CREATEDDATE TEXT NOT NULL,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT NOT NULL,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER NOT NULL
        );
        """;

    private const string CreateAccountLinkTafsilGroupTableSql = """
        CREATE TABLE TB_ACCOUNT_LINK_TAFSILGROUP (
            ID TEXT PRIMARY KEY,
            ACCOUNT_ID TEXT NOT NULL,
            TAFSILGROUP_ID TEXT NOT NULL,
            LEVEL_ID TEXT NOT NULL,
            CREATEDDATE TEXT NOT NULL,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT NOT NULL,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER NOT NULL
        );
        """;

    private const string CreateTafsilLinkTafsilGroupTableSql = """
        CREATE TABLE TB_TAFSIL_LINK_TAFSILGROUP (
            ID TEXT PRIMARY KEY,
            TAFSIL_ID TEXT NOT NULL,
            TAFSILGROUP_ID TEXT NOT NULL,
            CREATEDDATE TEXT NOT NULL,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT NOT NULL,
            CHANGEUSERID TEXT,
            VAHEDCODE TEXT,
            ISDELETED INTEGER NOT NULL,
            VAHEDTYPE INTEGER
        );
        """;

    private const string CreateTafsiliTableSql = """
        CREATE TABLE TB_TAFSILI (
            ID TEXT PRIMARY KEY,
            TAFSILI_CODE TEXT,
            TAFSILI_NAME TEXT,
            ISACTIVE INTEGER,
            PERSONTYPE INTEGER,
            CREATEDDATE TEXT,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT,
            CHANGEUSERID TEXT,
            VAHEDCODE TEXT,
            ISDELETED INTEGER,
            TAFSIL_DESC TEXT,
            OWNER INTEGER,
            VAHEDTYPE INTEGER
        );
        """;

    private const string CreateVahedInfoTableSql = """
        CREATE TABLE TB_VAHED_INFO (
            ID TEXT PRIMARY KEY,
            VAHEDCODE TEXT NOT NULL,
            VAHEDNAME TEXT NOT NULL,
            CITY_ID TEXT NOT NULL,
            VAHEDTYPE_ID TEXT NOT NULL,
            PARENT_ID TEXT
        );
        """;

    private const string CreateVahedTypeTableSql = """
        CREATE TABLE TB_VAHED_TYPE (
            ID TEXT PRIMARY KEY,
            TYPECODE TEXT,
            TYPENAME TEXT,
            PARENTTYPECODE TEXT
        );
        """;

    private readonly SqliteConnection _connection;

    public TafsiliLookupReadRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateAccountLinkLevelTableSql);
        setupContext.Database.ExecuteSqlRaw(CreateLevelTafsilTableSql);
        setupContext.Database.ExecuteSqlRaw(CreateAccountLinkTafsilGroupTableSql);
        setupContext.Database.ExecuteSqlRaw(CreateTafsilLinkTafsilGroupTableSql);
        setupContext.Database.ExecuteSqlRaw(CreateTafsiliTableSql);
        setupContext.Database.ExecuteSqlRaw(CreateVahedInfoTableSql);
        setupContext.Database.ExecuteSqlRaw(CreateVahedTypeTableSql);
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LegacyDbContext(options);
    }

    private static readonly DateTime SeedDate = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);

    private static TB_ACCOUNT_LINK_LEVEL AccountLinkLevel(Guid accountId, Guid levelId, bool isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNT_ID = accountId,
        LEVEL_ID = levelId,
        CREATEDDATE = SeedDate,
        ADDUSERID = "seed-user",
        ISDELETED = isDeleted,
    };

    private static TB_LEVEL_TAFSIL LevelTafsil(Guid id, string levelCode, string levelName = "سطح", bool isDeleted = false) => new()
    {
        ID = id,
        LEVEL_CODE = levelCode,
        LEVEL_NAME = levelName,
        CREATEDDATE = SeedDate,
        ADDUSERID = "seed-user",
        ISDELETED = isDeleted,
    };

    private static TB_ACCOUNT_LINK_TAFSILGROUP AccountLinkTafsilGroup(
        Guid accountId, Guid levelId, Guid tafsilGroupId, bool isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNT_ID = accountId,
        LEVEL_ID = levelId,
        TAFSILGROUP_ID = tafsilGroupId,
        CREATEDDATE = SeedDate,
        ADDUSERID = "seed-user",
        ISDELETED = isDeleted,
    };

    private static TB_TAFSIL_LINK_TAFSILGROUP TafsilLinkTafsilGroup(
        Guid tafsilId, Guid tafsilGroupId, string? vahedCode, short? vahedType, bool isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        TAFSIL_ID = tafsilId,
        TAFSILGROUP_ID = tafsilGroupId,
        CREATEDDATE = SeedDate,
        ADDUSERID = "seed-user",
        VAHEDCODE = vahedCode,
        VAHEDTYPE = vahedType,
        ISDELETED = isDeleted,
    };

    private static TB_TAFSILI Tafsili(Guid id, string code, string name, bool? isDeleted = false) => new()
    {
        ID = id,
        TAFSILI_CODE = code,
        TAFSILI_NAME = name,
        ISDELETED = isDeleted,
    };

    private static TB_VAHED_TYPE VahedType(Guid id, string typeCode) => new()
    {
        ID = id,
        TYPECODE = typeCode,
        TYPENAME = "نوع واحد",
    };

    private static TB_VAHED_INFO VahedInfo(string vahedCode, Guid vahedTypeId) => new()
    {
        ID = Guid.NewGuid(),
        VAHEDCODE = vahedCode,
        VAHEDNAME = "واحد",
        CITY_ID = Guid.NewGuid(),
        VAHEDTYPE_ID = vahedTypeId,
    };

    // ------------------------------------------------------------------
    // GetActiveLevelsAsync — Rule A
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetActiveLevelsAsync_ReturnsLevels_OrderedByNumericCode()
    {
        var accountId = Guid.NewGuid();
        var level1 = LevelTafsil(Guid.NewGuid(), "1", "معین");
        var level2 = LevelTafsil(Guid.NewGuid(), "2", "تفصیلی سطح ۲");
        var level3 = LevelTafsil(Guid.NewGuid(), "3", "تفصیلی سطح ۳");

        using (var seedContext = CreateContext())
        {
            // Deliberately seeded out of order (3, 1, 2) to prove the repository sorts, not the
            // seed/insertion order.
            seedContext.TB_LEVEL_TAFSILs.AddRange(level3, level1, level2);
            seedContext.TB_ACCOUNT_LINK_LEVELs.AddRange(
                AccountLinkLevel(accountId, level3.ID),
                AccountLinkLevel(accountId, level1.ID),
                AccountLinkLevel(accountId, level2.ID));
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetActiveLevelsAsync(accountId);

        Assert.Equal(new[] { 1, 2, 3 }, result.Select(l => l.Code));
        Assert.All(result, l => Assert.True(l.IsRequired));
    }

    [Fact]
    public async Task GetActiveLevelsAsync_UnparsableLevelCode_IsSkipped_OthersStillReturned()
    {
        var accountId = Guid.NewGuid();
        var validLevel = LevelTafsil(Guid.NewGuid(), "1");
        var garbageLevel = LevelTafsil(Guid.NewGuid(), "not-a-number");

        using (var seedContext = CreateContext())
        {
            seedContext.TB_LEVEL_TAFSILs.AddRange(validLevel, garbageLevel);
            seedContext.TB_ACCOUNT_LINK_LEVELs.AddRange(
                AccountLinkLevel(accountId, validLevel.ID),
                AccountLinkLevel(accountId, garbageLevel.ID));
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetActiveLevelsAsync(accountId);

        var single = Assert.Single(result);
        Assert.Equal(validLevel.ID, single.LevelId);
        Assert.Equal(1, single.Code);
    }

    [Fact]
    public async Task GetActiveLevelsAsync_SoftDeletedLinkRow_IsExcluded()
    {
        var accountId = Guid.NewGuid();
        var level = LevelTafsil(Guid.NewGuid(), "1");

        using (var seedContext = CreateContext())
        {
            seedContext.TB_LEVEL_TAFSILs.Add(level);
            seedContext.TB_ACCOUNT_LINK_LEVELs.Add(AccountLinkLevel(accountId, level.ID, isDeleted: true));
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetActiveLevelsAsync(accountId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveLevelsAsync_DuplicateLinkRowsToSameLevel_LevelAppearsOnceInResult()
    {
        // TB_ACCOUNT_LINK_LEVEL has no unique constraint on (ACCOUNT_ID, LEVEL_ID) — this seeds
        // exactly that anomaly (two non-deleted link rows to the same level for the same account)
        // to prove the repository still returns the level exactly once.
        var accountId = Guid.NewGuid();
        var level = LevelTafsil(Guid.NewGuid(), "1");

        using (var seedContext = CreateContext())
        {
            seedContext.TB_LEVEL_TAFSILs.Add(level);
            seedContext.TB_ACCOUNT_LINK_LEVELs.AddRange(
                AccountLinkLevel(accountId, level.ID),
                AccountLinkLevel(accountId, level.ID));
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetActiveLevelsAsync(accountId);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetActiveLevelsAsync_SoftDeletedLevelTafsilRow_IsExcluded()
    {
        var accountId = Guid.NewGuid();
        var level = LevelTafsil(Guid.NewGuid(), "1", isDeleted: true);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_LEVEL_TAFSILs.Add(level);
            seedContext.TB_ACCOUNT_LINK_LEVELs.Add(AccountLinkLevel(accountId, level.ID));
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetActiveLevelsAsync(accountId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveLevelsAsync_UnknownAccountId_ReturnsEmptyList_Not404()
    {
        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetActiveLevelsAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ------------------------------------------------------------------
    // GetSelectableItemsAsync — Rule B truth table
    // ------------------------------------------------------------------

    private const string CallerVahedCode = "0001";
    private const string OtherVahedCode = "0002";

    /// <summary>
    /// Seeds one (account, level) → one تفصیلی گروه → one تفصیلی chain and returns the ids needed
    /// to call <see cref="TafsiliLookupReadRepository.GetSelectableItemsAsync"/>.
    /// </summary>
    private static (Guid AccountId, Guid LevelId, Guid TafsilGroupId, Guid TafsiliId) SeedChain(
        LegacyDbContext context, string? linkVahedCode, short? linkVahedType)
    {
        var accountId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var tafsilGroupId = Guid.NewGuid();
        var tafsiliId = Guid.NewGuid();

        context.TB_ACCOUNT_LINK_TAFSILGROUPs.Add(AccountLinkTafsilGroup(accountId, levelId, tafsilGroupId));
        context.TB_TAFSIL_LINK_TAFSILGROUPs.Add(
            TafsilLinkTafsilGroup(tafsiliId, tafsilGroupId, linkVahedCode, linkVahedType));
        context.TB_TAFSILIs.Add(Tafsili(tafsiliId, "100", "تفصیلی نمونه"));

        return (accountId, levelId, tafsilGroupId, tafsiliId);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_VisibleWhenVahedCodeMatches_EvenWithNullVahedType()
    {
        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            chain = SeedChain(seedContext, linkVahedCode: CallerVahedCode, linkVahedType: null);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Single(result.Items);
        Assert.Equal(chain.tafsiliId, result.Items[0].Id);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_VisibleWhenVahedTypeIsAllWildcard_EvenWithDifferentVahedCode()
    {
        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            chain = SeedChain(seedContext, linkVahedCode: OtherVahedCode, linkVahedType: 3);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Single(result.Items);
        Assert.Equal(chain.tafsiliId, result.Items[0].Id);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_VisibleWhenVahedTypeMatchesCallerCategory()
    {
        // Caller's own unit resolves to VahedCategory.Insurance (TYPECODE "1" = EdareKol); the
        // link's VAHEDTYPE is 1 (Insurance) too, and its VAHEDCODE deliberately does not match.
        var vahedTypeId = Guid.NewGuid();

        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            seedContext.TB_VAHED_TYPEs.Add(VahedType(vahedTypeId, "1"));
            seedContext.TB_VAHED_INFOs.Add(VahedInfo(CallerVahedCode, vahedTypeId));
            chain = SeedChain(seedContext, linkVahedCode: OtherVahedCode, linkVahedType: 1);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Single(result.Items);
        Assert.Equal(chain.tafsiliId, result.Items[0].Id);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_NotVisibleWhenVahedTypeIsNull_AndVahedCodeDiffers()
    {
        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            chain = SeedChain(seedContext, linkVahedCode: OtherVahedCode, linkVahedType: null);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_NotVisibleWhenVahedTypeBelongsToAnotherCategory_AndVahedCodeDiffers()
    {
        // Caller resolves to Insurance (TYPECODE "1"); the link's VAHEDTYPE is 2 (Treatment) and
        // its VAHEDCODE does not match — must not be visible.
        var vahedTypeId = Guid.NewGuid();

        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            seedContext.TB_VAHED_TYPEs.Add(VahedType(vahedTypeId, "1"));
            seedContext.TB_VAHED_INFOs.Add(VahedInfo(CallerVahedCode, vahedTypeId));
            chain = SeedChain(seedContext, linkVahedCode: OtherVahedCode, linkVahedType: 2);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Empty(result.Items);
    }

    // ------------------------------------------------------------------
    // Caller-category derivation
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("1")] // EdareKol
    [InlineData("9")] // Shob
    public async Task GetSelectableItemsAsync_InsuranceTypeCodes_ResolveToInsuranceCategory(string typeCode)
    {
        var vahedTypeId = Guid.NewGuid();

        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            seedContext.TB_VAHED_TYPEs.Add(VahedType(vahedTypeId, typeCode));
            seedContext.TB_VAHED_INFOs.Add(VahedInfo(CallerVahedCode, vahedTypeId));
            // Link is visible only via VAHEDTYPE == Insurance (1) — proves the caller actually
            // resolved to Insurance, not just "matched by accident".
            chain = SeedChain(seedContext, linkVahedCode: OtherVahedCode, linkVahedType: 1);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Single(result.Items);
        Assert.Equal(chain.tafsiliId, result.Items[0].Id);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_TreatmentTypeCode_ResolvesToTreatmentCategory()
    {
        // TYPECODE "3" = Bimarestan, a Treatment-category unit per VahedCategoryMapper.
        var vahedTypeId = Guid.NewGuid();

        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            seedContext.TB_VAHED_TYPEs.Add(VahedType(vahedTypeId, "3"));
            seedContext.TB_VAHED_INFOs.Add(VahedInfo(CallerVahedCode, vahedTypeId));
            // Visible only via VAHEDTYPE == Treatment (2).
            chain = SeedChain(seedContext, linkVahedCode: OtherVahedCode, linkVahedType: 2);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Single(result.Items);
        Assert.Equal(chain.tafsiliId, result.Items[0].Id);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_UnknownTypeCode_FallsBackToAll_NotInsuranceOrTreatment()
    {
        var vahedTypeId = Guid.NewGuid();

        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            seedContext.TB_VAHED_TYPEs.Add(VahedType(vahedTypeId, "999"));
            seedContext.TB_VAHED_INFOs.Add(VahedInfo(CallerVahedCode, vahedTypeId));
            // Link only visible via VAHEDTYPE == Insurance (1) — must NOT match an "All" fallback.
            chain = SeedChain(seedContext, linkVahedCode: OtherVahedCode, linkVahedType: 1);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_MissingVahedInfoRow_FallsBackToAll_NotInsuranceOrTreatment()
    {
        // No TB_VAHED_INFO row at all for CallerVahedCode — must not throw, must resolve to All.
        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            chain = SeedChain(seedContext, linkVahedCode: OtherVahedCode, linkVahedType: 1);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Empty(result.Items);
    }

    // ------------------------------------------------------------------
    // Search heuristic
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetSelectableItemsAsync_SearchWithDigit_MatchesCodeOnly_NotName()
    {
        var accountId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var tafsilGroupId = Guid.NewGuid();
        var matchByCodeId = Guid.NewGuid();
        var matchByNameOnlyId = Guid.NewGuid();

        using (var seedContext = CreateContext())
        {
            seedContext.TB_ACCOUNT_LINK_TAFSILGROUPs.Add(AccountLinkTafsilGroup(accountId, levelId, tafsilGroupId));
            seedContext.TB_TAFSIL_LINK_TAFSILGROUPs.AddRange(
                TafsilLinkTafsilGroup(matchByCodeId, tafsilGroupId, CallerVahedCode, null),
                TafsilLinkTafsilGroup(matchByNameOnlyId, tafsilGroupId, CallerVahedCode, null));
            seedContext.TB_TAFSILIs.AddRange(
                Tafsili(matchByCodeId, code: "100200", name: "Alpha"),
                // Code has NO digit-matching substring; name contains "100" — must NOT match a
                // digit-search, because a digit search only ever looks at TAFSILI_CODE.
                Tafsili(matchByNameOnlyId, code: "ZZZ", name: "Contains100InName"));
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            accountId, levelId, search: "100", callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        var single = Assert.Single(result.Items);
        Assert.Equal(matchByCodeId, single.Id);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_SearchWithoutDigit_MatchesNameOnly_NotCode()
    {
        var accountId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var tafsilGroupId = Guid.NewGuid();
        var matchByNameId = Guid.NewGuid();
        var matchByCodeOnlyId = Guid.NewGuid();

        using (var seedContext = CreateContext())
        {
            seedContext.TB_ACCOUNT_LINK_TAFSILGROUPs.Add(AccountLinkTafsilGroup(accountId, levelId, tafsilGroupId));
            seedContext.TB_TAFSIL_LINK_TAFSILGROUPs.AddRange(
                TafsilLinkTafsilGroup(matchByNameId, tafsilGroupId, CallerVahedCode, null),
                TafsilLinkTafsilGroup(matchByCodeOnlyId, tafsilGroupId, CallerVahedCode, null));
            seedContext.TB_TAFSILIs.AddRange(
                Tafsili(matchByNameId, code: "500", name: "Alpha Corp"),
                // Code contains "Alpha"; name does not — a no-digit search only ever looks at
                // TAFSILI_NAME, so this must NOT match.
                Tafsili(matchByCodeOnlyId, code: "AlphaCode", name: "Other"));
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            accountId, levelId, search: "Alpha", callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        var single = Assert.Single(result.Items);
        Assert.Equal(matchByNameId, single.Id);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_BlankSearch_AppliesNoFilter()
    {
        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            chain = SeedChain(seedContext, linkVahedCode: CallerVahedCode, linkVahedType: null);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: "   ", callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Single(result.Items);
    }

    // ------------------------------------------------------------------
    // De-duplication across multiple تفصیلی گروه
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetSelectableItemsAsync_SameTafsiliReachableThroughTwoGroups_AppearsOnceInItemsAndCount()
    {
        var accountId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var groupOneId = Guid.NewGuid();
        var groupTwoId = Guid.NewGuid();
        var sharedTafsiliId = Guid.NewGuid();

        using (var seedContext = CreateContext())
        {
            seedContext.TB_ACCOUNT_LINK_TAFSILGROUPs.AddRange(
                AccountLinkTafsilGroup(accountId, levelId, groupOneId),
                AccountLinkTafsilGroup(accountId, levelId, groupTwoId));
            seedContext.TB_TAFSIL_LINK_TAFSILGROUPs.AddRange(
                TafsilLinkTafsilGroup(sharedTafsiliId, groupOneId, CallerVahedCode, null),
                TafsilLinkTafsilGroup(sharedTafsiliId, groupTwoId, CallerVahedCode, null));
            seedContext.TB_TAFSILIs.Add(Tafsili(sharedTafsiliId, "700", "تفصیلی مشترک"));
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            accountId, levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetSelectableItemsAsync_Label_IsCodeDashName()
    {
        (Guid accountId, Guid levelId, Guid tafsilGroupId, Guid tafsiliId) chain;
        using (var seedContext = CreateContext())
        {
            chain = SeedChain(seedContext, linkVahedCode: CallerVahedCode, linkVahedType: null);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new TafsiliLookupReadRepository(actContext);

        var result = await repository.GetSelectableItemsAsync(
            chain.accountId, chain.levelId, search: null, callerVahedCode: CallerVahedCode,
            pageNumber: 1, pageSize: 20);

        var single = Assert.Single(result.Items);
        Assert.Equal("100 - تفصیلی نمونه", single.Label);
    }
}
