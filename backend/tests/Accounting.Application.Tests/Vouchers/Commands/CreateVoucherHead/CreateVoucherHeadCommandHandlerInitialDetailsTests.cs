using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.CreateVoucherHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.CreateVoucherHead;

/// <summary>
/// Dedicated coverage for the composite-create behaviour of <see cref="CreateVoucherHeadCommandHandler"/>
/// introduced by <see cref="CreateVoucherHeadCommand.InitialDetails"/> — the new architectural
/// claim under test for this QA pass (head + opening <c>TB_VOUCHERSDETAIL</c> lines persisted in
/// exactly one <see cref="IUnitOfWork.SaveChangesAsync"/> call). Kept in a separate file from
/// <see cref="CreateVoucherHeadCommandHandlerTests"/> (which pins the pre-existing, unchanged,
/// header-only behaviour) so a reviewer can see at a glance which coverage is new.
/// </summary>
public sealed class CreateVoucherHeadCommandHandlerInitialDetailsTests
{
    private static CreateVoucherHeadCommand ValidCommand(
        IReadOnlyList<CreateVoucherHeadDetailInput>? initialDetails = null) => new(
        DocNum: "000001",
        DateDoc: "14050101",
        DocLife: true,
        HeadDesc: "سند افتتاحیه",
        Apendix: null,
        SystemTypeId: null,
        FlagState: null,
        VahedCode: "0001",
        Year: "1405",
        IsAutomatic: false,
        SndVahedCode: null,
        ParentHeadId: null,
        AttachFileName: null,
        AtfNum: null,
        InitialDetails: initialDetails);

    private static CreateVoucherHeadDetailInput SampleDetail(int radif) => new(
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: $"line {radif}",
        Radif: radif,
        Debtor: radif % 2 == 0 ? 1000m : null,
        Creditor: radif % 2 == 0 ? null : 1000m);

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    private sealed class Fixture
    {
        public Mock<IVoucherHeadRepository> HeadRepository { get; } = new();
        public Mock<IVoucherDetailRepository> DetailRepository { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public Mock<ICurrentUser> CurrentUser { get; } = CurrentUserMock();
        public TB_VOUCHERSHEAD? StagedHead { get; private set; }
        public List<TB_VOUCHERSDETAIL> StagedDetails { get; } = new();
        public List<string> CallOrder { get; } = new();

        public Fixture()
        {
            HeadRepository
                .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
                .Callback<TB_VOUCHERSHEAD, CancellationToken>((entity, _) =>
                {
                    StagedHead = entity;
                    CallOrder.Add("AddAsync:Head");
                })
                .Returns(Task.CompletedTask);

            DetailRepository
                .Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
                .Callback<TB_VOUCHERSDETAIL, CancellationToken>((entity, _) =>
                {
                    StagedDetails.Add(entity);
                    CallOrder.Add("AddAsync:Detail");
                })
                .Returns(Task.CompletedTask);

            UnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => CallOrder.Add("SaveChangesAsync"))
                .ReturnsAsync(1);
        }

        public CreateVoucherHeadCommandHandler CreateHandler() => new(
            HeadRepository.Object,
            DetailRepository.Object,
            UnitOfWork.Object,
            CurrentUser.Object);
    }

    [Fact]
    public async Task Handle_InitialDetailsNull_BehavesExactlyAsHeaderOnlyCreate()
    {
        var fixture = new Fixture();
        var handler = fixture.CreateHandler();

        var id = await handler.Handle(ValidCommand(initialDetails: null), CancellationToken.None);

        Assert.NotNull(fixture.StagedHead);
        Assert.Equal(id, fixture.StagedHead!.ID);
        Assert.Empty(fixture.StagedDetails);
        fixture.DetailRepository.Verify(
            r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InitialDetailsEmptyList_BehavesExactlyAsHeaderOnlyCreate()
    {
        var fixture = new Fixture();
        var handler = fixture.CreateHandler();

        var id = await handler.Handle(
            ValidCommand(initialDetails: Array.Empty<CreateVoucherHeadDetailInput>()),
            CancellationToken.None);

        Assert.NotNull(fixture.StagedHead);
        Assert.Equal(id, fixture.StagedHead!.ID);
        Assert.Empty(fixture.StagedDetails);
        fixture.DetailRepository.Verify(
            r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NInitialDetails_StagesNDetailLines_EachWiredToGeneratedHeadId()
    {
        var fixture = new Fixture();
        var handler = fixture.CreateHandler();
        var details = new[] { SampleDetail(1), SampleDetail(2), SampleDetail(3) };

        var headId = await handler.Handle(ValidCommand(details), CancellationToken.None);

        Assert.Equal(3, fixture.StagedDetails.Count);
        Assert.All(fixture.StagedDetails, d => Assert.Equal(headId, d.VOUCHERSHEAD_ID));
    }

    [Fact]
    public async Task Handle_DetailLines_CopyVahedCodeAndYearFromHead_NeverFromLineInput()
    {
        // CreateVoucherHeadDetailInput structurally has no VahedCode/Year property at all — this
        // test proves the handler sources both from the command's (head-level) VahedCode/Year,
        // not from anything the line could carry.
        var fixture = new Fixture();
        var handler = fixture.CreateHandler();
        var command = ValidCommand(new[] { SampleDetail(1), SampleDetail(2) }) with
        {
            VahedCode = "0099",
            Year = "1408",
        };

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, fixture.StagedDetails.Count);
        Assert.All(fixture.StagedDetails, d =>
        {
            Assert.Equal("0099", d.VAHEDCODE);
            Assert.Equal("1408", d.YEAR);
        });
    }

    [Fact]
    public async Task Handle_HeadAndDetails_SaveChangesCalledExactlyOnce_AndEveryAddAsyncBeforeIt()
    {
        var fixture = new Fixture();
        var handler = fixture.CreateHandler();
        var details = new[] { SampleDetail(1), SampleDetail(2), SampleDetail(3) };

        await handler.Handle(ValidCommand(details), CancellationToken.None);

        fixture.HeadRepository.Verify(
            r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.DetailRepository.Verify(
            r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        fixture.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Strict ordering: SaveChangesAsync must be the very last call, after the head AND every
        // detail line has already been staged.
        Assert.Equal(5, fixture.CallOrder.Count); // 1 head AddAsync + 3 detail AddAsync + 1 SaveChangesAsync
        Assert.Equal("SaveChangesAsync", fixture.CallOrder[^1]);
        Assert.Equal("AddAsync:Head", fixture.CallOrder[0]);
        Assert.Equal(3, fixture.CallOrder.Count(c => c == "AddAsync:Detail"));
        Assert.DoesNotContain("SaveChangesAsync", fixture.CallOrder.Take(fixture.CallOrder.Count - 1));
    }

    [Fact]
    public async Task Handle_HeadAndAllDetailLines_ShareOneIdenticalCreatedDate_AndSameAddUserIdFromCurrentUser()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.SetupGet(u => u.UserId).Returns("srvusr01");
        var handler = fixture.CreateHandler();
        var details = new[] { SampleDetail(1), SampleDetail(2), SampleDetail(3) };

        await handler.Handle(ValidCommand(details), CancellationToken.None);

        Assert.NotNull(fixture.StagedHead);
        Assert.NotNull(fixture.StagedHead!.CREATEDDATE);
        Assert.Equal("srvusr01", fixture.StagedHead.ADDUSERID);

        Assert.Equal(3, fixture.StagedDetails.Count);
        Assert.All(fixture.StagedDetails, d =>
        {
            Assert.Equal(fixture.StagedHead.CREATEDDATE, d.CREATEDDATE);
            Assert.Equal("srvusr01", d.ADDUSERID);
        });

        // All three lines (and the head) must carry the exact same DateTime value — not merely
        // "close" values computed independently per line.
        var distinctCreatedDates = fixture.StagedDetails
            .Select(d => d.CREATEDDATE)
            .Append(fixture.StagedHead.CREATEDDATE)
            .Distinct()
            .Count();
        Assert.Equal(1, distinctCreatedDates);
    }

    [Fact]
    public async Task Handle_EachDetailLine_GetsDistinctNonEmptyId_AndIsDeletedFalse()
    {
        var fixture = new Fixture();
        var handler = fixture.CreateHandler();
        var details = new[] { SampleDetail(1), SampleDetail(2), SampleDetail(3) };

        await handler.Handle(ValidCommand(details), CancellationToken.None);

        Assert.Equal(3, fixture.StagedDetails.Count);
        Assert.All(fixture.StagedDetails, d => Assert.NotEqual(Guid.Empty, d.ID));
        Assert.Equal(3, fixture.StagedDetails.Select(d => d.ID).Distinct().Count());
        Assert.All(fixture.StagedDetails, d => Assert.False(d.ISDELETED));
    }

    [Fact]
    public async Task Handle_DetailLines_MapRemainingFieldsFromInput()
    {
        var fixture = new Fixture();
        var handler = fixture.CreateHandler();
        var detail = SampleDetail(7);

        await handler.Handle(ValidCommand(new[] { detail }), CancellationToken.None);

        var staged = Assert.Single(fixture.StagedDetails);
        Assert.Equal(detail.AccountId, staged.ACCOUNT_ID);
        Assert.Equal(detail.ReceiptId, staged.RECEIP_ID);
        Assert.Equal(detail.CheckId, staged.CHECK_ID);
        Assert.Equal(detail.LowLevelCodeId, staged.LOWLEVELCODE_ID);
        Assert.Equal(detail.EtebarId, staged.ETEBAR_ID);
        Assert.Equal(detail.Description, staged.DESCRIPTION);
        Assert.Equal(detail.Radif, staged.RADIF);
        Assert.Equal(detail.Debtor, staged.DEBTOR);
        Assert.Equal(detail.Creditor, staged.CREDITOR);
    }
}
