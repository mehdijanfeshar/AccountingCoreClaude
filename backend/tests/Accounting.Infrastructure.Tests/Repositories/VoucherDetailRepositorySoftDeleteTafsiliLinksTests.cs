using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) repository-level tests for
/// <see cref="VoucherDetailRepository.SoftDeleteTafsiliLinksAsync"/>, exercising the actual EF
/// Core LINQ query and SQL translation against <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> — the mocked
/// handler tests in <c>Accounting.Application.Tests</c> only verify
/// <c>DeleteVoucherDetailCommandHandler</c> CALLS this method with the right arguments/ordering;
/// they never execute the real <c>Where(l =&gt; l.VOUCHERSDETAIL_ID == detailId &amp;&amp;
/// l.ISDELETED == false)</c> predicate.
///
/// Structure, provider choice (SQLite in-memory, NOT EF Core InMemory) and the hand-written
/// <c>CREATE TABLE</c> workaround are all copied verbatim in rationale from
/// <c>VoucherHeadRepositorySoftDeleteDetailLinesTests</c> — see that file's class-level XML doc
/// for the full justification (SQLite does not support the <c>HasSequence</c> the full
/// <see cref="LegacyDbContext"/> model declares, so <c>EnsureCreated()</c> cannot be used against
/// the whole context).
///
/// Unlike the head-level cascade, <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI.ISDELETED"/> is a
/// non-nullable <see cref="bool"/> (verified against the entity/Fluent Mapping — see
/// <c>VoucherDetailRepository.SoftDeleteTafsiliLinksAsync</c>'s own XML doc), so the filter under
/// test here is a plain <c>== false</c>, with no NULL branch to prove — that is the one behavioural
/// difference from the head-level fixture's "NULL treated as not-deleted" test.
/// </summary>
public sealed class VoucherDetailRepositorySoftDeleteTafsiliLinksTests : IDisposable
{
    // Mirrors every column of TB_VOUCHERDETAIL_LINK_TAFSILI (see the entity/Fluent Mapping).
    // ISDELETED is declared NOT NULL here (unlike the head-cascade fixture's copy of the same
    // table) to mirror the entity's non-nullable bool ISDELETED property.
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

    public VoucherDetailRepositorySoftDeleteTafsiliLinksTests()
    {
        // A single open connection is reused for the whole test's lifetime: SQLite's ":memory:"
        // database only lives as long as the connection that created it stays open. A fresh
        // DbContext is opened per "phase" (seed / act / assert) so mutations are proven to be
        // actually persisted via SaveChangesAsync, not merely visible on one context's change
        // tracker.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateVoucherDetailLinkTafsiliTableSql);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private LegacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new LegacyDbContext(options);
    }

    private static TB_VOUCHERDETAIL_LINK_TAFSILI Link(
        Guid detailId,
        bool isDeleted,
        string? changeUserId = null,
        DateTime? updatedDate = null) => new()
        {
            ID = Guid.NewGuid(),
            VOUCHERSDETAIL_ID = detailId,
            TAFSILI_ID = Guid.NewGuid(),
            LEVEL_ID = Guid.NewGuid(),
            CREATEDDATE = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ADDUSERID = "seed-user",
            ISDELETED = isDeleted,
            CHANGEUSERID = changeUserId,
            UPDATEDDATE = updatedDate,
        };

    [Fact]
    public async Task DetailWithMultipleNonDeletedLinks_AllSoftDeleted_WithStampedAuditFields_AndCorrectReturnCount()
    {
        var detailId = Guid.NewGuid();
        var links = new[]
        {
            Link(detailId, isDeleted: false),
            Link(detailId, isDeleted: false),
            Link(detailId, isDeleted: false),
        };

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRange(links);
            await seedContext.SaveChangesAsync();
        }

        var stampedUser = "deleter-42";
        var stampedDate = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherDetailRepository(actContext);

            var affected = await repository.SoftDeleteTafsiliLinksAsync(detailId, stampedUser, stampedDate);
            await actContext.SaveChangesAsync();

            Assert.Equal(3, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persisted = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs
                .Where(l => l.VOUCHERSDETAIL_ID == detailId)
                .ToListAsync();

            Assert.Equal(3, persisted.Count);
            Assert.All(persisted, l =>
            {
                Assert.True(l.ISDELETED);
                Assert.Equal(stampedUser, l.CHANGEUSERID);
                Assert.Equal(stampedDate, l.UPDATEDDATE);
            });
        }
    }

    [Fact]
    public async Task DetailWithZeroLinkRows_ReturnsZero_NoCrash()
    {
        var detailId = Guid.NewGuid();

        using var context = CreateContext();
        var repository = new VoucherDetailRepository(context);

        var affected = await repository.SoftDeleteTafsiliLinksAsync(detailId, "someone", DateTime.UtcNow);
        await context.SaveChangesAsync();

        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task AlreadySoftDeletedLinks_AreNotRetouched_AuditFieldsRemainByteIdentical_AndNotCounted()
    {
        var detailId = Guid.NewGuid();
        var originalUser = "original-editor";
        var originalDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var alreadyDeleted = Link(detailId, isDeleted: true, changeUserId: originalUser, updatedDate: originalDate);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.Add(alreadyDeleted);
            await seedContext.SaveChangesAsync();
        }

        using (var actContext = CreateContext())
        {
            var repository = new VoucherDetailRepository(actContext);

            var affected = await repository.SoftDeleteTafsiliLinksAsync(
                detailId, "new-editor", new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc));
            await actContext.SaveChangesAsync();

            Assert.Equal(0, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persisted = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.SingleAsync(l => l.ID == alreadyDeleted.ID);

            Assert.True(persisted.ISDELETED);
            Assert.Equal(originalUser, persisted.CHANGEUSERID);
            Assert.Equal(originalDate, persisted.UPDATEDDATE);
        }
    }

    [Fact]
    public async Task LinksBelongingToDifferentDetailRow_AreNotTouched_Isolation()
    {
        var targetDetailId = Guid.NewGuid();
        var otherDetailId = Guid.NewGuid();

        var targetLink = Link(targetDetailId, isDeleted: false);
        var otherLink = Link(otherDetailId, isDeleted: false);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRange(targetLink, otherLink);
            await seedContext.SaveChangesAsync();
        }

        var stampedUser = "deleter-isolation";
        var stampedDate = new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherDetailRepository(actContext);

            var affected = await repository.SoftDeleteTafsiliLinksAsync(targetDetailId, stampedUser, stampedDate);
            await actContext.SaveChangesAsync();

            Assert.Equal(1, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persistedTarget = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.SingleAsync(l => l.ID == targetLink.ID);
            var persistedOther = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.SingleAsync(l => l.ID == otherLink.ID);

            Assert.True(persistedTarget.ISDELETED);
            Assert.Equal(stampedUser, persistedTarget.CHANGEUSERID);
            Assert.Equal(stampedDate, persistedTarget.UPDATEDDATE);

            Assert.False(persistedOther.ISDELETED);
            Assert.Null(persistedOther.CHANGEUSERID);
            Assert.Null(persistedOther.UPDATEDDATE);
        }
    }

    [Fact]
    public async Task MixedActiveAndAlreadyDeletedLinks_OnSameDetail_OnlyActiveOnesAreSoftDeleted_ReturnCountMatchesActiveOnly()
    {
        var detailId = Guid.NewGuid();
        var active1 = Link(detailId, isDeleted: false);
        var active2 = Link(detailId, isDeleted: false);
        var alreadyDeleted = Link(detailId, isDeleted: true, changeUserId: "old", updatedDate: new DateTime(2018, 1, 1));

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRange(active1, active2, alreadyDeleted);
            await seedContext.SaveChangesAsync();
        }

        var stampedUser = "mixed-case-deleter";
        var stampedDate = new DateTime(2026, 8, 21, 11, 0, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherDetailRepository(actContext);

            var affected = await repository.SoftDeleteTafsiliLinksAsync(detailId, stampedUser, stampedDate);
            await actContext.SaveChangesAsync();

            Assert.Equal(2, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persistedActive1 = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.SingleAsync(l => l.ID == active1.ID);
            var persistedActive2 = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.SingleAsync(l => l.ID == active2.ID);
            var persistedAlreadyDeleted = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.SingleAsync(l => l.ID == alreadyDeleted.ID);

            Assert.True(persistedActive1.ISDELETED);
            Assert.Equal(stampedUser, persistedActive1.CHANGEUSERID);
            Assert.Equal(stampedDate, persistedActive1.UPDATEDDATE);

            Assert.True(persistedActive2.ISDELETED);
            Assert.Equal(stampedUser, persistedActive2.CHANGEUSERID);
            Assert.Equal(stampedDate, persistedActive2.UPDATEDDATE);

            // Untouched — byte-identical to the seeded values.
            Assert.True(persistedAlreadyDeleted.ISDELETED);
            Assert.Equal("old", persistedAlreadyDeleted.CHANGEUSERID);
            Assert.Equal(new DateTime(2018, 1, 1), persistedAlreadyDeleted.UPDATEDDATE);
        }
    }

    [Fact]
    public async Task ReturnedTask_PersistsOnlyAfterCallerCallsSaveChangesAsync_ProvenWithFreshContext()
    {
        // Explicit proof that SoftDeleteTafsiliLinksAsync only STAGES the change (matches its own
        // XML doc: "Does NOT call IUnitOfWork.SaveChangesAsync") — a fresh, unrelated DbContext
        // must see the row still active until the caller's own SaveChangesAsync runs.
        var detailId = Guid.NewGuid();
        var link = Link(detailId, isDeleted: false);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.Add(link);
            await seedContext.SaveChangesAsync();
        }

        using var actContext = CreateContext();
        var repository = new VoucherDetailRepository(actContext);

        var affected = await repository.SoftDeleteTafsiliLinksAsync(detailId, "staged-only", DateTime.UtcNow);
        Assert.Equal(1, affected);

        using (var beforeSaveContext = CreateContext())
        {
            var stillActive = await beforeSaveContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.SingleAsync(l => l.ID == link.ID);
            Assert.False(stillActive.ISDELETED);
        }

        await actContext.SaveChangesAsync();

        using (var afterSaveContext = CreateContext())
        {
            var nowDeleted = await afterSaveContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.SingleAsync(l => l.ID == link.ID);
            Assert.True(nowDeleted.ISDELETED);
        }
    }
}
