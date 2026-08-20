using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Real (non-mocked) repository-level tests for
/// <see cref="VoucherHeadRepository.SoftDeleteDetailTreeAsync"/>, exercising the actual EF Core
/// LINQ query and SQL translation against <c>TB_VOUCHERSDETAIL</c> — the mocked handler tests in
/// <c>Accounting.Application.Tests</c> only verify the handler CALLS this method with the right
/// arguments/ordering; they never execute the real <c>Where(d =&gt; ... d.ISDELETED == null ||
/// d.ISDELETED == false)</c> predicate.
///
/// This fixture also creates an empty <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> table so the level-3
/// query inside <c>SoftDeleteDetailTreeAsync</c> has something to run against (SQLite errors on
/// "no such table" otherwise). None of the tests here seed rows into it or assert on it — the
/// existing suite only ever exercised level 2 (<c>TB_VOUCHERSDETAIL</c>) and is kept green as-is;
/// dedicated level-3 coverage (link rows actually present/soft-deleted, and the "scoped to ALL
/// details of the head, not just freshly-deleted ones" subtlety) is intentionally left for a
/// follow-up QA pass, per the task instructions for this change.
///
/// Provider choice — SQLite (in-memory), NOT EF Core InMemory:
/// <list type="bullet">
/// <item>
/// InMemory evaluates LINQ largely client-side and does not translate to SQL at all, so it would
/// NOT prove anything about the three-valued-logic claim in the implementation's own comments
/// ("a naive C#-style <c>!= true</c> predicate does not reliably translate to SQL"). It would
/// pass even if the predicate were subtly wrong for a real relational provider.
/// </item>
/// <item>
/// SQLite is a real relational provider: it parses/executes actual SQL, so it DOES validate that
/// <c>(d.ISDELETED == null || d.ISDELETED == false)</c> compiles to
/// <c>"ISDELETED" IS NULL OR NOT ("ISDELETED")</c> (confirmed via <c>ToQueryString()</c> during
/// investigation) and that this predicate actually includes NULL rows against a real SQL engine.
/// </item>
/// <item>
/// Feasibility was verified empirically before writing these tests: calling
/// <c>context.Database.EnsureCreated()</c> against the FULL <see cref="LegacyDbContext"/> model
/// fails on SQLite with <c>NotSupportedException: SQLite does not support sequences</c> (the
/// model declares <c>modelBuilder.HasSequence("VOUCHERHEAD_SEQ")</c> unconditionally for the
/// whole context). That failure is in DDL generation, not model building — the model itself
/// builds fine on SQLite, and <see cref="LegacyDbContext.HasDefaultSchema"/>
/// ("CENTRALACCOUNT") is silently ignored by the SQLite provider (confirmed the generated SQL
/// targets bare <c>"TB_VOUCHERSDETAIL"</c>, no schema prefix). So instead of
/// <c>EnsureCreated()</c>, these tests create ONLY the one physical table they need via a
/// hand-written <c>CREATE TABLE</c> (see <see cref="CreateVoucherDetailTableSql"/>), then use the
/// real, fully-converter-mapped <see cref="LegacyDbContext"/>/<see cref="VoucherHeadRepository"/>
/// against it. Declared SQLite column types are irrelevant here (SQLite has dynamic typing/type
/// affinity, not strict column types), so they don't need to mirror the Oracle
/// <c>NUMBER(1)</c>/<c>CHAR(36)</c> types.
/// </item>
/// <item>
/// What this SQLite provider choice does NOT prove: Oracle-specific behaviour (its actual
/// <c>NUMBER(1)</c> tri-state semantics, <c>sys_guid()</c> defaults, collation
/// <c>USING_NLS_COMP</c>, or anything requiring the real Oracle engine) is out of scope here —
/// per the task constraints, no real Oracle connection is used anywhere in this suite.
/// </item>
/// </list>
/// </summary>
public sealed class VoucherHeadRepositorySoftDeleteDetailLinesTests : IDisposable
{
    private const string CreateVoucherDetailTableSql = """
        CREATE TABLE TB_VOUCHERSDETAIL (
            ID TEXT PRIMARY KEY,
            ACCOUNT_ID TEXT,
            ADDUSERID TEXT,
            CHANGEUSERID TEXT,
            CHECK_ID TEXT,
            CREATEDDATE TEXT,
            CREDITOR TEXT,
            DEBTOR TEXT,
            DESCRIPTION TEXT,
            ETEBAR_ID TEXT,
            ISDELETED INTEGER,
            LOWLEVELCODE_ID TEXT,
            RADIF INTEGER,
            RECEIP_ID TEXT,
            UPDATEDDATE TEXT,
            VAHEDCODE TEXT,
            VOUCHERSHEAD_ID TEXT,
            YEAR TEXT
        );
        """;

    // Mirrors every column of TB_VOUCHERDETAIL_LINK_TAFSILI (see the entity/Fluent Mapping) so
    // the real LegacyDbContext model can round-trip against it. No test in this file seeds rows
    // here yet — it only exists so the level-3 query inside SoftDeleteDetailTreeAsync has a real
    // table to run against instead of erroring with "no such table".
    private const string CreateVoucherDetailLinkTafsiliTableSql = """
        CREATE TABLE TB_VOUCHERDETAIL_LINK_TAFSILI (
            ID TEXT PRIMARY KEY,
            VOUCHERSDETAIL_ID TEXT,
            TAFSILI_ID TEXT,
            LEVEL_ID TEXT,
            CREATEDDATE TEXT,
            UPDATEDDATE TEXT,
            ADDUSERID TEXT,
            CHANGEUSERID TEXT,
            ISDELETED INTEGER,
            VAHEDCODE TEXT,
            YEAR TEXT
        );
        """;

    private readonly SqliteConnection _connection;

    public VoucherHeadRepositorySoftDeleteDetailLinesTests()
    {
        // A single open connection is reused for the whole test's lifetime: SQLite's ":memory:"
        // database only lives as long as the connection that created it stays open, and we
        // deliberately open a fresh DbContext per "phase" (seed / act / assert) below to prove
        // the mutation was actually persisted via SaveChangesAsync — not merely visible on the
        // same context's change tracker.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var setupContext = CreateContext();
        setupContext.Database.ExecuteSqlRaw(CreateVoucherDetailTableSql);
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

    private static TB_VOUCHERSDETAIL Line(
        Guid headId,
        bool? isDeleted,
        string? changeUserId = null,
        DateTime? updatedDate = null) => new()
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = headId,
            ISDELETED = isDeleted,
            CHANGEUSERID = changeUserId,
            UPDATEDDATE = updatedDate,
            DESCRIPTION = "seed line",
        };

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
    public async Task HeadWithMultipleNonDeletedLines_AllLinesSoftDeleted_WithStampedAuditFields()
    {
        var headId = Guid.NewGuid();
        var lines = new[]
        {
            Line(headId, isDeleted: false),
            Line(headId, isDeleted: false),
            Line(headId, isDeleted: false),
        };

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.AddRange(lines);
            await seedContext.SaveChangesAsync();
        }

        var stampedUser = "deleter-42";
        var stampedDate = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(headId, stampedUser, stampedDate);
            await actContext.SaveChangesAsync();

            Assert.Equal(3, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persisted = await assertContext.TB_VOUCHERSDETAILs
                .Where(d => d.VOUCHERSHEAD_ID == headId)
                .ToListAsync();

            Assert.Equal(3, persisted.Count);
            Assert.All(persisted, d =>
            {
                Assert.True(d.ISDELETED);
                Assert.Equal(stampedUser, d.CHANGEUSERID);
                Assert.Equal(stampedDate, d.UPDATEDDATE);
            });
        }
    }

    [Fact]
    public async Task HeadWithZeroDetailLines_ReturnsZero_NoCrash()
    {
        var headId = Guid.NewGuid();

        using var context = CreateContext();
        var repository = new VoucherHeadRepository(context);

        var affected = await repository.SoftDeleteDetailTreeAsync(
            headId, "someone", DateTime.UtcNow);
        await context.SaveChangesAsync();

        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task AlreadySoftDeletedLines_AreNotRetouched_AuditFieldsRemainByteIdentical()
    {
        var headId = Guid.NewGuid();
        var originalUser = "original-editor";
        var originalDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var alreadyDeleted = Line(headId, isDeleted: true, changeUserId: originalUser, updatedDate: originalDate);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.Add(alreadyDeleted);
            await seedContext.SaveChangesAsync();
        }

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(
                headId, "new-editor", new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc));
            await actContext.SaveChangesAsync();

            Assert.Equal(0, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persisted = await assertContext.TB_VOUCHERSDETAILs.SingleAsync(d => d.ID == alreadyDeleted.ID);

            Assert.True(persisted.ISDELETED);
            Assert.Equal(originalUser, persisted.CHANGEUSERID);
            Assert.Equal(originalDate, persisted.UPDATEDDATE);
        }
    }

    [Fact]
    public async Task LinesWithNullIsDeleted_AreTreatedAsNotDeleted_AndAreSoftDeleted()
    {
        // This is the crux of the load-bearing SQL-translation claim in the implementation's
        // comments: `d.ISDELETED == null || d.ISDELETED == false` must include NULL rows under
        // real SQL three-valued logic (confirmed via ToQueryString(): translates to
        // `"ISDELETED" IS NULL OR NOT ("ISDELETED")`).
        var headId = Guid.NewGuid();
        var nullLine = Line(headId, isDeleted: null);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.Add(nullLine);
            await seedContext.SaveChangesAsync();
        }

        var stampedUser = "deleter-null-case";
        var stampedDate = new DateTime(2026, 8, 20, 6, 30, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(headId, stampedUser, stampedDate);
            await actContext.SaveChangesAsync();

            Assert.Equal(1, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persisted = await assertContext.TB_VOUCHERSDETAILs.SingleAsync(d => d.ID == nullLine.ID);

            Assert.True(persisted.ISDELETED);
            Assert.Equal(stampedUser, persisted.CHANGEUSERID);
            Assert.Equal(stampedDate, persisted.UPDATEDDATE);
        }
    }

    [Fact]
    public async Task LinesBelongingToDifferentHead_AreNotTouched_Isolation()
    {
        var targetHeadId = Guid.NewGuid();
        var otherHeadId = Guid.NewGuid();

        var targetLine = Line(targetHeadId, isDeleted: false);
        var otherLine = Line(otherHeadId, isDeleted: false);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.AddRange(targetLine, otherLine);
            await seedContext.SaveChangesAsync();
        }

        var stampedUser = "deleter-isolation";
        var stampedDate = new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(targetHeadId, stampedUser, stampedDate);
            await actContext.SaveChangesAsync();

            Assert.Equal(1, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persistedTarget = await assertContext.TB_VOUCHERSDETAILs.SingleAsync(d => d.ID == targetLine.ID);
            var persistedOther = await assertContext.TB_VOUCHERSDETAILs.SingleAsync(d => d.ID == otherLine.ID);

            Assert.True(persistedTarget.ISDELETED);
            Assert.Equal(stampedUser, persistedTarget.CHANGEUSERID);
            Assert.Equal(stampedDate, persistedTarget.UPDATEDDATE);

            Assert.False(persistedOther.ISDELETED);
            Assert.Null(persistedOther.CHANGEUSERID);
            Assert.Null(persistedOther.UPDATEDDATE);
        }
    }

    [Fact]
    public async Task FullThreeLevelCascade_EveryDetailAndEveryLink_SoftDeleted_WithStampedAuditFields()
    {
        var headId = Guid.NewGuid();
        var detail1 = Line(headId, isDeleted: false);
        var detail2 = Line(headId, isDeleted: false);

        var links = new[]
        {
            Link(detail1.ID, isDeleted: false),
            Link(detail1.ID, isDeleted: false),
            Link(detail2.ID, isDeleted: false),
        };

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.AddRange(detail1, detail2);
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRange(links);
            await seedContext.SaveChangesAsync();
        }

        var stampedUser = "cascade-deleter";
        var stampedDate = new DateTime(2026, 8, 21, 8, 0, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(headId, stampedUser, stampedDate);
            await actContext.SaveChangesAsync();

            Assert.Equal(5, affected); // 2 details + 3 links
        }

        using (var assertContext = CreateContext())
        {
            var persistedDetails = await assertContext.TB_VOUCHERSDETAILs
                .Where(d => d.VOUCHERSHEAD_ID == headId)
                .ToListAsync();
            Assert.Equal(2, persistedDetails.Count);
            Assert.All(persistedDetails, d =>
            {
                Assert.True(d.ISDELETED);
                Assert.Equal(stampedUser, d.CHANGEUSERID);
                Assert.Equal(stampedDate, d.UPDATEDDATE);
            });

            var detailIds = persistedDetails.Select(d => d.ID).ToList();
            var persistedLinks = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs
                .Where(l => detailIds.Contains(l.VOUCHERSDETAIL_ID))
                .ToListAsync();
            Assert.Equal(3, persistedLinks.Count);
            Assert.All(persistedLinks, l =>
            {
                Assert.True(l.ISDELETED);
                Assert.Equal(stampedUser, l.CHANGEUSERID);
                Assert.Equal(stampedDate, l.UPDATEDDATE);
            });
        }
    }

    [Fact]
    public async Task LinksBelongingToDetailOfDifferentHead_AreNotTouched_Isolation()
    {
        var targetHeadId = Guid.NewGuid();
        var otherHeadId = Guid.NewGuid();

        var targetDetail = Line(targetHeadId, isDeleted: false);
        var otherDetail = Line(otherHeadId, isDeleted: false);

        var targetLink = Link(targetDetail.ID, isDeleted: false);
        var otherLink = Link(otherDetail.ID, isDeleted: false);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.AddRange(targetDetail, otherDetail);
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRange(targetLink, otherLink);
            await seedContext.SaveChangesAsync();
        }

        var stampedUser = "isolation-deleter";
        var stampedDate = new DateTime(2026, 8, 21, 9, 0, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(targetHeadId, stampedUser, stampedDate);
            await actContext.SaveChangesAsync();

            Assert.Equal(2, affected); // 1 detail + 1 link
        }

        using (var assertContext = CreateContext())
        {
            var persistedTargetLink = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs
                .SingleAsync(l => l.ID == targetLink.ID);
            var persistedOtherLink = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs
                .SingleAsync(l => l.ID == otherLink.ID);

            Assert.True(persistedTargetLink.ISDELETED);
            Assert.Equal(stampedUser, persistedTargetLink.CHANGEUSERID);
            Assert.Equal(stampedDate, persistedTargetLink.UPDATEDDATE);

            Assert.False(persistedOtherLink.ISDELETED);
            Assert.Null(persistedOtherLink.CHANGEUSERID);
            Assert.Null(persistedOtherLink.UPDATEDDATE);
        }
    }

    [Fact]
    public async Task AlreadySoftDeletedLinkRows_AreNotRetouched_AuditFieldsRemainByteIdentical()
    {
        var headId = Guid.NewGuid();
        var detail = Line(headId, isDeleted: false);

        var originalUser = "original-link-editor";
        var originalDate = new DateTime(2019, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var alreadyDeletedLink = Link(detail.ID, isDeleted: true, changeUserId: originalUser, updatedDate: originalDate);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.Add(detail);
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.Add(alreadyDeletedLink);
            await seedContext.SaveChangesAsync();
        }

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(
                headId, "new-link-editor", new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc));
            await actContext.SaveChangesAsync();

            // The detail row itself still gets soft-deleted (it was active); only the already
            // -deleted link is expected to be skipped.
            Assert.Equal(1, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persistedLink = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs
                .SingleAsync(l => l.ID == alreadyDeletedLink.ID);

            Assert.True(persistedLink.ISDELETED);
            Assert.Equal(originalUser, persistedLink.CHANGEUSERID);
            Assert.Equal(originalDate, persistedLink.UPDATEDDATE);
        }
    }

    [Fact]
    public async Task DetailAlreadySoftDeleted_ButLinksStillActive_LinksAreStillSoftDeleted_DanglingLinkInvariant()
    {
        // The whole reason level 3 is scoped to ALL detail IDs of the head (not just the
        // freshly-deleted ones): a detail row that was already soft-deleted on its own, with its
        // tafsili links left active, must still have those links cascaded when the voucher head
        // is (re)deleted — otherwise an active link would dangle under a deleted voucher.
        var headId = Guid.NewGuid();
        var originalDetailUser = "original-detail-editor";
        var originalDetailDate = new DateTime(2018, 3, 3, 0, 0, 0, DateTimeKind.Utc);
        var alreadyDeletedDetail = Line(
            headId, isDeleted: true, changeUserId: originalDetailUser, updatedDate: originalDetailDate);

        var activeLink1 = Link(alreadyDeletedDetail.ID, isDeleted: false);
        var activeLink2 = Link(alreadyDeletedDetail.ID, isDeleted: false);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.Add(alreadyDeletedDetail);
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRange(activeLink1, activeLink2);
            await seedContext.SaveChangesAsync();
        }

        var stampedUser = "dangling-invariant-deleter";
        var stampedDate = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc);

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(headId, stampedUser, stampedDate);
            await actContext.SaveChangesAsync();

            // 0 details freshly soft-deleted (it already was) + 2 links soft-deleted.
            Assert.Equal(2, affected);
        }

        using (var assertContext = CreateContext())
        {
            var persistedDetail = await assertContext.TB_VOUCHERSDETAILs
                .SingleAsync(d => d.ID == alreadyDeletedDetail.ID);
            Assert.True(persistedDetail.ISDELETED);
            Assert.Equal(originalDetailUser, persistedDetail.CHANGEUSERID);
            Assert.Equal(originalDetailDate, persistedDetail.UPDATEDDATE);

            var persistedLinks = await assertContext.TB_VOUCHERDETAIL_LINK_TAFSILIs
                .Where(l => l.VOUCHERSDETAIL_ID == alreadyDeletedDetail.ID)
                .ToListAsync();
            Assert.Equal(2, persistedLinks.Count);
            Assert.All(persistedLinks, l =>
            {
                Assert.True(l.ISDELETED);
                Assert.Equal(stampedUser, l.CHANGEUSERID);
                Assert.Equal(stampedDate, l.UPDATEDDATE);
            });
        }
    }

    [Fact]
    public async Task HeadWithDetailsButZeroLinkRows_NoCrash()
    {
        var headId = Guid.NewGuid();
        var detail1 = Line(headId, isDeleted: false);
        var detail2 = Line(headId, isDeleted: false);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.AddRange(detail1, detail2);
            await seedContext.SaveChangesAsync();
        }

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(
                headId, "no-link-deleter", DateTime.UtcNow);
            await actContext.SaveChangesAsync();

            Assert.Equal(2, affected); // 2 details + 0 links, no crash
        }
    }

    [Fact]
    public async Task ReturnedCount_EqualsDetailsSoftDeletedPlusLinksSoftDeleted_ForRepresentativeMixedCase()
    {
        // Mix of: a fresh detail with two fresh links, an already-deleted detail with one active
        // link (must still cascade) and one already-deleted link (must not be recounted), and an
        // untouched detail belonging to a different head.
        var headId = Guid.NewGuid();
        var otherHeadId = Guid.NewGuid();

        var freshDetail = Line(headId, isDeleted: false);
        var freshDetailLink1 = Link(freshDetail.ID, isDeleted: false);
        var freshDetailLink2 = Link(freshDetail.ID, isDeleted: false);

        var alreadyDeletedDetail = Line(headId, isDeleted: true, changeUserId: "old", updatedDate: new DateTime(2018, 1, 1));
        var alreadyDeletedDetailActiveLink = Link(alreadyDeletedDetail.ID, isDeleted: false);
        var alreadyDeletedDetailDeletedLink = Link(
            alreadyDeletedDetail.ID, isDeleted: true, changeUserId: "old", updatedDate: new DateTime(2018, 1, 1));

        var otherHeadDetail = Line(otherHeadId, isDeleted: false);
        var otherHeadLink = Link(otherHeadDetail.ID, isDeleted: false);

        using (var seedContext = CreateContext())
        {
            seedContext.TB_VOUCHERSDETAILs.AddRange(freshDetail, alreadyDeletedDetail, otherHeadDetail);
            seedContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRange(
                freshDetailLink1,
                freshDetailLink2,
                alreadyDeletedDetailActiveLink,
                alreadyDeletedDetailDeletedLink,
                otherHeadLink);
            await seedContext.SaveChangesAsync();
        }

        using (var actContext = CreateContext())
        {
            var repository = new VoucherHeadRepository(actContext);

            var affected = await repository.SoftDeleteDetailTreeAsync(
                headId, "mixed-case-deleter", new DateTime(2026, 8, 21, 11, 0, 0, DateTimeKind.Utc));
            await actContext.SaveChangesAsync();

            // Details freshly soft-deleted: 1 (freshDetail only; alreadyDeletedDetail was already deleted).
            // Links freshly soft-deleted: 3 (freshDetailLink1, freshDetailLink2, alreadyDeletedDetailActiveLink).
            // alreadyDeletedDetailDeletedLink and otherHeadLink must NOT be counted.
            Assert.Equal(4, affected);
        }
    }
}
