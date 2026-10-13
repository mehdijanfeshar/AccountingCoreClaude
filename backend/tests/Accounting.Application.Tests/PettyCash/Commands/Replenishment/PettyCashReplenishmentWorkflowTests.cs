using Accounting.Application.Commands.PettyCash.ApprovePettyCashReplenishment;
using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.PettyCash.RejectPettyCashReplenishment;
using Accounting.Application.Commands.PettyCash.SubmitPettyCashReplenishment;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.Replenishment;

/// <summary>گردش کار ترمیم تنخواه: ارسال، تأیید مدیر مالی، رد.</summary>
public class PettyCashReplenishmentWorkflowTests
{
    private readonly Mock<IPettyCashReplenishmentRepository> _repl = new();
    private readonly Mock<IChargeAndCostRepository> _charge = new();
    private readonly Mock<IPettyCashReplenishmentAuthorizer> _auth = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Guid _id = Guid.NewGuid();

    public PettyCashReplenishmentWorkflowTests() => _user.SetupGet(u => u.UserId).Returns("approver");

    private TB_PC_REPLENISHMENT SetupRepl(PettyCashReplenishmentState state, string creator = "creator", bool deleted = false)
    {
        var r = new TB_PC_REPLENISHMENT
        {
            ID = _id, FUND_ID = Guid.NewGuid(), CHARGEANDCOSTHEAD_ID = Guid.NewGuid(),
            STATE = state, ADDUSERID = creator, ISDELETED = deleted, NOTE = "اولیه",
        };
        _repl.Setup(x => x.GetForUpdateAsync(_id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(r);
        return r;
    }

    private TB_CHARGEANDCOST_HEAD SetupHead(TB_PC_REPLENISHMENT r)
    {
        var head = new TB_CHARGEANDCOST_HEAD();
        _charge.Setup(c => c.GetHeadForUpdateAsync(r.CHARGEANDCOSTHEAD_ID, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(head);
        return head;
    }

    private void SetupNotFound()
        => _repl.Setup(x => x.GetForUpdateAsync(_id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((TB_PC_REPLENISHMENT?)null);

    private void AuthDenies()
        => _auth.Setup(a => a.EnsureHasRoleAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<PettyCashRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashReplenishmentRoleRequiredException(Guid.NewGuid(), new[] { PettyCashRole.FinanceManager }));

    private SubmitPettyCashReplenishmentCommandHandler Submit() => new(_repl.Object, _charge.Object, _auth.Object, _uow.Object, _user.Object);
    private ApprovePettyCashReplenishmentCommandHandler Approve() => new(_repl.Object, _charge.Object, _auth.Object, _uow.Object, _user.Object);
    private RejectPettyCashReplenishmentCommandHandler Reject() => new(_repl.Object, _charge.Object, _auth.Object, _uow.Object, _user.Object);

    // ---------- Submit ----------

    [Fact]
    public async Task Submit_NotFound_Throws()
    {
        SetupNotFound();
        await Assert.ThrowsAsync<NotFoundException>(() => Submit().Handle(new SubmitPettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default));
    }

    [Fact]
    public async Task Submit_Deleted_ThrowsNotFound()
    {
        SetupRepl(PettyCashReplenishmentState.Draft, deleted: true);
        await Assert.ThrowsAsync<NotFoundException>(() => Submit().Handle(new SubmitPettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default));
    }

    [Theory]
    [InlineData(PettyCashReplenishmentState.PendingFinanceManager)]
    [InlineData(PettyCashReplenishmentState.PendingTreasurer)]
    [InlineData(PettyCashReplenishmentState.Paid)]
    [InlineData(PettyCashReplenishmentState.Rejected)]
    public async Task Submit_NotDraft_ThrowsStateConflict(PettyCashReplenishmentState state)
    {
        SetupRepl(state);
        await Assert.ThrowsAsync<PettyCashReplenishmentStateConflictException>(
            () => Submit().Handle(new SubmitPettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default));
    }

    [Fact]
    public async Task Submit_NoRole_Propagates()
    {
        SetupRepl(PettyCashReplenishmentState.Draft);
        AuthDenies();
        await Assert.ThrowsAsync<PettyCashReplenishmentRoleRequiredException>(
            () => Submit().Handle(new SubmitPettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_Valid_MovesToPendingFinanceManagerAndKeepsHeadTemporary()
    {
        var r = SetupRepl(PettyCashReplenishmentState.Draft);
        var head = SetupHead(r);

        await Submit().Handle(new SubmitPettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default);

        Assert.Equal(PettyCashReplenishmentState.PendingFinanceManager, r.STATE);
        Assert.Equal(ChargeAndCostStatus.Temporary, head.STATUS);
        Assert.Equal("approver", r.CHANGEUSERID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Approve ----------

    [Fact]
    public async Task Approve_NotFound_Throws()
    {
        SetupNotFound();
        await Assert.ThrowsAsync<NotFoundException>(() => Approve().Handle(new ApprovePettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default));
    }

    [Theory]
    [InlineData(PettyCashReplenishmentState.Draft)]
    [InlineData(PettyCashReplenishmentState.PendingTreasurer)]
    [InlineData(PettyCashReplenishmentState.Paid)]
    public async Task Approve_NotPendingFinanceManager_ThrowsStateConflict(PettyCashReplenishmentState state)
    {
        SetupRepl(state);
        await Assert.ThrowsAsync<PettyCashReplenishmentStateConflictException>(
            () => Approve().Handle(new ApprovePettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default));
    }

    [Fact]
    public async Task Approve_RequiresFinanceManagerRoleOnly()
    {
        var r = SetupRepl(PettyCashReplenishmentState.PendingFinanceManager);
        SetupHead(r);

        await Approve().Handle(new ApprovePettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default);

        _auth.Verify(a => a.EnsureHasRoleAsync(r.FUND_ID,
            It.Is<IReadOnlyCollection<PettyCashRole>>(x => x.SequenceEqual(new[] { PettyCashRole.FinanceManager })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_NoRole_Propagates()
    {
        SetupRepl(PettyCashReplenishmentState.PendingFinanceManager);
        AuthDenies();
        await Assert.ThrowsAsync<PettyCashReplenishmentRoleRequiredException>(
            () => Approve().Handle(new ApprovePettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default));
    }

    [Fact]
    public async Task Approve_CreatorApprovesOwn_ThrowsApproverConflict()
    {
        SetupRepl(PettyCashReplenishmentState.PendingFinanceManager, creator: "approver");
        await Assert.ThrowsAsync<PettyCashReplenishmentApproverConflictException>(
            () => Approve().Handle(new ApprovePettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Approve_Valid_StampsApproverAndMovesToPendingTreasurer()
    {
        var r = SetupRepl(PettyCashReplenishmentState.PendingFinanceManager);
        var head = SetupHead(r);

        await Approve().Handle(new ApprovePettyCashReplenishmentCommand(_id) { VahedCode = "0101" }, default);

        Assert.Equal(PettyCashReplenishmentState.PendingTreasurer, r.STATE);
        Assert.Equal("approver", r.APPROVED_BY_USERID);
        Assert.Equal(ChargeAndCostStatus.Temporary, head.STATUS);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Reject ----------

    [Fact]
    public async Task Reject_NotFound_Throws()
    {
        SetupNotFound();
        await Assert.ThrowsAsync<NotFoundException>(() => Reject().Handle(new RejectPettyCashReplenishmentCommand(_id, "x") { VahedCode = "0101" }, default));
    }

    [Theory]
    [InlineData(PettyCashReplenishmentState.Draft)]
    [InlineData(PettyCashReplenishmentState.Paid)]
    [InlineData(PettyCashReplenishmentState.Rejected)]
    public async Task Reject_NotPending_ThrowsStateConflict(PettyCashReplenishmentState state)
    {
        SetupRepl(state);
        await Assert.ThrowsAsync<PettyCashReplenishmentStateConflictException>(
            () => Reject().Handle(new RejectPettyCashReplenishmentCommand(_id, "x") { VahedCode = "0101" }, default));
    }

    [Theory]
    [InlineData(PettyCashReplenishmentState.PendingFinanceManager)]
    [InlineData(PettyCashReplenishmentState.PendingTreasurer)]
    public async Task Reject_Pending_RejectsSoftDeletesLinksAndAppendsNote(PettyCashReplenishmentState state)
    {
        var r = SetupRepl(state);
        var head = SetupHead(r);
        var links = new List<TB_CHARGE_LINK_COST> { new(), new() };
        _charge.Setup(c => c.GetActiveLinksByChargeIdAsync(r.CHARGEANDCOSTHEAD_ID, It.IsAny<CancellationToken>())).ReturnsAsync(links);

        await Reject().Handle(new RejectPettyCashReplenishmentCommand(_id, "مبلغ اشتباه") { VahedCode = "0101" }, default);

        Assert.Equal(PettyCashReplenishmentState.Rejected, r.STATE);
        Assert.Equal("اولیه | رد: مبلغ اشتباه", r.NOTE);
        Assert.All(links, l => Assert.True(l.ISDELETED));
        Assert.Equal(ChargeAndCostStatus.Temporary, head.STATUS);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_BlankNote_KeepsOriginalNote()
    {
        var r = SetupRepl(PettyCashReplenishmentState.PendingTreasurer);
        _charge.Setup(c => c.GetActiveLinksByChargeIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_CHARGE_LINK_COST>());

        await Reject().Handle(new RejectPettyCashReplenishmentCommand(_id, "  ") { VahedCode = "0101" }, default);

        Assert.Equal("اولیه", r.NOTE);
    }

    [Fact]
    public async Task Reject_EmptyOriginalNote_UsesRejectNoteAlone()
    {
        var r = SetupRepl(PettyCashReplenishmentState.PendingTreasurer);
        r.NOTE = null;
        _charge.Setup(c => c.GetActiveLinksByChargeIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_CHARGE_LINK_COST>());

        await Reject().Handle(new RejectPettyCashReplenishmentCommand(_id, "دلیل") { VahedCode = "0101" }, default);

        Assert.Equal("دلیل", r.NOTE);
    }

    [Fact]
    public async Task Reject_NoRole_Propagates()
    {
        SetupRepl(PettyCashReplenishmentState.PendingTreasurer);
        AuthDenies();
        await Assert.ThrowsAsync<PettyCashReplenishmentRoleRequiredException>(
            () => Reject().Handle(new RejectPettyCashReplenishmentCommand(_id, "x") { VahedCode = "0101" }, default));
    }

    [Fact]
    public void RejectValidator_NoteTooLong_Fails()
        => new RejectPettyCashReplenishmentCommandValidator()
            .TestValidate(new RejectPettyCashReplenishmentCommand(_id, new string('a', 1001)) { VahedCode = "0101" })
            .ShouldHaveValidationErrorFor(x => x.Note);

    [Fact]
    public void RejectValidator_NullNote_Passes()
        => new RejectPettyCashReplenishmentCommandValidator()
            .TestValidate(new RejectPettyCashReplenishmentCommand(_id, null) { VahedCode = "0101" })
            .ShouldNotHaveAnyValidationErrors();
}
