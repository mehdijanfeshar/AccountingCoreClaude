using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real EF Core translation tests for the تفصیلی links now exposed on <c>VoucherDetailDto</c>.
///
/// <para>
/// This projection is the kind that looks obviously right and is easy to get wrong: a correlated
/// sub-select inside an <see cref="System.Linq.Expressions.Expression"/> has to translate to SQL
/// rather than fall back to client evaluation, and the soft-delete predicate has to be inside it.
/// A mocked repository proves neither. SQLite in-memory is this project's established stand-in —
/// never live Oracle.
/// </para>
/// </summary>
public sealed class VoucherDetailReadRepositoryTafsiliLinkTests : IDisposable
{
    private const string CreateVoucherDetailTableSql = """
        CREATE TABLE TB_VOUCHERSDETAIL (
            ID TEXT PRIMARY KEY,
            VOUCHERSHEAD_ID TEXT,
            ACCOUNT_ID TEXT,
            RECEIP_ID TEXT,
            CHECK_ID TEXT,
            LOWLEVELCODE_ID TEXT,
            ETEBAR_ID TEXT,
            DESCRIPTION TEXT,
            RADIF INTEGER,
            DEBTOR TEXT,
            CREDITOR TEXT,
            CREATEDDATE TEXT NOT NULL,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT NOT NULL,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER,
            VAHEDCODE TEXT,
            YEAR TEXT
        );
        """;

    private const string CreateLinkTableSql = """
        CREATE TABLE TB_VOUCHERDETAIL_LINK_TAFSILI (
            ID TEXT PRIMARY KEY,
            VOUCHERSDETAIL_ID TEXT NOT NULL,
            TAFSILI_ID TEXT NOT NULL,
            LEVEL_ID TEXT NOT NULL,
            CREATEDDATE TEXT NOT NULL,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT NOT NULL,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER NOT NULL,
            VAHEDCODE TEXT,
            YEAR TEXT
        );
        """;

    private static readonly Guid DetailId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherDetailId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string Unit = "0042";

    private readonly SqliteConnection _connection;

    public VoucherDetailReadRepositoryTafsiliLinkTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setup = CreateContext();
        setup.Database.ExecuteSqlRaw(CreateVoucherDetailTableSql);
        setup.Database.ExecuteSqlRaw(CreateLinkTableSql);
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>().UseSqlite(_connection).Options;
        return new LegacyDbContext(options);
    }

    private static TB_VOUCHERSDETAIL Detail(Guid id) => new()
    {
        ID = id,
        VOUCHERSHEAD_ID = Guid.NewGuid(),
        CREATEDDATE = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ADDUSERID = "seed",
        ISDELETED = false,
        VAHEDCODE = Unit,
        YEAR = "1404",
    };

    private static TB_VOUCHERDETAIL_LINK_TAFSILI Link(Guid detailId, Guid tafsiliId, Guid levelId, bool isDeleted = false) => new()
    {
        ID = Guid.NewGuid(),
        VOUCHERSDETAIL_ID = detailId,
        TAFSILI_ID = tafsiliId,
        LEVEL_ID = levelId,
        CREATEDDATE = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ADDUSERID = "seed",
        ISDELETED = isDeleted,
        VAHEDCODE = Unit,
        YEAR = "1404",
    };

    private async Task SeedAsync(params object[] entities)
    {
        await using var context = CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsActiveLinks()
    {
        var tafsili = Guid.NewGuid();
        var level = Guid.NewGuid();
        await SeedAsync(Detail(DetailId), Link(DetailId, tafsili, level));

        await using var context = CreateContext();
        var dto = await new VoucherDetailReadRepository(context).GetByIdAsync(DetailId, Unit);

        var link = Assert.Single(dto!.TafsiliLinks);
        Assert.Equal(tafsili, link.TafsiliId);
        Assert.Equal(level, link.LevelId);
    }

    [Fact]
    public async Task GetByIdAsync_ExcludesSoftDeletedLinks()
    {
        // The whole reason an edit form needs this data is to know what currently applies. A link
        // someone removed last week is not that, and returning it would have the form faithfully
        // write it back.
        var kept = Guid.NewGuid();
        await SeedAsync(
            Detail(DetailId),
            Link(DetailId, kept, Guid.NewGuid()),
            Link(DetailId, Guid.NewGuid(), Guid.NewGuid(), isDeleted: true));

        await using var context = CreateContext();
        var dto = await new VoucherDetailReadRepository(context).GetByIdAsync(DetailId, Unit);

        Assert.Equal(kept, Assert.Single(dto!.TafsiliLinks).TafsiliId);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotBorrowAnotherLinesLinks()
    {
        var mine = Guid.NewGuid();
        await SeedAsync(
            Detail(DetailId),
            Detail(OtherDetailId),
            Link(DetailId, mine, Guid.NewGuid()),
            Link(OtherDetailId, Guid.NewGuid(), Guid.NewGuid()));

        await using var context = CreateContext();
        var dto = await new VoucherDetailReadRepository(context).GetByIdAsync(DetailId, Unit);

        Assert.Equal(mine, Assert.Single(dto!.TafsiliLinks).TafsiliId);
    }

    [Fact]
    public async Task ALineWithNoLinks_ReturnsAnEmptyList_NotNull()
    {
        // Empty and unknown are different answers, and the form branches on that: empty means
        // "send an empty list and clear them", null would mean "leave them alone".
        await SeedAsync(Detail(DetailId));

        await using var context = CreateContext();
        var dto = await new VoucherDetailReadRepository(context).GetByIdAsync(DetailId, Unit);

        Assert.NotNull(dto!.TafsiliLinks);
        Assert.Empty(dto.TafsiliLinks);
    }

    [Fact]
    public async Task ListProjection_CarriesLinksToo()
    {
        // The edit form loads a voucher's lines through the list query, not one by one, so the
        // links have to survive that projection as well.
        var tafsili = Guid.NewGuid();
        await SeedAsync(Detail(DetailId), Link(DetailId, tafsili, Guid.NewGuid()));

        await using var context = CreateContext();
        var page = await new VoucherDetailReadRepository(context)
            .GetPagedAsync(1, 20, null, null, Unit);

        var row = Assert.Single(page.Items);
        Assert.Equal(tafsili, Assert.Single(row.TafsiliLinks).TafsiliId);
    }
}
