using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherDetail;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.CreateVoucherDetail;

/// <summary>
/// Covers the phase-11 addition to <see cref="CreateVoucherDetailCommandHandler"/>: staging
/// <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI"/> rows alongside the detail line, in the same single
/// <see cref="IUnitOfWork.SaveChangesAsync"/>. This closed the phase-10 open item that the table
/// had a cascade-delete path but no write path at all.
///
/// Kept in a separate file from <c>CreateVoucherDetailCommandHandlerTests</c> for the same reason
/// <c>CreateVoucherHeadCommandHandlerInitialDetailsTests</c> is separate from its own base tests:
/// the pre-existing line-only behaviour must stay independently readable and independently
/// verifiable from the nested-collection behaviour layered on top of it.
/// </summary>
public sealed class CreateVoucherDetailCommandHandlerTafsiliLinksTests
{
    private static CreateVoucherDetailCommand CommandWithLinks(
        Guid voucherHeadId,
        IReadOnlyList<VoucherDetailTafsiliLinkInput>? tafsiliLinks) => new(
        VoucherHeadId: voucherHeadId,
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف با تفصیلی",
        Radif: 1,
        Debtor: 1000m,
        Creditor: null,
        Year: "1405",
        TafsiliLinks: tafsiliLinks)
    {
        VahedCode = "0001",
    };

    private static TB_VOUCHERSHEAD ExistingHead(Guid id) => new()
    {
        ID = id,
        DOC_NUM = "000001",
        DATE_DOC = "14050101",
        VAHEDCODE = "0001",
        YEAR = "1405",
        ADDUSERID = "creator1",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = false,
    };

    private sealed record Harness(
        CreateVoucherDetailCommandHandler Handler,
        Mock<IVoucherDetailRepository> DetailRepository,
        Mock<IUnitOfWork> UnitOfWork,
        List<TB_VOUCHERSDETAIL> StagedDetails,
        List<TB_VOUCHERDETAIL_LINK_TAFSILI> StagedLinks,
        List<string> CallOrder);

    private static Harness CreateHarness(Guid headId, string userId = "user1")
    {
        var headRepository = new Mock<IVoucherHeadRepository>();
        headRepository
            .Setup(r => r.GetForUpdateAsync(headId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingHead(headId));

        var stagedDetails = new List<TB_VOUCHERSDETAIL>();
        var stagedLinks = new List<TB_VOUCHERDETAIL_LINK_TAFSILI>();
        var callOrder = new List<string>();

        var detailRepository = new Mock<IVoucherDetailRepository>();
        detailRepository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((entity, _) =>
            {
                stagedDetails.Add(entity);
                callOrder.Add("AddAsync");
            })
            .Returns(Task.CompletedTask);
        detailRepository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_VOUCHERDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERDETAIL_LINK_TAFSILI, CancellationToken>((entity, _) =>
            {
                stagedLinks.Add(entity);
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

        var handler = new CreateVoucherDetailCommandHandler(
            headRepository.Object, detailRepository.Object, unitOfWork.Object, currentUser.Object);

        return new Harness(handler, detailRepository, unitOfWork, stagedDetails, stagedLinks, callOrder);
    }

    [Fact]
    public async Task Handle_WithMultipleTafsiliLinks_StagesOneLinkPerInput_WiredToTheNewDetailLine()
    {
        var headId = Guid.NewGuid();
        var harness = CreateHarness(headId);
        var firstTafsili = Guid.NewGuid();
        var secondTafsili = Guid.NewGuid();
        var firstLevel = Guid.NewGuid();
        var secondLevel = Guid.NewGuid();

        var detailId = await harness.Handler.Handle(
            CommandWithLinks(headId, new[]
            {
                new VoucherDetailTafsiliLinkInput(firstTafsili, firstLevel),
                new VoucherDetailTafsiliLinkInput(secondTafsili, secondLevel),
            }),
            CancellationToken.None);

        Assert.Equal(2, harness.StagedLinks.Count);
        Assert.All(harness.StagedLinks, link =>
        {
            Assert.Equal(detailId, link.VOUCHERSDETAIL_ID);
            Assert.NotEqual(Guid.Empty, link.ID);
            Assert.False(link.ISDELETED);
        });
        Assert.Contains(harness.StagedLinks, l => l.TAFSILI_ID == firstTafsili && l.LEVEL_ID == firstLevel);
        Assert.Contains(harness.StagedLinks, l => l.TAFSILI_ID == secondTafsili && l.LEVEL_ID == secondLevel);

        // Each link row must get its own identity — a shared/duplicated ID would collide on
        // PK_VOUCHERDETAILLINKTAF.
        Assert.Equal(2, harness.StagedLinks.Select(l => l.ID).Distinct().Count());
    }

    [Fact]
    public async Task Handle_WithTafsiliLinks_DerivesVahedCodeAndYearFromTheLine_NotFromLinkInput()
    {
        var headId = Guid.NewGuid();
        var harness = CreateHarness(headId);

        await harness.Handler.Handle(
            CommandWithLinks(headId, new[] { new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()) }),
            CancellationToken.None);

        var line = Assert.Single(harness.StagedDetails);
        var link = Assert.Single(harness.StagedLinks);

        // VoucherDetailTafsiliLinkInput carries no VahedCode/Year at all — these can only have
        // come from the line, which is what structurally prevents a link disagreeing with its
        // parent about unit/year.
        Assert.Equal(line.VAHEDCODE, link.VAHEDCODE);
        Assert.Equal(line.YEAR, link.YEAR);
    }

    [Fact]
    public async Task Handle_WithTafsiliLinks_StampsAuditFromCurrentUser_AndSharesTheLinesCreatedDate()
    {
        var headId = Guid.NewGuid();
        var harness = CreateHarness(headId, userId: "auditor7");

        await harness.Handler.Handle(
            CommandWithLinks(headId, new[]
            {
                new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()),
                new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()),
            }),
            CancellationToken.None);

        var line = Assert.Single(harness.StagedDetails);

        Assert.All(harness.StagedLinks, link =>
        {
            // ADDUSERID must come from ICurrentUser, never from the request — the command has no
            // field for it, so this also proves it cannot be forged.
            Assert.Equal("auditor7", link.ADDUSERID);
            // One clock read shared by the line and every link, rather than drifting per row.
            Assert.Equal(line.CREATEDDATE, link.CREATEDDATE);
            Assert.Null(link.CHANGEUSERID);
            Assert.Null(link.UPDATEDDATE);
        });
    }

    [Fact]
    public async Task Handle_WithTafsiliLinks_StagesEverythingBeforeTheSingleSaveChanges()
    {
        var headId = Guid.NewGuid();
        var harness = CreateHarness(headId);

        await harness.Handler.Handle(
            CommandWithLinks(headId, new[]
            {
                new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()),
                new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()),
            }),
            CancellationToken.None);

        // The line and its links must be one atomic write: a single SaveChangesAsync, last.
        Assert.Equal(
            new[] { "AddAsync", "AddTafsiliLinkAsync", "AddTafsiliLinkAsync", "SaveChangesAsync" },
            harness.CallOrder);
        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateTafsiliPairsInRequest_CollapsesThemToASingleLink()
    {
        var headId = Guid.NewGuid();
        var harness = CreateHarness(headId);
        var tafsiliId = Guid.NewGuid();
        var levelId = Guid.NewGuid();

        await harness.Handler.Handle(
            CommandWithLinks(headId, new[]
            {
                new VoucherDetailTafsiliLinkInput(tafsiliId, levelId),
                new VoucherDetailTafsiliLinkInput(tafsiliId, levelId),
            }),
            CancellationToken.None);

        // TB_VOUCHERDETAIL_LINK_TAFSILI has no UNIQUE constraint, so the database would happily
        // accept both rows — the handler must be the one to collapse them.
        var link = Assert.Single(harness.StagedLinks);
        Assert.Equal(tafsiliId, link.TAFSILI_ID);
        Assert.Equal(levelId, link.LEVEL_ID);
    }

    /// <summary>
    /// Same tafsili at two different levels is NOT a duplicate — the identity of an assignment is
    /// the (TAFSILI_ID, LEVEL_ID) pair, not the tafsili alone. Guards against a dedupe keyed on
    /// the wrong field silently dropping a legitimate assignment.
    /// </summary>
    [Fact]
    public async Task Handle_SameTafsiliAtDifferentLevels_StagesBothLinks()
    {
        var headId = Guid.NewGuid();
        var harness = CreateHarness(headId);
        var tafsiliId = Guid.NewGuid();

        await harness.Handler.Handle(
            CommandWithLinks(headId, new[]
            {
                new VoucherDetailTafsiliLinkInput(tafsiliId, Guid.NewGuid()),
                new VoucherDetailTafsiliLinkInput(tafsiliId, Guid.NewGuid()),
            }),
            CancellationToken.None);

        Assert.Equal(2, harness.StagedLinks.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WithoutTafsiliLinks_BehavesExactlyAsBefore(bool useEmptyListInsteadOfNull)
    {
        var headId = Guid.NewGuid();
        var harness = CreateHarness(headId);

        var result = await harness.Handler.Handle(
            CommandWithLinks(headId, useEmptyListInsteadOfNull ? Array.Empty<VoucherDetailTafsiliLinkInput>() : null),
            CancellationToken.None);

        // Non-breaking guarantee: an absent or empty list must not stage a single link, and must
        // not disturb the pre-existing line-only write path in any way.
        Assert.Empty(harness.StagedLinks);
        Assert.NotEqual(Guid.Empty, result);
        Assert.Single(harness.StagedDetails);
        harness.DetailRepository.Verify(
            r => r.AddTafsiliLinkAsync(It.IsAny<TB_VOUCHERDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()),
            Times.Never);
        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// The critical IDOR-closure proof for this line's own composite-create path (2026-09):
    /// running the command through <see cref="VahedScopeBehavior{TRequest,TResponse}"/> — exactly
    /// as the real MediatR pipeline does — must overwrite the line's <c>VAHEDCODE</c> AND, because
    /// every تفصیلی link derives its <c>VAHEDCODE</c> from the line's own (post-overwrite) value,
    /// every تفصیلی link created together with it. This is the end-to-end guarantee that matters
    /// for IDOR risk #1 (CLAUDE.md); <see cref="Handle_WithTafsiliLinks_DerivesVahedCodeAndYearFromTheLine_NotFromLinkInput"/>
    /// above only proves the handler-level derivation is faithful to <c>request.VahedCode</c>.
    /// </summary>
    [Fact]
    public async Task Handle_ThroughVahedScopeBehavior_ForgedVahedCode_IsDiscarded_LineAndEveryTafsiliLinkGetServerValue()
    {
        var headId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("user1");
        currentUser.SetupGet(u => u.VahedCode).Returns("0009");

        var headRepository = new Mock<IVoucherHeadRepository>();
        headRepository
            .Setup(r => r.GetForUpdateAsync(headId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingHead(headId));

        var stagedDetails = new List<TB_VOUCHERSDETAIL>();
        var stagedLinks = new List<TB_VOUCHERDETAIL_LINK_TAFSILI>();
        var detailRepository = new Mock<IVoucherDetailRepository>();
        detailRepository
            .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((entity, _) => stagedDetails.Add(entity))
            .Returns(Task.CompletedTask);
        detailRepository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_VOUCHERDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERDETAIL_LINK_TAFSILI, CancellationToken>((entity, _) => stagedLinks.Add(entity))
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateVoucherDetailCommandHandler(
            headRepository.Object, detailRepository.Object, unitOfWork.Object, currentUser.Object);
        var behavior = new VahedScopeBehavior<CreateVoucherDetailCommand, Guid>(currentUser.Object);
        var forgedCommand = CommandWithLinks(headId, new[]
        {
            new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()),
            new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()),
        }) with
        {
            VahedCode = "9999",
        };

        await behavior.Handle(forgedCommand, ct => handler.Handle(forgedCommand, ct), CancellationToken.None);

        Assert.Equal("0009", forgedCommand.VahedCode);
        var line = Assert.Single(stagedDetails);
        Assert.Equal("0009", line.VAHEDCODE);
        Assert.Equal(2, stagedLinks.Count);
        Assert.All(stagedLinks, l => Assert.Equal("0009", l.VAHEDCODE));
    }
}
