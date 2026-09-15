using Accounting.Application.Expenses.Commands.Common;
using Accounting.Application.Expenses.Commands.CreateExpense;
using Accounting.Application.Expenses.Commands.UpdateExpense;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Expenses.Commands;

/// <summary>
/// Covers the <c>TB_EXPENCE_LINK_TAFSILI</c> write path — the embedded-table shape sanctioned by
/// <see cref="Tests.Common.NoIndependentLinkTableWritePathTests"/>. Mirrors
/// <c>BankAccounts.Commands.BankAccountTafsiliLinkWriteTests</c>, which is the reference
/// implementation for this feature across every entity that points at a معین.
/// </summary>
public sealed class ExpenseTafsiliLinkWriteTests
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

    private static CreateExpenseCommand CreateCommand(IReadOnlyList<ExpenseTafsiliLinkInput>? links) => new(
        ExpenseCode: "0001",
        ExpenseName: "هزینه یک",
        Description: null,
        DefaultAmount: null,
        ExpenseGroupId: null,
        AccountCodeId: Guid.NewGuid(),
        TafsiliLinks: links)
    {
        VahedCode = "0001",
    };

    private static UpdateExpenseCommand UpdateCommand(Guid id, IReadOnlyList<ExpenseTafsiliLinkInput>? links) => new(
        Id: id,
        ExpenseCode: "0001",
        ExpenseName: "هزینه یک",
        Description: null,
        DefaultAmount: null,
        ExpenseGroupId: null,
        AccountCodeId: Guid.NewGuid(),
        TafsiliLinks: links)
    {
        VahedCode = "0001",
    };

    [Fact]
    public async Task Create_StagesOneLinkPerInput_InheritingParentKeyAndUnitAndUser()
    {
        var repository = new Mock<IExpenseRepository>();
        TB_EXPENCE? stagedParent = null;
        var stagedLinks = new List<TB_EXPENCE_LINK_TAFSILI>();
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_EXPENCE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_EXPENCE, CancellationToken>((p, _) => stagedParent = p);
        repository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_EXPENCE_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_EXPENCE_LINK_TAFSILI, CancellationToken>((l, _) => stagedLinks.Add(l));

        var handler = new CreateExpenseCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock("creator").Object);

        await handler.Handle(
            CreateCommand(new[]
            {
                new ExpenseTafsiliLinkInput(TafsiliA, LevelOne),
                new ExpenseTafsiliLinkInput(TafsiliB, LevelTwo),
            }),
            CancellationToken.None);

        Assert.NotNull(stagedParent);
        Assert.Equal(2, stagedLinks.Count);
        Assert.All(stagedLinks, link =>
        {
            Assert.Equal(stagedParent!.ID, link.EXPENSE_ID);
            Assert.Equal("0001", link.VAHEDCODE);
            Assert.Equal("creator", link.ADDUSERID);
            Assert.False(link.ISDELETED);
        });
        Assert.Contains(stagedLinks, l => l.TAFSILI_ID == TafsiliA && l.LEVEL_ID == LevelOne);
        Assert.Contains(stagedLinks, l => l.TAFSILI_ID == TafsiliB && l.LEVEL_ID == LevelTwo);
    }

    [Fact]
    public async Task Create_WithoutLinks_StagesNone()
    {
        var repository = new Mock<IExpenseRepository>();
        var handler = new CreateExpenseCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock().Object);

        await handler.Handle(CreateCommand(null), CancellationToken.None);

        repository.Verify(
            r => r.AddTafsiliLinkAsync(It.IsAny<TB_EXPENCE_LINK_TAFSILI>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_AddsNewLink_KeepsUnchangedOne_AndSoftDeletesDroppedOne()
    {
        var parentId = Guid.NewGuid();
        var keptLink = new TB_EXPENCE_LINK_TAFSILI
        {
            ID = Guid.NewGuid(),
            EXPENSE_ID = parentId,
            TAFSILI_ID = TafsiliA,
            LEVEL_ID = LevelOne,
            ADDUSERID = "original-user",
            ISDELETED = false,
        };
        var droppedLink = new TB_EXPENCE_LINK_TAFSILI
        {
            ID = Guid.NewGuid(),
            EXPENSE_ID = parentId,
            TAFSILI_ID = TafsiliB,
            LEVEL_ID = LevelTwo,
            ADDUSERID = "original-user",
            ISDELETED = false,
        };

        var repository = new Mock<IExpenseRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_EXPENCE { ID = parentId, ISDELETED = false });
        repository
            .Setup(r => r.GetActiveTafsiliLinksAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { keptLink, droppedLink });
        var addedLinks = new List<TB_EXPENCE_LINK_TAFSILI>();
        repository
            .Setup(r => r.AddTafsiliLinkAsync(It.IsAny<TB_EXPENCE_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_EXPENCE_LINK_TAFSILI, CancellationToken>((l, _) => addedLinks.Add(l));

        var newTafsili = Guid.NewGuid();
        var handler = new UpdateExpenseCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock("editor").Object);

        await handler.Handle(
            UpdateCommand(parentId, new[]
            {
                new ExpenseTafsiliLinkInput(TafsiliA, LevelOne),
                new ExpenseTafsiliLinkInput(newTafsili, LevelTwo),
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
        Assert.Equal(parentId, added.EXPENSE_ID);
    }

    [Fact]
    public async Task Update_WithNullLinks_ClearsEveryExistingLink()
    {
        var parentId = Guid.NewGuid();
        var existing = new TB_EXPENCE_LINK_TAFSILI
        {
            ID = Guid.NewGuid(),
            EXPENSE_ID = parentId,
            TAFSILI_ID = TafsiliA,
            LEVEL_ID = LevelOne,
            ADDUSERID = "original-user",
            ISDELETED = false,
        };

        var repository = new Mock<IExpenseRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_EXPENCE { ID = parentId, ISDELETED = false });
        repository
            .Setup(r => r.GetActiveTafsiliLinksAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existing });

        var handler = new UpdateExpenseCommandHandler(
            repository.Object,
            new Mock<IUnitOfWork>().Object,
            CurrentUserMock().Object);

        await handler.Handle(UpdateCommand(parentId, null), CancellationToken.None);

        Assert.True(existing.ISDELETED);
    }
}
