using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.IdentityHeads.Commands.Common;
using Accounting.Application.IdentityHeads.Commands.UpdateIdentityHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.IdentityHeads.Commands.UpdateIdentityHead;

/// <summary>
/// The reconcile logic is the whole point of this handler, so most of these tests are about which
/// rows get touched — and, just as importantly, which do not.
/// </summary>
public sealed class UpdateIdentityHeadCommandHandlerTests
{
    private const string Vahed = "1155";
    private const string Year = "1404";
    private static readonly DateTime OriginalDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static TB_IDENTITYHEAD ExistingHead(Guid id, bool isDeleted = false) => new()
    {
        ID = id,
        IDENTITYGROUPS_ID = Guid.NewGuid(),
        SERIAL = 7,
        VAHEDCODE = Vahed,
        YEAR = Year,
        ADDUSERID = "creator",
        CREATEDDATE = OriginalDate,
        ISDELETED = isDeleted,
    };

    private static TB_IDENTITYFIXITEM ExistingItem(Guid headId, Guid subGroupId, string? value) => new()
    {
        ID = Guid.NewGuid(),
        IDENTITYHEAD_ID = headId,
        IDENTITYSUBGRPS_ID = subGroupId,
        FIXITEMS_VALUE = value,
        VAHEDCODE = Vahed,
        YEAR = Year,
        ADDUSERID = "creator",
        CREATEDDATE = OriginalDate,
        ISDELETED = false,
    };

    private sealed record Harness(
        Mock<IIdentityHeadRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork,
        UpdateIdentityHeadCommandHandler Handler,
        List<TB_IDENTITYFIXITEM> AddedFixItems);

    private static Harness CreateHarness(TB_IDENTITYHEAD? head, IEnumerable<TB_IDENTITYFIXITEM>? existingItems = null)
    {
        var repository = new Mock<IIdentityHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("editor");

        var added = new List<TB_IDENTITYFIXITEM>();

        repository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(head);
        repository
            .Setup(r => r.GetActiveFixItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((existingItems ?? Array.Empty<TB_IDENTITYFIXITEM>()).ToList());
        repository
            .Setup(r => r.AddFixItemAsync(It.IsAny<TB_IDENTITYFIXITEM>(), It.IsAny<CancellationToken>()))
            .Callback<TB_IDENTITYFIXITEM, CancellationToken>((f, _) => added.Add(f))
            .Returns(Task.CompletedTask);

        var handler = new UpdateIdentityHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        return new Harness(repository, unitOfWork, handler, added);
    }

    [Fact]
    public async Task Handle_MissingHead_ThrowsNotFound()
    {
        var harness = CreateHarness(head: null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => harness.Handler.Handle(new UpdateIdentityHeadCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeletedHead_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        var harness = CreateHarness(ExistingHead(id, isDeleted: true));

        await Assert.ThrowsAsync<NotFoundException>(
            () => harness.Handler.Handle(new UpdateIdentityHeadCommand(id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_StampsTheHeadsAuditTrail()
    {
        var id = Guid.NewGuid();
        var head = ExistingHead(id);
        var harness = CreateHarness(head);

        await harness.Handler.Handle(new UpdateIdentityHeadCommand(id), CancellationToken.None);

        Assert.Equal("editor", head.CHANGEUSERID);
        Assert.NotNull(head.UPDATEDDATE);
    }

    /// <summary>
    /// The group and serial identify the record; an edit of its values must never move it.
    /// </summary>
    [Fact]
    public async Task Handle_LeavesGroupAndSerialAlone()
    {
        var id = Guid.NewGuid();
        var head = ExistingHead(id);
        var originalGroup = head.IDENTITYGROUPS_ID;
        var harness = CreateHarness(head);

        await harness.Handler.Handle(new UpdateIdentityHeadCommand(id), CancellationToken.None);

        Assert.Equal(originalGroup, head.IDENTITYGROUPS_ID);
        Assert.Equal(7, head.SERIAL);
    }

    [Fact]
    public async Task Handle_ChangedValue_IsUpdatedAndRestamped()
    {
        var id = Guid.NewGuid();
        var subGroup = Guid.NewGuid();
        var item = ExistingItem(id, subGroup, "قدیمی");
        var harness = CreateHarness(ExistingHead(id), new[] { item });

        await harness.Handler.Handle(
            new UpdateIdentityHeadCommand(id, new[] { new IdentityHeadFixItemInput(subGroup, "جدید") }),
            CancellationToken.None);

        Assert.Equal("جدید", item.FIXITEMS_VALUE);
        Assert.Equal("editor", item.CHANGEUSERID);
        Assert.NotNull(item.UPDATEDDATE);
        Assert.False(item.ISDELETED);
    }

    /// <summary>
    /// The behaviour that separates this from the tafsili-link reconcile it is modelled on: an
    /// item whose value did not change keeps its original audit trail, so editing one field never
    /// rewrites the history of the others.
    /// </summary>
    [Fact]
    public async Task Handle_UnchangedValue_IsLeftCompletelyUntouched()
    {
        var id = Guid.NewGuid();
        var subGroup = Guid.NewGuid();
        var item = ExistingItem(id, subGroup, "بدون تغییر");
        var harness = CreateHarness(ExistingHead(id), new[] { item });

        await harness.Handler.Handle(
            new UpdateIdentityHeadCommand(id, new[] { new IdentityHeadFixItemInput(subGroup, "بدون تغییر") }),
            CancellationToken.None);

        Assert.Null(item.CHANGEUSERID);
        Assert.Null(item.UPDATEDDATE);
        Assert.Equal(OriginalDate, item.CREATEDDATE);
        Assert.Equal("creator", item.ADDUSERID);
    }

    [Fact]
    public async Task Handle_ItemDroppedFromTheSet_IsSoftDeleted()
    {
        var id = Guid.NewGuid();
        var kept = Guid.NewGuid();
        var dropped = Guid.NewGuid();
        var keptItem = ExistingItem(id, kept, "می‌ماند");
        var droppedItem = ExistingItem(id, dropped, "می‌رود");
        var harness = CreateHarness(ExistingHead(id), new[] { keptItem, droppedItem });

        await harness.Handler.Handle(
            new UpdateIdentityHeadCommand(id, new[] { new IdentityHeadFixItemInput(kept, "می‌ماند") }),
            CancellationToken.None);

        Assert.True(droppedItem.ISDELETED);
        Assert.Equal("editor", droppedItem.CHANGEUSERID);
        Assert.False(keptItem.ISDELETED);
    }

    [Fact]
    public async Task Handle_NewSubGroup_IsAdded_InheritingTheHeadsScope()
    {
        var id = Guid.NewGuid();
        var newSubGroup = Guid.NewGuid();
        var head = ExistingHead(id);
        var harness = CreateHarness(head);

        await harness.Handler.Handle(
            new UpdateIdentityHeadCommand(id, new[] { new IdentityHeadFixItemInput(newSubGroup, "تازه") }),
            CancellationToken.None);

        var added = Assert.Single(harness.AddedFixItems);
        Assert.Equal(id, added.IDENTITYHEAD_ID);
        Assert.Equal(newSubGroup, added.IDENTITYSUBGRPS_ID);
        Assert.Equal("تازه", added.FIXITEMS_VALUE);
        Assert.Equal(head.VAHEDCODE, added.VAHEDCODE);
        Assert.Equal(head.YEAR, added.YEAR);
        Assert.Equal("editor", added.ADDUSERID);
    }

    /// <summary>
    /// PUT-semantics: an empty set clears everything. Null is treated identically, so "not
    /// supplied" is never silently read as "leave them alone".
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_EmptyOrNullSet_SoftDeletesEveryItem(bool useNull)
    {
        var id = Guid.NewGuid();
        var item = ExistingItem(id, Guid.NewGuid(), "هرچه");
        var harness = CreateHarness(ExistingHead(id), new[] { item });

        var command = useNull
            ? new UpdateIdentityHeadCommand(id)
            : new UpdateIdentityHeadCommand(id, Array.Empty<IdentityHeadFixItemInput>());

        await harness.Handler.Handle(command, CancellationToken.None);

        Assert.True(item.ISDELETED);
    }

    [Fact]
    public async Task Handle_SavesOnce_SoHeadAndItemsMoveTogether()
    {
        var id = Guid.NewGuid();
        var harness = CreateHarness(ExistingHead(id), new[] { ExistingItem(id, Guid.NewGuid(), "الف") });

        await harness.Handler.Handle(
            new UpdateIdentityHeadCommand(id, new[] { new IdentityHeadFixItemInput(Guid.NewGuid(), "ب") }),
            CancellationToken.None);

        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
