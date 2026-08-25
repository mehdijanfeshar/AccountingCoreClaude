using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) repository-level tests for the phase-11 تفصیلی WRITE path —
/// <see cref="VoucherDetailRepository.AddTafsiliLinkAsync"/> and
/// <see cref="VoucherDetailRepository.GetActiveTafsiliLinksAsync"/> — exercising the actual EF Core
/// insert and the real <c>Where(l =&gt; l.VOUCHERSDETAIL_ID == detailId &amp;&amp; l.ISDELETED == false)</c>
/// SQL translation. The mocked handler tests in <c>Accounting.Application.Tests</c> only prove the
/// handlers CALL these methods correctly; they never execute a query.
///
/// Fixture structure and provider choice (SQLite in-memory, NOT EF Core InMemory, with a
/// hand-written <c>CREATE TABLE</c> because <c>EnsureCreated()</c> fails on the full
/// <see cref="LegacyDbContext"/> model's <c>HasSequence</c>) are copied in rationale from
/// <see cref="VoucherDetailRepositorySoftDeleteTafsiliLinksTests"/> — see that file's class-level
/// XML doc.
///
/// ⚠️ Same standing limitation as every other test in this project: SQLite, never live Oracle, so
/// Oracle-specific behaviour (including the <c>CHAR(36)</c> GUID converter's real column semantics)
/// is not proven here.
/// </summary>
public sealed class VoucherDetailRepositoryTafsiliLinkWriteTests : IDisposable
{
    // Mirrors every column of TB_VOUCHERDETAIL_LINK_TAFSILI; ISDELETED is NOT NULL to match the
    // entity's non-nullable bool property.
    private const string CreateVoucherDetailLinkTafsiliTableSql = """
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

    private readonly SqliteConnection _connection;

    public VoucherDetailRepositoryTafsiliLinkWriteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateVoucherDetailLinkTafsiliTableSql);
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LegacyDbContext(options);
    }

    private static TB_VOUCHERDETAIL_LINK_TAFSILI Link(
        Guid detailId,
        bool isDeleted = false,
        Guid? tafsiliId = null,
        Guid? levelId = null) => new()
        {
            ID = Guid.NewGuid(),
            VOUCHERSDETAIL_ID = detailId,
            TAFSILI_ID = tafsiliId ?? Guid.NewGuid(),
            LEVEL_ID = levelId ?? Guid.NewGuid(),
            CREATEDDATE = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ADDUSERID = "seed-user",
            ISDELETED = isDeleted,
            VAHEDCODE = "0001",
            YEAR = "1405",
        };

    [Fact]
    public async Task AddTafsiliLinkAsync_StagesRow_ThatIsPersistedByTheCallersSaveChanges()
    {
        var detailId = Guid.NewGuid();
        var link = Link(detailId);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherDetailRepository(actContext);

            await repository.AddTafsiliLinkAsync(link);

            // The repository must only STAGE — nothing may reach the database until the handler's
            // own SaveChangesAsync, which is what keeps the line and its links in one transaction.
            using (var beforeSaveContext = CreateContext())
            {
                Assert.Empty(await beforeSaveContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.ToListAsync());
            }

            await actContext.SaveChangesAsync();
        }

        using (var assertContext = CreateContext())
        {
            var persisted = Assert.Single(await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.ToListAsync());
            Assert.Equal(link.ID, persisted.ID);
            Assert.Equal(detailId, persisted.VOUCHERSDETAIL_ID);
            Assert.Equal(link.TAFSILI_ID, persisted.TAFSILI_ID);
            Assert.Equal(link.LEVEL_ID, persisted.LEVEL_ID);
            Assert.Equal("seed-user", persisted.ADDUSERID);
            Assert.False(persisted.ISDELETED);
        }
    }

    [Fact]
    public async Task GetActiveTafsiliLinksAsync_ReturnsOnlyNonDeletedLinksOfThatDetail()
    {
        var detailId = Guid.NewGuid();
        var otherDetailId = Guid.NewGuid();
        var active1 = Link(detailId);
        var active2 = Link(detailId);
        var deleted = Link(detailId, isDeleted: true);
        var otherDetailsLink = Link(otherDetailId);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRange(active1, active2, deleted, otherDetailsLink);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var result = await new VoucherDetailRepository(actContext).GetActiveTafsiliLinksAsync(detailId);

        // Soft-deleted rows must not resurface (they would otherwise be "kept" by the update
        // reconcile), and another line's links must never be touched.
        Assert.Equal(2, result.Count);
        Assert.Contains(result, l => l.ID == active1.ID);
        Assert.Contains(result, l => l.ID == active2.ID);
        Assert.DoesNotContain(result, l => l.ID == deleted.ID);
        Assert.DoesNotContain(result, l => l.ID == otherDetailsLink.ID);
    }

    [Fact]
    public async Task GetActiveTafsiliLinksAsync_DetailWithNoLinks_ReturnsEmptyNotNull()
    {
        using var actContext = CreateContext();

        var result = await new VoucherDetailRepository(actContext).GetActiveTafsiliLinksAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// The returned entities must be change-tracked (no <c>AsNoTracking()</c>), because the update
    /// reconcile soft-deletes dropped links by mutating these instances in place and relies on the
    /// handler's single <c>SaveChangesAsync</c> to persist them. Proven by mutating and saving,
    /// then re-reading through a FRESH context.
    /// </summary>
    [Fact]
    public async Task GetActiveTafsiliLinksAsync_ReturnsTrackedEntities_SoInPlaceMutationPersists()
    {
        var detailId = Guid.NewGuid();
        var link = Link(detailId);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.Add(link);
            await seedContext.SaveChangesAsync();
        }

        var stampedDate = new DateTime(2026, 8, 25, 9, 30, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var loaded = await new VoucherDetailRepository(actContext).GetActiveTafsiliLinksAsync(detailId);

            var single = Assert.Single(loaded);
            single.ISDELETED = true;
            single.CHANGEUSERID = "editor9";
            single.UPDATEDDATE = stampedDate;

            await actContext.SaveChangesAsync();
        }

        using (var assertContext = CreateContext())
        {
            var persisted = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.SingleAsync(l => l.ID == link.ID);
            Assert.True(persisted.ISDELETED);
            Assert.Equal("editor9", persisted.CHANGEUSERID);
            Assert.Equal(stampedDate, persisted.UPDATEDDATE);
        }
    }

    /// <summary>
    /// End-to-end shape of the update reconcile at the repository level: drop one link, keep one,
    /// add one — all staged against a single context and flushed by one SaveChangesAsync.
    /// </summary>
    [Fact]
    public async Task ReconcileShape_DropKeepAdd_AllPersistInOneSaveChanges()
    {
        var detailId = Guid.NewGuid();
        var keptTafsili = Guid.NewGuid();
        var keptLevel = Guid.NewGuid();
        var kept = Link(detailId, tafsiliId: keptTafsili, levelId: keptLevel);
        var dropped = Link(detailId);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRange(kept, dropped);
            await seedContext.SaveChangesAsync();
        }

        var added = Link(detailId);
        var stampedDate = new DateTime(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherDetailRepository(actContext);

            var existing = await repository.GetActiveTafsiliLinksAsync(detailId);
            var toDrop = existing.Single(l => l.ID == dropped.ID);
            toDrop.ISDELETED = true;
            toDrop.CHANGEUSERID = "editor9";
            toDrop.UPDATEDDATE = stampedDate;

            await repository.AddTafsiliLinkAsync(added);

            await actContext.SaveChangesAsync();
        }

        using (var assertContext = CreateContext())
        {
            var all = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs
                .Where(l => l.VOUCHERSDETAIL_ID == detailId)
                .ToListAsync();

            Assert.Equal(3, all.Count);

            var persistedKept = all.Single(l => l.ID == kept.ID);
            Assert.False(persistedKept.ISDELETED);
            Assert.Null(persistedKept.CHANGEUSERID);
            Assert.Null(persistedKept.UPDATEDDATE);

            Assert.True(all.Single(l => l.ID == dropped.ID).ISDELETED);
            Assert.False(all.Single(l => l.ID == added.ID).ISDELETED);

            // And the post-reconcile active set is exactly {kept, added}.
            var stillActive = await new VoucherDetailRepository(assertContext).GetActiveTafsiliLinksAsync(detailId);
            Assert.Equal(2, stillActive.Count);
            Assert.Contains(stillActive, l => l.ID == kept.ID);
            Assert.Contains(stillActive, l => l.ID == added.ID);
        }
    }
}
