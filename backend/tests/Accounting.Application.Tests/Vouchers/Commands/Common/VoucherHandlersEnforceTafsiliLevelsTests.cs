using Accounting.Domain.ValueObjects;
using Accounting.Application.Tests.TestSupport;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherDetail;
using Accounting.Application.Vouchers.Commands.CreateVoucherHead;
using Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.Common;

/// <summary>
/// Proves each voucher write path actually enforces «تفصیلی الزامی», and enforces it <b>before</b>
/// staging anything.
///
/// <b>This is the test that makes <see cref="TafsiliLevelGuards.Permissive"/> safe to use
/// elsewhere.</b> Every other voucher handler test runs with a guard that accepts everything, so
/// on its own none of them would notice a handler that quietly stopped calling it. These do — and
/// they check the ordering too, because a guard invoked after the rows are staged would still
/// throw, still look correct in a test that only asserts the exception, and still leave the work
/// partially done if anything between the two ever started saving.
/// </summary>
public sealed class VoucherHandlersEnforceTafsiliLevelsTests
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid LevelId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static RequiredTafsiliLevelMissingException Rejection() =>
        new(AccountId, ["مرکز هزینه"]);

    private static Mock<ICurrentUser> CurrentUser()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("user1");
        return currentUser;
    }

    private static TB_VOUCHERSHEAD ExistingHead(Guid id) => new()
    {
        ID = id,
        // Draft: the editable state these tests assume. Phase 38 fails closed on an absent or
        // unknown DOCLIFE, which is its own rule with its own tests.
        DOCLIFE = DocLife.Draft,
        DOC_NUM = "000001",
        DATE_DOC = "14050101",
        VAHEDCODE = "0001",
        YEAR = "1405",
        ADDUSERID = "creator1",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = false,
    };

    private static TB_VOUCHERSDETAIL ExistingLine(Guid id, Guid? accountId) => new()
    {
        ID = id,
        VOUCHERSHEAD_ID = Guid.NewGuid(),
        ACCOUNT_ID = accountId,
        VAHEDCODE = "0001",
        YEAR = "1405",
        ADDUSERID = "creator1",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = false,
    };

    [Fact]
    public async Task CreateVoucherDetail_ChecksTheLinesOwnAccountAndTafsili()
    {
        var headId = Guid.NewGuid();
        var headRepository = new Mock<IVoucherHeadRepository>();
        headRepository
            .Setup(r => r.GetForUpdateAsync(headId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingHead(headId));
        var guard = TafsiliLevelGuards.PermissiveMock();

        var handler = new CreateVoucherDetailCommandHandler(
            headRepository.Object,
            new Mock<IVoucherDetailRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUser().Object,
            guard.Object);

        await handler.Handle(
            new CreateVoucherDetailCommand(
                VoucherHeadId: headId,
                AccountId: AccountId,
                ReceiptId: null,
                CheckId: null,
                LowLevelCodeId: null,
                EtebarId: null,
                Description: null,
                Radif: null,
                Debtor: null,
                Creditor: null,
                Year: "1405",
                TafsiliLinks: [new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), LevelId)])
            {
                VahedCode = "0001",
            },
            CancellationToken.None);

        guard.Verify(
            g => g.EnsureSatisfiedAsync(
                AccountId,
                It.Is<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(links =>
                    links.Count == 1 && links.First().LevelId == LevelId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateVoucherDetail_StagesNothingWhenTheRuleRejects()
    {
        var headId = Guid.NewGuid();
        var headRepository = new Mock<IVoucherHeadRepository>();
        headRepository
            .Setup(r => r.GetForUpdateAsync(headId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingHead(headId));
        var detailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new CreateVoucherDetailCommandHandler(
            headRepository.Object,
            detailRepository.Object,
            unitOfWork.Object,
            CurrentUser().Object,
            TafsiliLevelGuards.Rejecting(Rejection()));

        await Assert.ThrowsAsync<RequiredTafsiliLevelMissingException>(() => handler.Handle(
            new CreateVoucherDetailCommand(
                VoucherHeadId: headId,
                AccountId: AccountId,
                ReceiptId: null,
                CheckId: null,
                LowLevelCodeId: null,
                EtebarId: null,
                Description: null,
                Radif: null,
                Debtor: null,
                Creditor: null,
                Year: "1405",
                TafsiliLinks: null)
            {
                VahedCode = "0001",
            },
            CancellationToken.None));

        detailRepository.Verify(
            r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// The update path has to resolve the line's <i>final</i> تفصیلی state itself: a null list means
    /// "leave the stored assignments alone", so those are what the rule has to see.
    /// </summary>
    [Fact]
    public async Task UpdateVoucherDetail_ChecksTheStoredTafsili_WhenTheRequestChangesOnlyTheAccount()
    {
        var lineId = Guid.NewGuid();
        var storedTafsiliId = Guid.NewGuid();
        var detailRepository = new Mock<IVoucherDetailRepository>();
        detailRepository
            .Setup(r => r.GetForUpdateAsync(lineId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingLine(lineId, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")));
        detailRepository
            .Setup(r => r.GetActiveTafsiliLinksAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_VOUCHERDETAIL_LINK_TAFSILI>
            {
                new()
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSDETAIL_ID = lineId,
                    TAFSILI_ID = storedTafsiliId,
                    LEVEL_ID = LevelId,
                    ADDUSERID = "creator1",
                    CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ISDELETED = false,
                },
            });
        var guard = TafsiliLevelGuards.PermissiveMock();

        var handler = new UpdateVoucherDetailCommandHandler(
            detailRepository.Object,
            VoucherHeadStubs.NotFound(),
            new Mock<IUnitOfWork>().Object,
            CurrentUser().Object,
            guard.Object);

        await handler.Handle(UpdateCommand(lineId, AccountId, tafsiliLinks: null), CancellationToken.None);

        guard.Verify(
            g => g.EnsureSatisfiedAsync(
                AccountId,
                It.Is<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(links =>
                    links.Count == 1 && links.First().TafsiliId == storedTafsiliId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Rows written before the rule existed can violate it. Re-validating them on an edit that
    /// touches neither the حساب nor the تفصیلی would make such a line permanently uneditable — a
    /// user could not even fix its شرح — for data they did not create.
    /// </summary>
    [Fact]
    public async Task UpdateVoucherDetail_SkipsTheRule_WhenNeitherAccountNorTafsiliChanges()
    {
        var lineId = Guid.NewGuid();
        var detailRepository = new Mock<IVoucherDetailRepository>();
        detailRepository
            .Setup(r => r.GetForUpdateAsync(lineId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingLine(lineId, AccountId));
        var guard = TafsiliLevelGuards.PermissiveMock();

        var handler = new UpdateVoucherDetailCommandHandler(
            detailRepository.Object,
            VoucherHeadStubs.NotFound(),
            new Mock<IUnitOfWork>().Object,
            CurrentUser().Object,
            guard.Object);

        await handler.Handle(UpdateCommand(lineId, AccountId, tafsiliLinks: null), CancellationToken.None);

        guard.VerifyNoOtherCalls();
        detailRepository.Verify(
            r => r.GetActiveTafsiliLinksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateVoucherDetail_ChecksTheRequestedTafsili_WhenTheRequestSuppliesThem()
    {
        var lineId = Guid.NewGuid();
        var detailRepository = new Mock<IVoucherDetailRepository>();
        detailRepository
            .Setup(r => r.GetForUpdateAsync(lineId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingLine(lineId, AccountId));
        detailRepository
            .Setup(r => r.GetActiveTafsiliLinksAsync(lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_VOUCHERDETAIL_LINK_TAFSILI>());
        var guard = TafsiliLevelGuards.PermissiveMock();

        var handler = new UpdateVoucherDetailCommandHandler(
            detailRepository.Object,
            VoucherHeadStubs.NotFound(),
            new Mock<IUnitOfWork>().Object,
            CurrentUser().Object,
            guard.Object);

        await handler.Handle(
            UpdateCommand(lineId, AccountId, [new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), LevelId)]),
            CancellationToken.None);

        guard.Verify(
            g => g.EnsureSatisfiedAsync(
                AccountId,
                It.Is<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(links =>
                    links.Count == 1 && links.First().LevelId == LevelId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateVoucherDetail_SavesNothingWhenTheRuleRejects()
    {
        var lineId = Guid.NewGuid();
        var detailRepository = new Mock<IVoucherDetailRepository>();
        detailRepository
            .Setup(r => r.GetForUpdateAsync(lineId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingLine(lineId, AccountId));
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateVoucherDetailCommandHandler(
            detailRepository.Object,
            VoucherHeadStubs.NotFound(),
            unitOfWork.Object,
            CurrentUser().Object,
            TafsiliLevelGuards.Rejecting(Rejection()));

        await Assert.ThrowsAsync<RequiredTafsiliLevelMissingException>(() => handler.Handle(
            UpdateCommand(lineId, AccountId, [new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), LevelId)]),
            CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// CreateVoucherHeadDetailInput has no tafsiliLinks field (open risk #21), so every initial
    /// line is necessarily created without تفصیلی — which is exactly what the rule has to be told,
    /// rather than being handed a list the input cannot carry.
    /// </summary>
    [Fact]
    public async Task CreateVoucherHead_ChecksEveryInitialLineWithNoTafsili()
    {
        var secondAccountId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var guard = TafsiliLevelGuards.PermissiveMock();

        var handler = new CreateVoucherHeadCommandHandler(
            new Mock<IVoucherHeadRepository>().Object,
            new Mock<IVoucherDetailRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUser().Object,
            guard.Object);

        await handler.Handle(HeadCommand([AccountId, secondAccountId]), CancellationToken.None);

        guard.Verify(
            g => g.EnsureSatisfiedAsync(
                AccountId,
                It.Is<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(links => links.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
        guard.Verify(
            g => g.EnsureSatisfiedAsync(
                secondAccountId,
                It.Is<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(links => links.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// All-or-nothing: one bad line must not leave the head and the lines before it staged.
    /// </summary>
    [Fact]
    public async Task CreateVoucherHead_StagesNoLineWhenAnyOfThemIsRejected()
    {
        var detailRepository = new Mock<IVoucherDetailRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new CreateVoucherHeadCommandHandler(
            new Mock<IVoucherHeadRepository>().Object,
            detailRepository.Object,
            unitOfWork.Object,
            CurrentUser().Object,
            TafsiliLevelGuards.Rejecting(Rejection()));

        await Assert.ThrowsAsync<RequiredTafsiliLevelMissingException>(
            () => handler.Handle(HeadCommand([AccountId]), CancellationToken.None));

        detailRepository.Verify(
            r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UpdateVoucherDetailCommand UpdateCommand(
        Guid id,
        Guid? accountId,
        IReadOnlyList<VoucherDetailTafsiliLinkInput>? tafsiliLinks) =>
        new(
            Id: id,
            AccountId: accountId,
            ReceiptId: null,
            CheckId: null,
            LowLevelCodeId: null,
            EtebarId: null,
            Description: null,
            Radif: null,
            Debtor: null,
            Creditor: null,
            Year: "1405",
            TafsiliLinks: tafsiliLinks)
        {
            VahedCode = "0001",
        };

    private static CreateVoucherHeadCommand HeadCommand(IReadOnlyList<Guid> accountIds) =>
        new(
            DocNum: "000001",
            DateDoc: "14050101",
            DocLife: null,
            HeadDesc: null,
            Apendix: null,
            SystemTypeId: null,
            FlagState: null,
            Year: "1405",
            IsAutomatic: null,
            SndVahedCode: null,
            ParentHeadId: null,
            AttachFileName: null,
            AtfNum: null,
            InitialDetails: accountIds
                .Select(accountId => new CreateVoucherHeadDetailInput(
                    AccountId: accountId,
                    ReceiptId: null,
                    CheckId: null,
                    LowLevelCodeId: null,
                    EtebarId: null,
                    Description: null,
                    Radif: null,
                    Debtor: null,
                    Creditor: null))
                .ToList())
        {
            VahedCode = "0001",
        };
}
