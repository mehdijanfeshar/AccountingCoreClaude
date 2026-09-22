using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.ReverseVoucher;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.ReverseVoucher;

/// <summary>
/// معکوس سند — the mirrored voucher.
/// </summary>
public sealed class ReverseVoucherCommandHandlerTests
{
    private const string Vahed = "0001";

    private sealed class Harness
    {
        public required ReverseVoucherCommandHandler Handler { get; init; }
        public required Mock<IUnitOfWork> UnitOfWork { get; init; }
        public List<TB_VOUCHERSHEAD> AddedHeads { get; } = [];
        public List<TB_VOUCHERSDETAIL> AddedLines { get; } = [];
        public List<TB_VOUCHERDETAIL_LINK_TAFSILI> AddedLinks { get; } = [];
    }

    private static TB_VOUCHERSHEAD SourceHead(Guid id, DocLife? docLife = DocLife.Accepted, bool? isDeleted = false) => new()
    {
        ID = id,
        DOC_NUM = "000042",
        DATE_DOC = "14030110",
        DOCLIFE = docLife,
        HEAD_DESC = "سند اصلی",
        APENDIX = "پیوست",
        SYSTEM_TYPE = Guid.NewGuid(),
        FLAG_STATE = 3m,
        ISAUTOMATIC = true,
        VAHEDCODE = Vahed,
        YEAR = "1403",
        ISDELETED = isDeleted,
    };

    private static TB_VOUCHERSDETAIL Line(
        decimal debtor,
        decimal creditor,
        Guid? receiptId = null,
        Guid? checkId = null) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNT_ID = Guid.NewGuid(),
        DESCRIPTION = "شرح ردیف",
        RADIF = 1,
        DEBTOR = debtor,
        CREDITOR = creditor,
        RECEIP_ID = receiptId,
        CHECK_ID = checkId,
        VAHEDCODE = Vahed,
        YEAR = "1403",
        ISDELETED = false,
    };

    private static Harness Build(
        TB_VOUCHERSHEAD? source,
        List<TB_VOUCHERSDETAIL>? lines = null,
        List<TB_VOUCHERDETAIL_LINK_TAFSILI>? tafsiliLinks = null)
    {
        var headRepository = new Mock<IVoucherHeadRepository>();
        var detailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("reverser");

        headRepository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        headRepository
            .Setup(r => r.GetNextDocNumAsync(Vahed, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("000099");

        detailRepository
            .Setup(r => r.GetActiveByHeadAsync(It.IsAny<Guid>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lines ?? []);

        detailRepository
            .Setup(r => r.GetActiveTafsiliLinksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tafsiliLinks ?? []);

        var harness = new Harness
        {
            Handler = new ReverseVoucherCommandHandler(
                headRepository.Object,
                detailRepository.Object,
                unitOfWork.Object,
                currentUser.Object),
            UnitOfWork = unitOfWork,
        };

        headRepository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((h, _) => harness.AddedHeads.Add(h));

        detailRepository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((d, _) => harness.AddedLines.Add(d));

        detailRepository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_VOUCHERDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERDETAIL_LINK_TAFSILI, CancellationToken>((l, _) => harness.AddedLinks.Add(l));

        return harness;
    }

    private static ReverseVoucherCommand Command(Guid id) => new(id) { VahedCode = Vahed };

    [Fact]
    public async Task Swaps_debtor_and_creditor_on_every_line()
    {
        var source = SourceHead(Guid.NewGuid());
        var harness = Build(source, [Line(debtor: 500, creditor: 0), Line(debtor: 0, creditor: 500)]);

        await harness.Handler.Handle(Command(source.ID), CancellationToken.None);

        Assert.Equal(2, harness.AddedLines.Count);
        Assert.Equal(0, harness.AddedLines[0].DEBTOR);
        Assert.Equal(500, harness.AddedLines[0].CREDITOR);
        Assert.Equal(500, harness.AddedLines[1].DEBTOR);
        Assert.Equal(0, harness.AddedLines[1].CREDITOR);
    }

    [Fact]
    public async Task Leaves_the_source_voucher_untouched()
    {
        var source = SourceHead(Guid.NewGuid());
        var harness = Build(source, [Line(100, 0)]);

        await harness.Handler.Handle(Command(source.ID), CancellationToken.None);

        Assert.Equal("000042", source.DOC_NUM);
        Assert.Equal(DocLife.Accepted, source.DOCLIFE);
        Assert.Equal("سند اصلی", source.HEAD_DESC);
    }

    [Fact]
    public async Task Reversing_a_finalized_voucher_is_allowed()
    {
        // The whole purpose of the operation: undoing a تأیید دائم voucher without deleting it.
        // Phase 38's editability lock deliberately does not apply, because the source is not
        // modified.
        var source = SourceHead(Guid.NewGuid(), DocLife.Accepted);
        var harness = Build(source, [Line(100, 0)]);

        await harness.Handler.Handle(Command(source.ID), CancellationToken.None);

        Assert.Single(harness.AddedHeads);
    }

    [Fact]
    public async Task The_reversal_is_always_a_draft_and_points_back_at_its_source()
    {
        var source = SourceHead(Guid.NewGuid(), DocLife.Accepted);
        var harness = Build(source, [Line(100, 0)]);

        await harness.Handler.Handle(Command(source.ID), CancellationToken.None);

        var head = Assert.Single(harness.AddedHeads);
        Assert.Equal(DocLife.Draft, head.DOCLIFE);
        Assert.Equal(source.ID, head.PARENTHEAD_ID);
        Assert.Equal("000099", head.DOC_NUM);
        Assert.Contains("000042", head.HEAD_DESC);
    }

    [Fact]
    public async Task The_reversal_is_never_born_soft_deleted()
    {
        // The reference copies IsDeleted from the source, so reversing a soft-deleted voucher
        // produced a reversal that was already deleted.
        var source = SourceHead(Guid.NewGuid(), isDeleted: false);
        var harness = Build(source, [Line(100, 0)]);

        await harness.Handler.Handle(Command(source.ID), CancellationToken.None);

        Assert.False(Assert.Single(harness.AddedHeads).ISDELETED);
    }

    [Fact]
    public async Task Lines_tied_to_a_receipt_or_cheque_are_not_carried_over()
    {
        var source = SourceHead(Guid.NewGuid());
        var harness = Build(source,
        [
            Line(100, 0),
            Line(0, 100, receiptId: Guid.NewGuid()),
            Line(50, 0, checkId: Guid.NewGuid()),
        ]);

        await harness.Handler.Handle(Command(source.ID), CancellationToken.None);

        // Only the plain line survives; a payment instrument must not be duplicated onto a second
        // document.
        Assert.Single(harness.AddedLines);
        Assert.Null(harness.AddedLines[0].RECEIP_ID);
        Assert.Null(harness.AddedLines[0].CHECK_ID);
    }

    [Fact]
    public async Task Tafsili_assignments_carry_over_unchanged()
    {
        var source = SourceHead(Guid.NewGuid());
        var tafsiliId = Guid.NewGuid();
        var levelId = Guid.NewGuid();

        var harness = Build(
            source,
            [Line(100, 0)],
            [new TB_VOUCHERDETAIL_LINK_TAFSILI { ID = Guid.NewGuid(), TAFSILI_ID = tafsiliId, LEVEL_ID = levelId }]);

        await harness.Handler.Handle(Command(source.ID), CancellationToken.None);

        var link = Assert.Single(harness.AddedLinks);
        Assert.Equal(tafsiliId, link.TAFSILI_ID);
        Assert.Equal(levelId, link.LEVEL_ID);
        Assert.Equal(harness.AddedLines[0].ID, link.VOUCHERSDETAIL_ID);
    }

    [Fact]
    public async Task An_unknown_voucher_is_a_not_found()
    {
        var harness = Build(source: null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => harness.Handler.Handle(Command(Guid.NewGuid()), CancellationToken.None));

        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_soft_deleted_voucher_cannot_be_reversed()
    {
        var source = SourceHead(Guid.NewGuid(), isDeleted: true);
        var harness = Build(source);

        await Assert.ThrowsAsync<NotFoundException>(
            () => harness.Handler.Handle(Command(source.ID), CancellationToken.None));
    }

    [Fact]
    public async Task Everything_is_written_in_one_save()
    {
        var source = SourceHead(Guid.NewGuid());
        var harness = Build(source, [Line(100, 0), Line(0, 100)]);

        await harness.Handler.Handle(Command(source.ID), CancellationToken.None);

        // Head, lines and تفصیلی links land together or not at all.
        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
