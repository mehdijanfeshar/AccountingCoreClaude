using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) tests for <c>GetNextSerialAsync</c>. Worth a real query because the whole
/// method is an aggregate over a filtered set, and because its most important behaviour —
/// counting soft-deleted rows — is the kind of thing a mock cannot demonstrate.
/// </summary>
public sealed class IdentityHeadRepositorySerialTests : IDisposable
{
    private const string CreateTableSql = """
        CREATE TABLE TB_IDENTITYHEAD (
            ID TEXT PRIMARY KEY,
            IDENTITYGROUPS_ID TEXT,
            SERIAL INTEGER,
            CREATEDDATE TEXT,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT,
            CHANGEUSERID TEXT,
            VAHEDCODE TEXT,
            YEAR TEXT,
            ISDELETED INTEGER
        );
        """;

    private const string Vahed = "1155";
    private const string Year = "1404";

    private readonly SqliteConnection _connection;

    public IdentityHeadRepositorySerialTests()
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

    private void Seed(Guid groupId, int serial, string vahedCode = Vahed, string year = Year, bool isDeleted = false)
    {
        using var context = CreateContext();

        context.TB_IDENTITYHEADs.Add(new TB_IDENTITYHEAD
        {
            ID = Guid.NewGuid(),
            IDENTITYGROUPS_ID = groupId,
            SERIAL = serial,
            CREATEDDATE = new DateTime(2025, 1, 1),
            ADDUSERID = "tester",
            VAHEDCODE = vahedCode,
            YEAR = year,
            ISDELETED = isDeleted,
        });

        context.SaveChanges();
    }

    private async Task<int> NextSerialAsync(Guid groupId, string vahedCode = Vahed, string year = Year)
    {
        using var context = CreateContext();
        var repository = new IdentityHeadRepository(context);

        return await repository.GetNextSerialAsync(groupId, vahedCode, year);
    }

    [Fact]
    public async Task FirstSerialOfAGroup_IsOne()
    {
        Assert.Equal(1, await NextSerialAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task NextSerial_IsOnePastTheHighest()
    {
        var group = Guid.NewGuid();
        Seed(group, 1);
        Seed(group, 5);
        Seed(group, 3);

        Assert.Equal(6, await NextSerialAsync(group));
    }

    /// <summary>
    /// Serials are per group, so a busy group must not push another group's numbering along.
    /// </summary>
    [Fact]
    public async Task SerialsAreScopedPerGroup()
    {
        var busy = Guid.NewGuid();
        var quiet = Guid.NewGuid();
        Seed(busy, 9);

        Assert.Equal(1, await NextSerialAsync(quiet));
    }

    [Fact]
    public async Task SerialsAreScopedPerUnit()
    {
        var group = Guid.NewGuid();
        Seed(group, 9, vahedCode: "9999");

        Assert.Equal(1, await NextSerialAsync(group));
    }

    [Fact]
    public async Task SerialsAreScopedPerYear()
    {
        var group = Guid.NewGuid();
        Seed(group, 9, year: "1403");

        Assert.Equal(1, await NextSerialAsync(group));
    }

    /// <summary>
    /// The one behaviour that is easy to get wrong: a soft-deleted head still owns its serial, because
    /// Oracle enforces AK_AK_IDENTYHEAD_IDENTYHE regardless of ISDELETED. Skipping it would hand
    /// out a value that then collides on insert. Serials are monotonic and may have gaps.
    /// </summary>
    [Fact]
    public async Task DeletedHeads_StillConsumeTheirSerial()
    {
        var group = Guid.NewGuid();
        Seed(group, 4, isDeleted: true);

        Assert.Equal(5, await NextSerialAsync(group));
    }
}
