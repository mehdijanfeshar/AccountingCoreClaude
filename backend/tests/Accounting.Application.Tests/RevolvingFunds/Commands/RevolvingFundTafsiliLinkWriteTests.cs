using Accounting.Application.RevolvingFunds.Commands.Common;
using Accounting.Application.RevolvingFunds.Commands.CreateRevolvingFund;
using Accounting.Application.RevolvingFunds.Commands.UpdateRevolvingFund;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.RevolvingFunds.Commands;

/// <summary>
/// Covers the <c>TB_REVOLVINGFUND_LINK_TAFSILI</c> write path — the embedded-table shape sanctioned by
/// <see cref="Tests.Common.NoIndependentLinkTableWritePathTests"/>. Mirrors
/// <c>BankAccounts.Commands.BankAccountTafsiliLinkWriteTests</c>, which is the reference
/// implementation for this feature across every entity that points at a معین.
/// </summary>
public sealed class RevolvingFundTafsiliLinkWriteTests
{
    private static readonly Guid LevelOne = Guid.NewGuid();
    private static readonly Guid LevelTwo = Guid.NewGuid();
    private static readonly Guid TafsiliA = Guid.NewGuid();
    private static readonly Guid TafsiliB = Guid.NewGuid();

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(c => c.UserId).Returns(userId);
        return currentUser;
    }

    private static CreateRevolvingFundCommand CreateCommand(IReadOnlyList<RevolvingFundTafsiliLinkInput>? links) => new(
        Code: "0001",
        Name: "تنخواه یک",
        Description: null,
        DefaultAmount: null,
        AccountCodeId: Guid.NewGuid(),
        Year: "1404",
        TafsiliLinks: links)
    {
        VahedCode = "0001",
    };

    private static UpdateRevolvingFundCommand UpdateCommand(Guid id, IReadOnlyList<RevolvingFundTafsiliLinkInput>? links) => new(
        Id: id,
        Code: "0001",
        Name: "تنخواه یک",
        Description: null,
        DefaultAmount: null,
        AccountCodeId: Guid.NewGuid(),
        Year: "1404",
        TafsiliLinks: links)
    {
        VahedCode = "0001",
    };

    [Fact]
    public async Task Create_StagesOneLinkPerInput_InheritingParentKeyAndUnitAndUser()
    {
        var repository = new Mock<IRevolvingFundRepository>();
        TB_REVOLVING_FUND? stagedParent = null;
        var stagedLinks = new List<TB_REVOLVINGFUND_LINK_TAFSILI>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_REVOLVING_FUND>(), It.IsAny<CancellationToken>()))
            .Callback<TB_REVOLVING_FUND, CancellationToken>((p, _) => stagedParent = p);
        repository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_REVOLVINGFUND_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_REVOLVINGFUND_LINK_TAFSILI, CancellationToken>((l, _) => stagedLinks.Add(l));

        var handler = new CreateRevolvingFundCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock("creator").Object);

        await handler.Handle(
            CreateCommand(new[]
            {
                new RevolvingFundTafsiliLinkInput(TafsiliA, LevelOne),
                new RevolvingFundTafsiliLinkInput(TafsiliB, LevelTwo),
            }),
            CancellationToken.None);

        Assert.NotNull(stagedParent);
        Assert.Equal(2, stagedLinks.Count);
        Assert.All(stagedLinks, link =>
        {
            Assert.Equal(stagedParent!.ID, link.REVOLVINGFUND_ID);
            Assert.Equal("0001", link.VAHEDCODE);
            Assert.Equal("creator", link.ADDUSERID);
            Assert.False(link.ISDELETED);
        });
        Assert.All(stagedLinks, link => Assert.Equal("1404", link.YEAR));
        Assert.Contains(stagedLinks, l => l.TAFSILI_ID == TafsiliA && l.LEVEL_ID == LevelOne);
        Assert.Contains(stagedLinks, l => l.TAFSILI_ID == TafsiliB && l.LEVEL_ID == LevelTwo);
    }

    [Fact]
    public async Task Create_WithoutLinks_StagesNone()
    {
        var repository = new Mock<IRevolvingFundRepository>();
        var handler = new CreateRevolvingFundCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock().Object);

        await handler.Handle(CreateCommand(null), CancellationToken.None);

        repository.Verify(
            r => r.AddTafsiliLinkAsync(It.IsAny<TB_REVOLVINGFUND_LINK_TAFSILI>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_AddsNewLink_KeepsUnchangedOne_AndSoftDeletesDroppedOne()
    {
        var parentId = Guid.NewGuid();
        var keptLink = new TB_REVOLVINGFUND_LINK_TAFSILI
        {
            ID = Guid.NewGuid(),
            REVOLVINGFUND_ID = parentId,
            TAFSILI_ID = TafsiliA,
            LEVEL_ID = LevelOne,
            ADDUSERID = "original-user",
            ISDELETED = false,
        };
        var droppedLink = new TB_REVOLVINGFUND_LINK_TAFSILI
        {
            ID = Guid.NewGuid(),
            REVOLVINGFUND_ID = parentId,
            TAFSILI_ID = TafsiliB,
            LEVEL_ID = LevelTwo,
            ADDUSERID = "original-user",
            ISDELETED = false,
        };

        var repository = new Mock<IRevolvingFundRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_REVOLVING_FUND { ID = parentId, ISDELETED = false });
        repository
            .Setup(r => r.GetActiveTafsiliLinksAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { keptLink, droppedLink });
        var addedLinks = new List<TB_REVOLVINGFUND_LINK_TAFSILI>();
        repository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_REVOLVINGFUND_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_REVOLVINGFUND_LINK_TAFSILI, CancellationToken>((l, _) => addedLinks.Add(l));

        var newTafsili = Guid.NewGuid();
        var handler = new UpdateRevolvingFundCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock("editor").Object);

        await handler.Handle(
            UpdateCommand(parentId, new[]
            {
                new RevolvingFundTafsiliLinkInput(TafsiliA, LevelOne),
                new RevolvingFundTafsiliLinkInput(newTafsili, LevelTwo),
            }),
            CancellationToken.None);

        // Survivor is left untouched, so an unrelated edit never rewrites its audit trail.
        Assert.False(keptLink.ISDELETED);
        Assert.Null(keptLink.CHANGEUSERID);
        Assert.Null(keptLink.UPDATEDDATE);

        Assert.True(droppedLink.ISDELETED);
        Assert.Equal("editor", droppedLink.CHANGEUSERID);

        var added = Assert.Single(addedLinks);
        Assert.Equal(newTafsili, added.TAFSILI_ID);
        Assert.Equal(LevelTwo, added.LEVEL_ID);
        Assert.Equal(parentId, added.REVOLVINGFUND_ID);
    }

    [Fact]
    public async Task Update_WithNullLinks_ClearsEveryExistingLink()
    {
        var parentId = Guid.NewGuid();
        var existing = new TB_REVOLVINGFUND_LINK_TAFSILI
        {
            ID = Guid.NewGuid(),
            REVOLVINGFUND_ID = parentId,
            TAFSILI_ID = TafsiliA,
            LEVEL_ID = LevelOne,
            ADDUSERID = "original-user",
            ISDELETED = false,
        };

        var repository = new Mock<IRevolvingFundRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_REVOLVING_FUND { ID = parentId, ISDELETED = false });
        repository
            .Setup(r => r.GetActiveTafsiliLinksAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existing });

        var handler = new UpdateRevolvingFundCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock().Object);

        await handler.Handle(UpdateCommand(parentId, null), CancellationToken.None);

        Assert.True(existing.ISDELETED);
    }
}
