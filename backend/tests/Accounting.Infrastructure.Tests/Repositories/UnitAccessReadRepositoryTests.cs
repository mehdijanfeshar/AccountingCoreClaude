using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) tests for <see cref="UnitAccessReadRepository"/> — the org-unit tree walk
/// behind «دسترسی به زیرمجموعه». The rule being locked in here is ported from the reference
/// project's <c>VahedInfoRepository.GetAllVahedInfoByParentAsync</c> +
/// <c>GetVahedInfoQueryHandler</c>.
///
/// Provider choice (SQLite in-memory, NOT EF Core InMemory) and the hand-written <c>CREATE
/// TABLE</c> workaround (<c>EnsureCreated()</c> fails against the full
/// <see cref="LegacyDbContext"/> model's <c>HasSequence</c>) follow
/// <c>TafsiliLookupReadRepositoryTests</c>. Same standing limitation as every other test in this
/// project: SQLite, never live Oracle.
/// </summary>
public sealed class UnitAccessReadRepositoryTests : IDisposable
{
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

    /// <summary>A non-headquarters unit type — anything that is not "17".</summary>
    private static readonly Guid OrdinaryTypeId = Guid.NewGuid();

    private static readonly Guid HeadquartersTypeId = Guid.NewGuid();

    public UnitAccessReadRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateVahedInfoTableSql);
        setupContext.Database.ExecuteSqlRaw(CreateVahedTypeTableSql);

        setupContext.TB_VAHED_TYPEs.Add(new TB_VAHED_TYPE
        {
            ID = OrdinaryTypeId,
            TYPECODE = "1",
            TYPENAME = "مدیریت درمان",
        });
        setupContext.TB_VAHED_TYPEs.Add(new TB_VAHED_TYPE
        {
            ID = HeadquartersTypeId,
            TYPECODE = UnitAccessReadRepository.HeadquartersVahedTypeCode,
            TYPENAME = "ستاد مرکزی",
        });
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

    private static TB_VAHED_INFO Unit(Guid id, string vahedCode, Guid typeId, Guid? parentId = null) => new()
    {
        ID = id,
        VAHEDCODE = vahedCode,
        VAHEDNAME = $"واحد {vahedCode}",
        CITY_ID = Guid.NewGuid(),
        VAHEDTYPE_ID = typeId,
        PARENT_ID = parentId,
    };

    /// <summary>
    /// Seeds ستاد(0000) → مدیریت(1000) → بیمارستان(1100) → بخش(1110), plus an unrelated
    /// مدیریت(2000) with its own child(2100). Three levels deep on purpose: that is the only
    /// shape that can tell "direct children only" apart from "whole subtree".
    /// </summary>
    private (Guid Hq, Guid Management, Guid Hospital, Guid Ward, Guid OtherManagement, Guid OtherChild) SeedTree()
    {
        var hq = Guid.NewGuid();
        var management = Guid.NewGuid();
        var hospital = Guid.NewGuid();
        var ward = Guid.NewGuid();
        var otherManagement = Guid.NewGuid();
        var otherChild = Guid.NewGuid();

        using var context = CreateContext();
        context.TB_VAHED_INFOs.AddRange(
            Unit(hq, "0000", HeadquartersTypeId),
            Unit(management, "1000", OrdinaryTypeId, hq),
            Unit(hospital, "1100", OrdinaryTypeId, management),
            Unit(ward, "1110", OrdinaryTypeId, hospital),
            Unit(otherManagement, "2000", OrdinaryTypeId, hq),
            Unit(otherChild, "2100", OrdinaryTypeId, otherManagement));
        context.SaveChanges();

        return (hq, management, hospital, ward, otherManagement, otherChild);
    }

    [Fact]
    public async Task Headquarters_unit_reaches_every_unit()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync("0000");

        Assert.Equal(
            new[] { "0000", "1000", "1100", "1110", "2000", "2100" },
            result.Select(u => u.VahedCode).ToArray());
    }

    [Fact]
    public async Task Ordinary_unit_reaches_its_whole_subtree_not_only_direct_children()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync("1000");

        // "1110" is a GRANDCHILD. The reference project's one-level rule would omit it; this
        // project deliberately walks the full subtree (project owner, 2026-09-22). If that
        // decision is ever reversed, this is the assertion that should fail first.
        Assert.Equal(new[] { "1000", "1100", "1110" }, result.Select(u => u.VahedCode).ToArray());
    }

    [Fact]
    public async Task Ordinary_unit_never_reaches_a_sibling_branch()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync("1000");

        Assert.DoesNotContain(result, u => u.VahedCode == "2000");
        Assert.DoesNotContain(result, u => u.VahedCode == "2100");
    }

    [Fact]
    public async Task Ordinary_unit_never_reaches_its_own_parent()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync("1100");

        Assert.DoesNotContain(result, u => u.VahedCode == "1000");
        Assert.DoesNotContain(result, u => u.VahedCode == "0000");
    }

    [Fact]
    public async Task Leaf_unit_reaches_only_itself()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync("1110");

        Assert.Equal(new[] { "1110" }, result.Select(u => u.VahedCode).ToArray());
    }

    [Fact]
    public async Task Caller_own_unit_is_the_only_row_flagged_default()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync("1000");

        Assert.Equal("1000", Assert.Single(result.Where(u => u.IsDefault)).VahedCode);
    }

    [Fact]
    public async Task Unknown_unit_code_returns_empty_rather_than_everything()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync("9999");

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_unit_code_returns_empty(string vahedCode)
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync(vahedCode);

        Assert.Empty(result);
    }

    /// <summary>
    /// TB_VAHED_INFO.PARENT_ID has no FK (open risk #9), so a self-referencing row is physically
    /// possible. Without the traversal's visited set this test hangs rather than fails — which is
    /// exactly why it exists.
    /// </summary>
    [Fact]
    public async Task Self_referencing_parent_does_not_loop_forever()
    {
        var selfParent = Guid.NewGuid();

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VAHED_INFOs.Add(Unit(selfParent, "3000", OrdinaryTypeId, selfParent));
            seedContext.SaveChanges();
        }

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync("3000");

        Assert.Equal(new[] { "3000" }, result.Select(u => u.VahedCode).ToArray());
    }

    /// <summary>The two-row version of the cycle above: A is B's parent and B is A's parent.</summary>
    [Fact]
    public async Task Mutual_parent_cycle_does_not_loop_forever()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VAHED_INFOs.AddRange(
                Unit(first, "4000", OrdinaryTypeId, second),
                Unit(second, "4100", OrdinaryTypeId, first));
            seedContext.SaveChanges();
        }

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var result = await repository.GetAccessibleUnitsAsync("4000");

        Assert.Equal(new[] { "4000", "4100" }, result.Select(u => u.VahedCode).ToArray());
    }

    [Fact]
    public async Task Unit_profile_reports_headquarters_for_the_hq_type()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var profile = await repository.GetUnitProfileAsync("0000");

        Assert.NotNull(profile);
        Assert.True(profile!.IsHeadquarters);
        Assert.Equal("واحد 0000", profile.VahedName);
    }

    [Fact]
    public async Task Unit_profile_reports_non_headquarters_for_an_ordinary_type()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        var profile = await repository.GetUnitProfileAsync("1000");

        Assert.NotNull(profile);
        Assert.False(profile!.IsHeadquarters);
    }

    [Fact]
    public async Task Can_act_as_self()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        Assert.True(await repository.CanActAsAsync("1000", "1000"));
    }

    [Fact]
    public async Task Can_act_as_a_direct_child()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        Assert.True(await repository.CanActAsAsync("1000", "1100"));
    }

    [Fact]
    public async Task Can_act_as_a_grandchild()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        // The ancestor walk must climb more than one step. Pairs with
        // Ordinary_unit_reaches_its_whole_subtree_not_only_direct_children.
        Assert.True(await repository.CanActAsAsync("1000", "1110"));
    }

    [Fact]
    public async Task Cannot_act_as_a_sibling_branch()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        Assert.False(await repository.CanActAsAsync("1000", "2000"));
        Assert.False(await repository.CanActAsAsync("1000", "2100"));
    }

    [Fact]
    public async Task Cannot_act_as_your_own_parent()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        Assert.False(await repository.CanActAsAsync("1100", "1000"));
    }

    [Fact]
    public async Task Headquarters_can_act_as_any_unit_even_outside_its_own_branch()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        // On live data ستاد مرکزی has only 7 direct children out of 1046 units, so this is NOT a
        // tree result — it is the TYPECODE 17 rule, and without it HQ would reach almost nothing.
        Assert.True(await repository.CanActAsAsync("0000", "1110"));
        Assert.True(await repository.CanActAsAsync("0000", "2100"));
    }

    [Fact]
    public async Task Headquarters_still_cannot_act_as_a_unit_that_does_not_exist()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        // Otherwise a typo'd unit code would become a valid scope for the one role that reaches
        // everything, and every downstream query would silently match zero rows.
        Assert.False(await repository.CanActAsAsync("0000", "9999"));
    }

    [Fact]
    public async Task Cannot_act_as_an_unknown_unit()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        Assert.False(await repository.CanActAsAsync("1000", "9999"));
        Assert.False(await repository.CanActAsAsync("9999", "1000"));
    }

    [Fact]
    public async Task Cycle_in_the_ancestor_chain_denies_rather_than_hanging()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var outsider = Guid.NewGuid();

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VAHED_INFOs.AddRange(
                Unit(first, "4000", OrdinaryTypeId, second),
                Unit(second, "4100", OrdinaryTypeId, first),
                Unit(outsider, "5000", OrdinaryTypeId));
            seedContext.SaveChanges();
        }

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        // Walking up from 4000 loops 4000 → 4100 → 4000 forever without the visited set.
        Assert.False(await repository.CanActAsAsync("5000", "4000"));
    }

    [Fact]
    public async Task Unit_profile_is_null_for_an_unknown_code()
    {
        SeedTree();

        using var context = CreateContext();
        var repository = new UnitAccessReadRepository(context);

        Assert.Null(await repository.GetUnitProfileAsync("9999"));
    }
}
