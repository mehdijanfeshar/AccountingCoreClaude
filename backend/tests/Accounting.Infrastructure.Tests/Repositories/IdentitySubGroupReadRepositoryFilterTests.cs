using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) repository tests for the two filters the شناسنامه entry form depends on:
/// "the FIXED subgroups of group X" — the reference app's <c>getFixed?Groupid=</c> call.
///
/// These two filters are worth testing against a real provider rather than a mock because
/// <c>FIXED</c> is an enum mapped with an explicit value converter; comparing it in a predicate
/// is exactly the shape that threw <c>InvalidCastException</c> on Oracle before phase 25's
/// mapping fix (see <see cref="LegacyEnumMappingConventionTests"/>).
/// </summary>
public sealed class IdentitySubGroupReadRepositoryFilterTests : IDisposable
{
    private const string CreateTableSql = """
        CREATE TABLE TB_IDENTITYSUBGRPS (
            ID TEXT PRIMARY KEY,
            IDENTYGROUPS_ID TEXT,
            SUBGRPS_DESC TEXT,
            SUBGRPS_LEN INTEGER,
            SUMFLAG INTEGER,
            FIXED INTEGER,
            SUBGRPS_TYPE INTEGER,
            CREATEDDATE TEXT,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT,
            CHANGEUSERID TEXT,
            VAHEDCODE TEXT,
            YEAR TEXT,
            ISDELETED INTEGER,
            IDENTYSUBGROUPS_CODE TEXT
        );
        """;

    private const string Vahed = "1155";
    private const string OtherVahed = "9999";

    private readonly SqliteConnection _connection;

    public IdentitySubGroupReadRepositoryFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateTableSql);
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LegacyDbContext(options);
    }

    private void Seed(
        Guid groupId,
        string desc,
        IdentitySubGroupKind kind,
        string vahedCode = Vahed,
        bool isDeleted = false)
    {
        using var context = CreateContext();

        context.TB_IDENTITYSUBGRPs.Add(new TB_IDENTITYSUBGRP
        {
            ID = Guid.NewGuid(),
            IDENTYGROUPS_ID = groupId,
            SUBGRPS_DESC = desc,
            SUBGRPS_LEN = 10,
            SUMFLAG = false,
            FIXED = kind,
            SUBGRPS_TYPE = IdentitySubGroupType.Number,
            CREATEDDATE = new DateTime(2025, 1, 1).AddMinutes(desc.Length),
            ADDUSERID = "tester",
            VAHEDCODE = vahedCode,
            YEAR = "1404",
            ISDELETED = isDeleted,
        });

        context.SaveChanges();
    }

    private async Task<IReadOnlyList<string>> DescriptionsAsync(Guid? groupId, IdentitySubGroupKind? kind)
    {
        using var context = CreateContext();
        var repository = new IdentitySubGroupReadRepository(context);

        var result = await repository.GetPagedAsync(1, 50, Vahed, groupId, kind);

        return result.Items.Select(i => i.SubgrpsDesc).OrderBy(d => d).ToList();
    }

    [Fact]
    public async Task NoFilters_ReturnEverySubGroupOfTheUnit()
    {
        var group = Guid.NewGuid();
        Seed(group, "الف", IdentitySubGroupKind.Fixed);
        Seed(Guid.NewGuid(), "ب", IdentitySubGroupKind.Variable);

        var descriptions = await DescriptionsAsync(null, null);

        Assert.Equal(2, descriptions.Count);
    }

    [Fact]
    public async Task GroupFilter_ExcludesOtherGroups()
    {
        var wanted = Guid.NewGuid();
        Seed(wanted, "الف", IdentitySubGroupKind.Fixed);
        Seed(Guid.NewGuid(), "ب", IdentitySubGroupKind.Fixed);

        var descriptions = await DescriptionsAsync(wanted, null);

        Assert.Equal(new[] { "الف" }, descriptions);
    }

    [Fact]
    public async Task KindFilter_SeparatesFixedFromChangeable()
    {
        var group = Guid.NewGuid();
        Seed(group, "ثابت", IdentitySubGroupKind.Fixed);
        Seed(group, "متغیر", IdentitySubGroupKind.Variable);

        var fixedOnly = await DescriptionsAsync(group, IdentitySubGroupKind.Fixed);
        var changeableOnly = await DescriptionsAsync(group, IdentitySubGroupKind.Variable);

        Assert.Equal(new[] { "ثابت" }, fixedOnly);
        Assert.Equal(new[] { "متغیر" }, changeableOnly);
    }

    /// <summary>
    /// The exact combination the شناسنامه entry form issues.
    /// </summary>
    [Fact]
    public async Task GroupAndKindTogether_ReturnOnlyThatGroupsFixedSubGroups()
    {
        var wanted = Guid.NewGuid();
        var other = Guid.NewGuid();
        Seed(wanted, "ثابتِ گروه درست", IdentitySubGroupKind.Fixed);
        Seed(wanted, "متغیرِ گروه درست", IdentitySubGroupKind.Variable);
        Seed(other, "ثابتِ گروه دیگر", IdentitySubGroupKind.Fixed);

        var descriptions = await DescriptionsAsync(wanted, IdentitySubGroupKind.Fixed);

        Assert.Equal(new[] { "ثابتِ گروه درست" }, descriptions);
    }

    [Fact]
    public async Task UnitScope_StillApplies_WhenFiltersAreSupplied()
    {
        var group = Guid.NewGuid();
        Seed(group, "مالِ من", IdentitySubGroupKind.Fixed);
        Seed(group, "مالِ واحد دیگر", IdentitySubGroupKind.Fixed, vahedCode: OtherVahed);

        var descriptions = await DescriptionsAsync(group, IdentitySubGroupKind.Fixed);

        Assert.Equal(new[] { "مالِ من" }, descriptions);
    }

    [Fact]
    public async Task DeletedRows_AreExcluded_WhenFiltersAreSupplied()
    {
        var group = Guid.NewGuid();
        Seed(group, "زنده", IdentitySubGroupKind.Fixed);
        Seed(group, "حذف‌شده", IdentitySubGroupKind.Fixed, isDeleted: true);

        var descriptions = await DescriptionsAsync(group, IdentitySubGroupKind.Fixed);

        Assert.Equal(new[] { "زنده" }, descriptions);
    }

    [Fact]
    public async Task TotalCount_ReflectsTheFilter_NotTheWholeTable()
    {
        var wanted = Guid.NewGuid();
        Seed(wanted, "الف", IdentitySubGroupKind.Fixed);
        Seed(Guid.NewGuid(), "ب", IdentitySubGroupKind.Fixed);
        Seed(Guid.NewGuid(), "ج", IdentitySubGroupKind.Fixed);

        using var context = CreateContext();
        var repository = new IdentitySubGroupReadRepository(context);

        var result = await repository.GetPagedAsync(1, 50, Vahed, wanted, IdentitySubGroupKind.Fixed);

        Assert.Equal(1, result.TotalCount);
    }
}
