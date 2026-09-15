using Accounting.Application.BankAccounts.Commands.Common;
using Accounting.Application.BankAccounts.Commands.CreateBankAccount;
using Accounting.Application.BankAccounts.Commands.UpdateBankAccount;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.BankAccounts.Commands;

/// <summary>
/// Covers the <c>TB_ACCOUNT_LINK_TAFSILI</c> write path added so a bank account can carry the
/// تفصیلی assignments of its معین's active levels — the embedded-table shape sanctioned by
/// <see cref="Common.NoIndependentLinkTableWritePathTests"/> (parent-scoped methods on the
/// parent's own repository, driven only by the parent's Create/Update command).
///
/// Both the create and the update path are exercised here rather than in the two existing
/// handler test classes, because what is being asserted is one feature spanning both.
/// </summary>
public sealed class BankAccountTafsiliLinkWriteTests
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

    private static CreateBankAccountCommand CreateCommand(IReadOnlyList<BankAccountTafsiliLinkInput>? links) => new(
        AccountNumber: "1234567890",
        AccountHolder: "علی رضایی",
        CardNumber: null,
        ShebaNumber: null,
        FirstAmount: null,
        BankId: null,
        BranchId: null,
        AccountTypeId: null,
        AccountCodeId: Guid.NewGuid(),
        CheckFile: null,
        AccountOpeningDate: null,
        TafsiliLinks: links)
    {
        VahedCode = "0001",
    };

    private static UpdateBankAccountCommand UpdateCommand(Guid id, IReadOnlyList<BankAccountTafsiliLinkInput>? links) => new(
        Id: id,
        AccountNumber: "1234567890",
        AccountHolder: "علی رضایی",
        CardNumber: null,
        ShebaNumber: null,
        FirstAmount: null,
        BankId: null,
        BranchId: null,
        AccountTypeId: null,
        AccountCodeId: Guid.NewGuid(),
        CheckFile: null,
        AccountOpeningDate: null,
        TafsiliLinks: links)
    {
        VahedCode = "0001",
    };

    [Fact]
    public async Task Create_StagesOneLinkPerInput_InheritingParentAccountAndUnitAndUser()
    {
        var repository = new Mock<IBankAccountRepository>();
        TB_ACCOUNT? stagedAccount = null;
        var stagedLinks = new List<TB_ACCOUNT_LINK_TAFSILI>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_ACCOUNT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT, CancellationToken>((a, _) => stagedAccount = a);
        repository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_ACCOUNT_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT_LINK_TAFSILI, CancellationToken>((l, _) => stagedLinks.Add(l));

        var handler = new CreateBankAccountCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock("creator").Object);

        await handler.Handle(
            CreateCommand(new[]
            {
                new BankAccountTafsiliLinkInput(TafsiliA, LevelOne),
                new BankAccountTafsiliLinkInput(TafsiliB, LevelTwo),
            }),
            CancellationToken.None);

        Assert.NotNull(stagedAccount);
        Assert.Equal(2, stagedLinks.Count);
        Assert.All(stagedLinks, link =>
        {
            Assert.Equal(stagedAccount!.ID, link.ACCOUNT_ID);
            Assert.Equal("0001", link.VAHEDCODE);
            Assert.Equal("creator", link.ADDUSERID);
            Assert.False(link.ISDELETED);
            Assert.NotEqual(Guid.Empty, link.ID);
        });
        Assert.Contains(stagedLinks, l => l.TAFSILI_ID == TafsiliA && l.LEVEL_ID == LevelOne);
        Assert.Contains(stagedLinks, l => l.TAFSILI_ID == TafsiliB && l.LEVEL_ID == LevelTwo);
    }

    [Fact]
    public async Task Create_WithoutLinks_StagesNone()
    {
        var repository = new Mock<IBankAccountRepository>();
        var handler = new CreateBankAccountCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock().Object);

        await handler.Handle(CreateCommand(null), CancellationToken.None);

        repository.Verify(
            r => r.AddTafsiliLinkAsync(It.IsAny<TB_ACCOUNT_LINK_TAFSILI>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_AddsNewLink_KeepsUnchangedOne_AndSoftDeletesDroppedOne()
    {
        var accountId = Guid.NewGuid();
        var keptLink = new TB_ACCOUNT_LINK_TAFSILI
        {
            ID = Guid.NewGuid(),
            ACCOUNT_ID = accountId,
            TAFSILI_ID = TafsiliA,
            LEVEL_ID = LevelOne,
            ADDUSERID = "original-user",
            CREATEDDATE = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ISDELETED = false,
        };
        var droppedLink = new TB_ACCOUNT_LINK_TAFSILI
        {
            ID = Guid.NewGuid(),
            ACCOUNT_ID = accountId,
            TAFSILI_ID = TafsiliB,
            LEVEL_ID = LevelTwo,
            ADDUSERID = "original-user",
            CREATEDDATE = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ISDELETED = false,
        };

        var repository = new Mock<IBankAccountRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_ACCOUNT { ID = accountId, ISDELETED = false });
        repository
            .Setup(r => r.GetActiveTafsiliLinksAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { keptLink, droppedLink });
        var addedLinks = new List<TB_ACCOUNT_LINK_TAFSILI>();
        repository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_ACCOUNT_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ACCOUNT_LINK_TAFSILI, CancellationToken>((l, _) => addedLinks.Add(l));

        var newTafsili = Guid.NewGuid();
        var handler = new UpdateBankAccountCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock("editor").Object);

        await handler.Handle(
            UpdateCommand(accountId, new[]
            {
                new BankAccountTafsiliLinkInput(TafsiliA, LevelOne), // unchanged
                new BankAccountTafsiliLinkInput(newTafsili, LevelTwo), // replaces the dropped one
            }),
            CancellationToken.None);

        // Unchanged link: not re-stamped, so an unrelated account edit never rewrites its audit trail.
        Assert.False(keptLink.ISDELETED);
        Assert.Null(keptLink.CHANGEUSERID);
        Assert.Null(keptLink.UPDATEDDATE);

        Assert.True(droppedLink.ISDELETED);
        Assert.Equal("editor", droppedLink.CHANGEUSERID);
        Assert.NotNull(droppedLink.UPDATEDDATE);

        var added = Assert.Single(addedLinks);
        Assert.Equal(newTafsili, added.TAFSILI_ID);
        Assert.Equal(LevelTwo, added.LEVEL_ID);
        Assert.Equal(accountId, added.ACCOUNT_ID);
        Assert.Equal("editor", added.ADDUSERID);
    }

    [Fact]
    public async Task Update_WithNullLinks_ClearsEveryExistingLink()
    {
        var accountId = Guid.NewGuid();
        var existing = new TB_ACCOUNT_LINK_TAFSILI
        {
            ID = Guid.NewGuid(),
            ACCOUNT_ID = accountId,
            TAFSILI_ID = TafsiliA,
            LEVEL_ID = LevelOne,
            ADDUSERID = "original-user",
            ISDELETED = false,
        };

        var repository = new Mock<IBankAccountRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_ACCOUNT { ID = accountId, ISDELETED = false });
        repository
            .Setup(r => r.GetActiveTafsiliLinksAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existing });

        var handler = new UpdateBankAccountCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock().Object);

        await handler.Handle(UpdateCommand(accountId, null), CancellationToken.None);

        Assert.True(existing.ISDELETED);
    }
}
