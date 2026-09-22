using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.ChangeVoucherState;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.ChangeVoucherState;

/// <summary>
/// تأیید دائم is terminal (project owner, 2026-09-22): a permanently-approved voucher cannot be
/// moved to any other state. With phase 38's editability rule this is what makes the lifecycle
/// genuinely final — such a voucher can no longer be edited, deleted, or walked back to a state
/// where it could be. معکوس سند is the only way to undo its effect.
/// </summary>
public sealed class AcceptedIsTerminalTests
{
    private static TB_VOUCHERSHEAD Head(Guid id, DocLife? docLife) =>
        new() { ID = id, DOCLIFE = docLife, ISDELETED = false };

    private static (ChangeVoucherStateCommandHandler Handler, Mock<IUnitOfWork> UnitOfWork)
        Build(params TB_VOUCHERSHEAD[] heads)
    {
        var repository = new Mock<IVoucherHeadRepository>();
        repository
            .Setup(r => r.GetManyForUpdateAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(heads);

        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("mover1");

        return (new ChangeVoucherStateCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object),
            unitOfWork);
    }

    [Theory]
    [InlineData(DocLife.Draft)]
    [InlineData(DocLife.Temporary)]
    [InlineData(DocLife.Reviewed)]
    public async Task An_accepted_voucher_cannot_be_moved_anywhere(DocLife target)
    {
        var id = Guid.NewGuid();
        var (handler, unitOfWork) = Build(Head(id, DocLife.Accepted));

        await Assert.ThrowsAsync<VoucherStateChangeDeniedException>(
            () => handler.Handle(new ChangeVoucherStateCommand([id], target), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task One_accepted_voucher_refuses_the_whole_batch()
    {
        // The command is all-or-nothing, so a terminal voucher in the selection must reject the
        // move rather than silently skipping that row and moving the rest.
        var movable = Guid.NewGuid();
        var terminal = Guid.NewGuid();
        var movableHead = Head(movable, DocLife.Draft);
        var (handler, unitOfWork) = Build(movableHead, Head(terminal, DocLife.Accepted));

        await Assert.ThrowsAsync<VoucherStateChangeDeniedException>(
            () => handler.Handle(
                new ChangeVoucherStateCommand([movable, terminal], DocLife.Temporary),
                CancellationToken.None));

        Assert.Equal(DocLife.Draft, movableHead.DOCLIFE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(DocLife.Draft)]
    [InlineData(DocLife.Temporary)]
    [InlineData(DocLife.Reviewed)]
    public async Task Every_other_state_can_still_be_moved_including_into_accepted(DocLife from)
    {
        var id = Guid.NewGuid();
        var head = Head(id, from);
        var (handler, unitOfWork) = Build(head);

        await handler.Handle(new ChangeVoucherStateCommand([id], DocLife.Accepted), CancellationToken.None);

        Assert.Equal(DocLife.Accepted, head.DOCLIFE);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
