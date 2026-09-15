using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.UpdateVoucherDetail;

/// <summary>
/// Covers the phase-11 تفصیلی reconcile in <see cref="UpdateVoucherDetailCommandHandler"/>:
/// <c>TafsiliLinks</c> is a FULL REPLACEMENT of the line's assignments (add missing, soft-delete
/// dropped, leave shared ones untouched), while <see langword="null"/> means "don't touch them at
/// all".
/// </summary>
public sealed class UpdateVoucherDetailCommandHandlerTafsiliLinksTests
{
    private static readonly DateTime OriginalStamp = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static UpdateVoucherDetailCommand CommandWithLinks(
        Guid id,
        IReadOnlyList<VoucherDetailTafsiliLinkInput>? tafsiliLinks,
        string vahedCode = "0001",
        string? year = "1405") => new(
        Id: id,
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ویرایش شد",
        Radif: 2,
        Debtor: 500m,
        Creditor: null,
        Year: year,
        TafsiliLinks: tafsiliLinks)
    {
        VahedCode = vahedCode,
    };

    private static TB_VOUCHERSDETAIL ExistingDetail(Guid id) => new()
    {
        ID = id,
        VOUCHERSHEAD_ID = Guid.NewGuid(),
        VAHEDCODE = "0001",
        YEAR = "1405",
        ADDUSERID = "creator1",
        CREATEDDATE = OriginalStamp,
        ISDELETED = false,
    };

    private static TB_VOUCHERDETAIL_LINK_TAFSILI ExistingLink(Guid detailId, Guid tafsiliId, Guid levelId) => new()
    {
        ID = Guid.NewGuid(),
        VOUCHERSDETAIL_ID = detailId,
        TAFSILI_ID = tafsiliId,
        LEVEL_ID = levelId,
        VAHEDCODE = "0001",
        YEAR = "1405",
        ADDUSERID = "creator1",
        CREATEDDATE = OriginalStamp,
        ISDELETED = false,
    };

    private sealed record Harness(
        UpdateVoucherDetailCommandHandler Handler,
        Mock<IVoucherDetailRepository> DetailRepository,
        Mock<IUnitOfWork> UnitOfWork,
        TB_VOUCHERSDETAIL Entity,
        List<TB_VOUCHERDETAIL_LINK_TAFSILI> StagedNewLinks,
        List<string> CallOrder);

    private static Harness CreateHarness(
        Guid detailId,
        IReadOnlyList<TB_VOUCHERDETAIL_LINK_TAFSILI> existingLinks,
        string userId = "user1")
    {
        var entity = ExistingDetail(detailId);
        var stagedNewLinks = new List<TB_VOUCHERDETAIL_LINK_TAFSILI>();
        var callOrder = new List<string>();

        var detailRepository = new Mock<IVoucherDetailRepository>();
        detailRepository
            .Setup(r => r.GetForUpdateAsync(detailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        detailRepository
            .Setup(r => r.GetActiveTafsiliLinksAsync(detailId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("GetActiveTafsiliLinksAsync"))
            .ReturnsAsync(existingLinks);
        detailRepository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_VOUCHERDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERDETAIL_LINK_TAFSILI, CancellationToken>((link, _) =>
            {
                stagedNewLinks.Add(link);
                callOrder.Add("AddTafsiliLinkAsync");
            })
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);

        var handler = new UpdateVoucherDetailCommandHandler(
            detailRepository.Object, unitOfWork.Object, currentUser.Object);

        return new Harness(handler, detailRepository, unitOfWork, entity, stagedNewLinks, callOrder);
    }

    [Fact]
    public async Task Handle_NullTafsiliLinks_DoesNotEvenQueryTheExistingLinks()
    {
        var detailId = Guid.NewGuid();
        var harness = CreateHarness(detailId, Array.Empty<TB_VOUCHERDETAIL_LINK_TAFSILI>());

        await harness.Handler.Handle(CommandWithLinks(detailId, null), CancellationToken.None);

        // "null means leave them alone" must be a genuine no-op, not a reconcile against an empty
        // set — otherwise every legacy caller would silently wipe تفصیلی data on an unrelated edit.
        harness.DetailRepository.Verify(
            r => r.GetActiveTafsiliLinksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.DetailRepository.Verify(
            r => r.AddTafsiliLinkAsync(It.IsAny<TB_VOUCHERDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyTafsiliLinks_SoftDeletesEveryExistingLink()
    {
        var detailId = Guid.NewGuid();
        var first = ExistingLink(detailId, Guid.NewGuid(), Guid.NewGuid());
        var second = ExistingLink(detailId, Guid.NewGuid(), Guid.NewGuid());
        var harness = CreateHarness(detailId, new[] { first, second }, userId: "editor9");

        await harness.Handler.Handle(
            CommandWithLinks(detailId, Array.Empty<VoucherDetailTafsiliLinkInput>()),
            CancellationToken.None);

        // An EMPTY list is the explicit "this line should have no تفصیلی" instruction — the one
        // way a caller can clear them. Distinct from null, which is the no-op above.
        Assert.True(first.ISDELETED);
        Assert.True(second.ISDELETED);
        Assert.Equal("editor9", first.CHANGEUSERID);
        Assert.Equal(harness.Entity.UPDATEDDATE, first.UPDATEDDATE);
        Assert.Empty(harness.StagedNewLinks);
    }

    [Fact]
    public async Task Handle_ReplacementSet_AddsMissing_SoftDeletesDropped_AndLeavesSharedUntouched()
    {
        var detailId = Guid.NewGuid();
        var keptTafsili = Guid.NewGuid();
        var keptLevel = Guid.NewGuid();
        var kept = ExistingLink(detailId, keptTafsili, keptLevel);
        var dropped = ExistingLink(detailId, Guid.NewGuid(), Guid.NewGuid());
        var harness = CreateHarness(detailId, new[] { kept, dropped });

        var addedTafsili = Guid.NewGuid();
        var addedLevel = Guid.NewGuid();

        await harness.Handler.Handle(
            CommandWithLinks(detailId, new[]
            {
                new VoucherDetailTafsiliLinkInput(keptTafsili, keptLevel),
                new VoucherDetailTafsiliLinkInput(addedTafsili, addedLevel),
            }),
            CancellationToken.None);

        // Dropped → soft-deleted.
        Assert.True(dropped.ISDELETED);

        // Shared → byte-identical. Re-stamping it would destroy the "who first assigned this
        // تفصیلی" audit signal on every unrelated edit, so this assertion is load-bearing.
        Assert.False(kept.ISDELETED);
        Assert.Null(kept.CHANGEUSERID);
        Assert.Null(kept.UPDATEDDATE);
        Assert.Equal(OriginalStamp, kept.CREATEDDATE);
        Assert.Equal("creator1", kept.ADDUSERID);

        // Missing → inserted exactly once, and NOT re-inserted for the shared pair.
        var added = Assert.Single(harness.StagedNewLinks);
        Assert.Equal(addedTafsili, added.TAFSILI_ID);
        Assert.Equal(addedLevel, added.LEVEL_ID);
        Assert.Equal(detailId, added.VOUCHERSDETAIL_ID);
        Assert.False(added.ISDELETED);
    }

    [Fact]
    public async Task Handle_NewLink_DerivesVahedCodeAndYearFromTheUpdatedLineValues()
    {
        var detailId = Guid.NewGuid();
        var harness = CreateHarness(detailId, Array.Empty<TB_VOUCHERDETAIL_LINK_TAFSILI>());

        await harness.Handler.Handle(
            CommandWithLinks(
                detailId,
                new[] { new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()) },
                vahedCode: "0009",
                year: "1406"),
            CancellationToken.None);

        var added = Assert.Single(harness.StagedNewLinks);

        // The line's own VAHEDCODE/YEAR were overwritten by this same command; the new link must
        // agree with the NEW values, not the pre-update ones.
        Assert.Equal("0009", added.VAHEDCODE);
        Assert.Equal("1406", added.YEAR);
        Assert.Equal(harness.Entity.VAHEDCODE, added.VAHEDCODE);
        Assert.Equal(harness.Entity.YEAR, added.YEAR);
    }

    [Fact]
    public async Task Handle_NewLink_StampsAuditFromCurrentUser_SharingTheLinesUpdateTimestamp()
    {
        var detailId = Guid.NewGuid();
        var harness = CreateHarness(detailId, Array.Empty<TB_VOUCHERDETAIL_LINK_TAFSILI>(), userId: "editor9");

        await harness.Handler.Handle(
            CommandWithLinks(detailId, new[] { new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()) }),
            CancellationToken.None);

        var added = Assert.Single(harness.StagedNewLinks);

        Assert.Equal("editor9", added.ADDUSERID);
        Assert.Equal(harness.Entity.UPDATEDDATE, added.CREATEDDATE);
        Assert.Null(added.CHANGEUSERID);
    }

    [Fact]
    public async Task Handle_Reconcile_HappensBeforeTheSingleSaveChanges()
    {
        var detailId = Guid.NewGuid();
        var dropped = ExistingLink(detailId, Guid.NewGuid(), Guid.NewGuid());
        var harness = CreateHarness(detailId, new[] { dropped });

        await harness.Handler.Handle(
            CommandWithLinks(detailId, new[] { new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()) }),
            CancellationToken.None);

        // The line's fields, the soft-deletes and the inserts must all land in one transaction.
        Assert.Equal(
            new[] { "GetActiveTafsiliLinksAsync", "AddTafsiliLinkAsync", "SaveChangesAsync" },
            harness.CallOrder);
        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicatePairsInReplacementSet_CollapseToASingleNewLink()
    {
        var detailId = Guid.NewGuid();
        var harness = CreateHarness(detailId, Array.Empty<TB_VOUCHERDETAIL_LINK_TAFSILI>());
        var tafsiliId = Guid.NewGuid();
        var levelId = Guid.NewGuid();

        await harness.Handler.Handle(
            CommandWithLinks(detailId, new[]
            {
                new VoucherDetailTafsiliLinkInput(tafsiliId, levelId),
                new VoucherDetailTafsiliLinkInput(tafsiliId, levelId),
            }),
            CancellationToken.None);

        Assert.Single(harness.StagedNewLinks);
    }

    /// <summary>
    /// Re-requesting a pair whose only row is already soft-deleted must insert a NEW row rather
    /// than resurrect the old one — the soft-deleted row keeps its original audit trail. The
    /// repository only ever returns ACTIVE links, so this is really a guard that the handler does
    /// not try to be clever about rows it was never handed.
    /// </summary>
    [Fact]
    public async Task Handle_PairThatExistsOnlyAsASoftDeletedRow_InsertsAFreshLink()
    {
        var detailId = Guid.NewGuid();
        var tafsiliId = Guid.NewGuid();
        var levelId = Guid.NewGuid();

        // GetActiveTafsiliLinksAsync filters ISDELETED == false, so a previously deleted row is
        // simply absent from what the handler sees.
        var harness = CreateHarness(detailId, Array.Empty<TB_VOUCHERDETAIL_LINK_TAFSILI>());

        await harness.Handler.Handle(
            CommandWithLinks(detailId, new[] { new VoucherDetailTafsiliLinkInput(tafsiliId, levelId) }),
            CancellationToken.None);

        var added = Assert.Single(harness.StagedNewLinks);
        Assert.Equal(tafsiliId, added.TAFSILI_ID);
        Assert.Equal(levelId, added.LEVEL_ID);
        Assert.False(added.ISDELETED);
    }

    /// <summary>
    /// Regression test for a code-review finding: a link kept across an update (present in both
    /// the existing set and the requested set) must have its VAHEDCODE/YEAR re-synced to the
    /// line's new values when the line itself changed unit/year in the same call — otherwise the
    /// link silently disagrees with its own parent, violating the invariant documented on
    /// <see cref="VoucherDetailTafsiliLinkInput"/>. Audit columns must still stay untouched.
    /// </summary>
    [Fact]
    public async Task Handle_KeptLink_ResyncsVahedCodeAndYear_ButLeavesAuditColumnsUntouched()
    {
        var detailId = Guid.NewGuid();
        var tafsiliId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var kept = ExistingLink(detailId, tafsiliId, levelId);
        var harness = CreateHarness(detailId, new[] { kept });

        await harness.Handler.Handle(
            CommandWithLinks(
                detailId,
                new[] { new VoucherDetailTafsiliLinkInput(tafsiliId, levelId) },
                vahedCode: "0009",
                year: "1406"),
            CancellationToken.None);

        Assert.Equal("0009", kept.VAHEDCODE);
        Assert.Equal("1406", kept.YEAR);
        Assert.False(kept.ISDELETED);
        Assert.Null(kept.CHANGEUSERID);
        Assert.Null(kept.UPDATEDDATE);
        Assert.Equal(OriginalStamp, kept.CREATEDDATE);
        Assert.Equal("creator1", kept.ADDUSERID);
        Assert.Empty(harness.StagedNewLinks);
    }

    [Fact]
    public async Task Handle_IdenticalReplacementSet_StagesNoInsertsAndNoDeletes()
    {
        var detailId = Guid.NewGuid();
        var tafsiliId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var existing = ExistingLink(detailId, tafsiliId, levelId);
        var harness = CreateHarness(detailId, new[] { existing });

        await harness.Handler.Handle(
            CommandWithLinks(detailId, new[] { new VoucherDetailTafsiliLinkInput(tafsiliId, levelId) }),
            CancellationToken.None);

        Assert.Empty(harness.StagedNewLinks);
        Assert.False(existing.ISDELETED);
        Assert.Null(existing.CHANGEUSERID);
        Assert.Null(existing.UPDATEDDATE);
    }
}
