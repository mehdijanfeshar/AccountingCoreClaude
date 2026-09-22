using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.ChangeVoucherState;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.ChangeVoucherState;

public sealed class ChangeVoucherStateCommandHandlerTests
{
    private static readonly DateTime OriginalDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static TB_VOUCHERSHEAD Head(Guid id, DocLife state = DocLife.Draft, bool? isDeleted = false) => new()
    {
        ID = id,
        DOC_NUM = "000001",
        DATE_DOC = "14040101",
        DOCLIFE = state,
        VAHEDCODE = "1155",
        YEAR = "1404",
        ADDUSERID = "creator",
        CREATEDDATE = OriginalDate,
        ISDELETED = isDeleted,
    };

    private sealed record Harness(
        Mock<IVoucherHeadRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork,
        ChangeVoucherStateCommandHandler Handler);

    private static Harness CreateHarness(IEnumerable<TB_VOUCHERSHEAD> stored)
    {
        var repository = new Mock<IVoucherHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("mover");

        var rows = stored.ToList();

        repository
            .Setup(r => r.GetManyForUpdateAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                rows.Where(h => ids.Contains(h.ID)).ToList());

        return new Harness(
            repository,
            unitOfWork,
            new ChangeVoucherStateCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object));
    }

    [Fact]
    public async Task Handle_MovesEverySelectedVoucher()
    {
        var first = Head(Guid.NewGuid());
        var second = Head(Guid.NewGuid());
        var harness = CreateHarness(new[] { first, second });

        await harness.Handler.Handle(
            new ChangeVoucherStateCommand(new[] { first.ID, second.ID }, DocLife.Temporary),
            CancellationToken.None);

        Assert.Equal(DocLife.Temporary, first.DOCLIFE);
        Assert.Equal(DocLife.Temporary, second.DOCLIFE);
    }

    [Fact]
    public async Task Handle_StampsTheAuditTrail()
    {
        var head = Head(Guid.NewGuid());
        var harness = CreateHarness(new[] { head });

        await harness.Handler.Handle(
            new ChangeVoucherStateCommand(new[] { head.ID }, DocLife.Reviewed),
            CancellationToken.None);

        Assert.Equal("mover", head.CHANGEUSERID);
        Assert.NotNull(head.UPDATEDDATE);
        Assert.Equal(OriginalDate, head.CREATEDDATE);
        Assert.Equal("creator", head.ADDUSERID);
    }

    /// <summary>
    /// A stale selection must not move half the batch — the کارتابل would then show some rows in
    /// the new tab and some in the old with nothing explaining which.
    /// </summary>
    [Fact]
    public async Task Handle_UnknownId_MovesNothingAndThrowsNotFound()
    {
        var known = Head(Guid.NewGuid());
        var harness = CreateHarness(new[] { known });

        await Assert.ThrowsAsync<NotFoundException>(() => harness.Handler.Handle(
            new ChangeVoucherStateCommand(new[] { known.ID, Guid.NewGuid() }, DocLife.Accepted),
            CancellationToken.None));

        Assert.Equal(DocLife.Draft, known.DOCLIFE);
        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeletedVoucherInBatch_IsTreatedAsMissing()
    {
        var alive = Head(Guid.NewGuid());
        var deleted = Head(Guid.NewGuid(), isDeleted: true);
        var harness = CreateHarness(new[] { alive, deleted });

        await Assert.ThrowsAsync<NotFoundException>(() => harness.Handler.Handle(
            new ChangeVoucherStateCommand(new[] { alive.ID, deleted.ID }, DocLife.Temporary),
            CancellationToken.None));

        Assert.Equal(DocLife.Draft, alive.DOCLIFE);
    }

    /// <summary>
    /// ISDELETED is nullable on this table, and null means "not deleted" everywhere else in the
    /// project — so a null must not be mistaken for a deleted row and rejected.
    /// </summary>
    [Fact]
    public async Task Handle_NullIsDeleted_CountsAsAlive()
    {
        var head = Head(Guid.NewGuid(), isDeleted: null);
        var harness = CreateHarness(new[] { head });

        await harness.Handler.Handle(
            new ChangeVoucherStateCommand(new[] { head.ID }, DocLife.Accepted),
            CancellationToken.None);

        Assert.Equal(DocLife.Accepted, head.DOCLIFE);
    }

    [Fact]
    public async Task Handle_SavesOnce_ForTheWholeBatch()
    {
        var first = Head(Guid.NewGuid());
        var second = Head(Guid.NewGuid());
        var harness = CreateHarness(new[] { first, second });

        await harness.Handler.Handle(
            new ChangeVoucherStateCommand(new[] { first.ID, second.ID }, DocLife.Temporary),
            CancellationToken.None);

        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// This test used to be <c>Handle_AcceptedBackToDraft_IsCurrentlyAllowed</c>, pinning the
    /// deliberate absence of a transition guard and saying that if a rule were ever introduced,
    /// it should be the test that fails and forces the decision to be explicit. That is exactly
    /// what happened: تأیید دائم became terminal by project-owner decision (2026-09-22), so the
    /// assertion is inverted rather than deleted.
    ///
    /// The broader rule and its batch behaviour live in <c>AcceptedIsTerminalTests</c>.
    /// </summary>
    [Fact]
    public async Task Handle_AcceptedBackToDraft_IsRefused()
    {
        var head = Head(Guid.NewGuid(), DocLife.Accepted);
        var harness = CreateHarness(new[] { head });

        await Assert.ThrowsAsync<VoucherStateChangeDeniedException>(
            () => harness.Handler.Handle(
                new ChangeVoucherStateCommand(new[] { head.ID }, DocLife.Draft),
                CancellationToken.None));

        Assert.Equal(DocLife.Accepted, head.DOCLIFE);
    }
}
