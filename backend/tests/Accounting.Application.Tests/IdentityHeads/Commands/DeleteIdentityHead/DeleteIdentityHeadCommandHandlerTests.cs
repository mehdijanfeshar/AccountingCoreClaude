using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.IdentityHeads.Commands.DeleteIdentityHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.IdentityHeads.Commands.DeleteIdentityHead;

public sealed class DeleteIdentityHeadCommandHandlerTests
{
    private static TB_IDENTITYHEAD ExistingHead(Guid id, bool isDeleted = false) => new()
    {
        ID = id,
        IDENTITYGROUPS_ID = Guid.NewGuid(),
        SERIAL = 1,
        VAHEDCODE = "1155",
        YEAR = "1404",
        ADDUSERID = "creator",
        CREATEDDATE = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = isDeleted,
    };

    private static TB_IDENTITYFIXITEM ActiveItem(Guid headId) => new()
    {
        ID = Guid.NewGuid(),
        IDENTITYHEAD_ID = headId,
        IDENTITYSUBGRPS_ID = Guid.NewGuid(),
        FIXITEMS_VALUE = "مقدار",
        VAHEDCODE = "1155",
        YEAR = "1404",
        ADDUSERID = "creator",
        CREATEDDATE = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        ISDELETED = false,
    };

    private sealed record Harness(
        Mock<IIdentityHeadRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork,
        DeleteIdentityHeadCommandHandler Handler);

    private static Harness CreateHarness(TB_IDENTITYHEAD? head, IEnumerable<TB_IDENTITYFIXITEM>? items = null)
    {
        var repository = new Mock<IIdentityHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("remover");

        repository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(head);
        repository
            .Setup(r => r.GetActiveFixItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((items ?? Array.Empty<TB_IDENTITYFIXITEM>()).ToList());

        return new Harness(
            repository,
            unitOfWork,
            new DeleteIdentityHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object));
    }

    [Fact]
    public async Task Handle_MissingHead_ThrowsNotFound()
    {
        var harness = CreateHarness(head: null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => harness.Handler.Handle(new DeleteIdentityHeadCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SoftDeletesTheHead()
    {
        var id = Guid.NewGuid();
        var head = ExistingHead(id);
        var harness = CreateHarness(head);

        await harness.Handler.Handle(new DeleteIdentityHeadCommand(id), CancellationToken.None);

        Assert.True(head.ISDELETED);
        Assert.Equal("remover", head.CHANGEUSERID);
        Assert.NotNull(head.UPDATEDDATE);
    }

    /// <summary>
    /// Without the cascade the items would outlive their parent and keep occupying their slot in
    /// AK_AK_IDENTYFIXITEMS_IDENTYFI — the orphan-rows problem phase 9 fixed for voucher lines.
    /// </summary>
    [Fact]
    public async Task Handle_CascadesToTheFixItems()
    {
        var id = Guid.NewGuid();
        var first = ActiveItem(id);
        var second = ActiveItem(id);
        var harness = CreateHarness(ExistingHead(id), new[] { first, second });

        await harness.Handler.Handle(new DeleteIdentityHeadCommand(id), CancellationToken.None);

        Assert.True(first.ISDELETED);
        Assert.True(second.ISDELETED);
        Assert.Equal("remover", first.CHANGEUSERID);
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_IsIdempotentAndDoesNotSave()
    {
        var id = Guid.NewGuid();
        var harness = CreateHarness(ExistingHead(id, isDeleted: true));

        await harness.Handler.Handle(new DeleteIdentityHeadCommand(id), CancellationToken.None);

        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SavesOnce_SoHeadAndItemsMoveTogether()
    {
        var id = Guid.NewGuid();
        var harness = CreateHarness(ExistingHead(id), new[] { ActiveItem(id) });

        await harness.Handler.Handle(new DeleteIdentityHeadCommand(id), CancellationToken.None);

        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
