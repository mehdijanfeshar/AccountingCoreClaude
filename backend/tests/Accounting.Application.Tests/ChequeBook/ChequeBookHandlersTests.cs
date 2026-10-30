using Accounting.Application.ChequeBook;
using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.ChequeBook;

/// <summary>
/// کارتابل دفتر چک (فاز ۴۷): دستور پرداخت ⇐ رئیس حسابداری ⇐ مدیر واحد ⇐ چاپ؛ تفکیک وظایف،
/// برگشت با علت، و قفل چاپ/ابطال.
/// </summary>
public class ChequeBookHandlersTests
{
    private const string Vahed = "0042";
    private const string Preparer = "1111111111";
    private const string Accountant = "2222222222";
    private const string Manager = "3333333333";

    private readonly Mock<IChequeBookRepository> _repo = new();
    private readonly Mock<ITreasuryRoleAuthorizer> _roles = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly List<TB_CHECK_APPROVAL_EVENT> _events = new();
    private readonly List<TB_CHECK_APPROVAL> _addedApprovals = new();

    public ChequeBookHandlersTests()
    {
        _user.SetupGet(u => u.UserId).Returns(Preparer);
        _repo.Setup(r => r.AddApprovalEventAsync(It.IsAny<TB_CHECK_APPROVAL_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHECK_APPROVAL_EVENT, CancellationToken>((e, _) => _events.Add(e));
        _repo.Setup(r => r.AddApprovalAsync(It.IsAny<TB_CHECK_APPROVAL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHECK_APPROVAL, CancellationToken>((a, _) => _addedApprovals.Add(a));
        _roles.Setup(r => r.EnsureHasRoleAsync(Vahed, It.IsAny<IReadOnlyCollection<TreasuryRole>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IReadOnlyCollection<TreasuryRole> allowed, CancellationToken _) => allowed);
    }

    private ChequeBookHandlers Handlers() => new(_repo.Object, _roles.Object, _uow.Object, _user.Object);

    private void ActAs(string userId) => _user.SetupGet(u => u.UserId).Returns(userId);

    private TB_CHECK Cheque(
        CheckCancelStatus cancel = CheckCancelStatus.NotCanceled,
        CheckPrintStatus print = CheckPrintStatus.None,
        string? payTo = "شرکت الف",
        string? date = "14040510",
        TB_CHECK_APPROVAL? approval = null,
        bool usedInVoucher = true)
    {
        var c = new TB_CHECK { ID = Guid.NewGuid(), CHEQ_NO = "123456", EBTAL = cancel, PRINT = print, PAYTO = payTo, CHEQ_DATE = date };
        _repo.Setup(r => r.GetCheckForUpdateAsync(c.ID, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(c);
        _repo.Setup(r => r.GetApprovalForUpdateAsync(c.ID, It.IsAny<CancellationToken>())).ReturnsAsync(approval);
        _repo.Setup(r => r.GetActiveDetailAsync(c.ID, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usedInVoucher ? new TB_VOUCHERSDETAIL { ID = Guid.NewGuid(), YEAR = "1404" } : null);
        return c;
    }

    private static TB_CHECK_APPROVAL Approval(ChequeApprovalState state, string preparedBy = Preparer, string? accountingBy = null) => new()
    {
        ID = Guid.NewGuid(),
        STATE = state,
        PREPARED_BY = preparedBy,
        ACCOUNTING_BY = accountingBy,
        ISDELETED = false,
    };

    private static ChequeApprovalCommand Cmd(ChequeApprovalAction action, string? note, params Guid[] ids)
        => new(action, ids, note) { VahedCode = Vahed };

    [Fact]
    public async Task Issue_creates_pending_accounting_approval_and_event()
    {
        var c = Cheque();

        var done = await Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, " یادداشت ", c.ID), default);

        Assert.Equal(1, done);
        var a = Assert.Single(_addedApprovals);
        Assert.Equal(ChequeApprovalState.PendingAccounting, a.STATE);
        Assert.Equal(Preparer, a.PREPARED_BY);
        Assert.Equal("1404", a.YEAR);
        Assert.Equal("یادداشت", a.NOTE);
        var e = Assert.Single(_events);
        Assert.Null(e.FROM_STATE);
        Assert.Equal(ChequeApprovalState.PendingAccounting, e.TO_STATE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Issue_dedupes_ids()
    {
        var c = Cheque();
        var done = await Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, null, c.ID, c.ID), default);
        Assert.Equal(1, done);
    }

    [Fact]
    public async Task Issue_twice_conflicts()
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.PendingAccounting));
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, null, c.ID), default));
    }

    [Fact]
    public async Task Issue_after_return_reuses_approval_and_clears_signatures()
    {
        var a = Approval(ChequeApprovalState.Returned, accountingBy: Accountant);
        a.MANAGER_BY = Manager;
        var c = Cheque(approval: a);

        await Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, null, c.ID), default);

        Assert.Empty(_addedApprovals);
        Assert.Equal(ChequeApprovalState.PendingAccounting, a.STATE);
        Assert.Null(a.ACCOUNTING_BY);
        Assert.Null(a.MANAGER_BY);
        Assert.Equal(ChequeApprovalState.Returned, Assert.Single(_events).FROM_STATE);
    }

    [Fact]
    public async Task Issue_on_soft_deleted_approval_starts_fresh()
    {
        var old = Approval(ChequeApprovalState.Confirmed);
        old.ISDELETED = true;
        var c = Cheque(approval: old);

        await Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, null, c.ID), default);

        Assert.Single(_addedApprovals);
    }

    [Fact]
    public async Task Issue_canceled_cheque_conflicts()
    {
        var c = Cheque(cancel: CheckCancelStatus.Canceled);
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, null, c.ID), default));
    }

    [Fact]
    public async Task Issue_cheque_not_in_voucher_conflicts()
    {
        var c = Cheque(usedInVoucher: false);
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, null, c.ID), default));
    }

    [Theory]
    [InlineData(null, "14040510")]
    [InlineData("x", " ")]
    public async Task Issue_without_payee_or_date_conflicts(string? payTo, string? date)
    {
        var c = Cheque(payTo: payTo, date: date);
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, null, c.ID), default));
    }

    [Fact]
    public async Task Missing_cheque_is_not_found()
        => await Assert.ThrowsAsync<NotFoundException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, null, Guid.NewGuid()), default));

    [Fact]
    public async Task Approve_without_order_conflicts()
    {
        var c = Cheque();
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.ApproveAccounting, null, c.ID), default));
    }

    [Fact]
    public async Task Accounting_approval_requires_senior_accountant_role()
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.PendingAccounting));
        ActAs(Accountant);
        _roles.Setup(r => r.EnsureHasRoleAsync(Vahed, It.Is<IReadOnlyCollection<TreasuryRole>>(x => x.Contains(TreasuryRole.SeniorAccountant)), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryRoleRequiredException(Vahed, TreasuryRole.SeniorAccountant));

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.ApproveAccounting, null, c.ID), default));
    }

    [Fact]
    public async Task Preparer_cannot_approve_accounting()
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.PendingAccounting));
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.ApproveAccounting, null, c.ID), default));
    }

    [Fact]
    public async Task Accounting_approval_moves_to_pending_manager()
    {
        var a = Approval(ChequeApprovalState.PendingAccounting);
        var c = Cheque(approval: a);
        ActAs(Accountant);

        await Handlers().Handle(Cmd(ChequeApprovalAction.ApproveAccounting, null, c.ID), default);

        Assert.Equal(ChequeApprovalState.PendingManager, a.STATE);
        Assert.Equal(Accountant, a.ACCOUNTING_BY);
        _roles.Verify(r => r.EnsureHasRoleAsync(Vahed, It.Is<IReadOnlyCollection<TreasuryRole>>(x => x.Single() == TreasuryRole.SeniorAccountant), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Approve_action_is_recorded_by_actual_step_not_requested_button()
    {
        // دکمهٔ «تأیید مدیر» روی چکی که هنوز در انتظار حسابداری است ⇒ گام حسابداری انجام و همان ثبت می‌شود.
        var a = Approval(ChequeApprovalState.PendingAccounting);
        var c = Cheque(approval: a);
        ActAs(Accountant);

        await Handlers().Handle(Cmd(ChequeApprovalAction.ApproveManager, null, c.ID), default);

        Assert.Equal(ChequeApprovalState.PendingManager, a.STATE);
        Assert.Equal(ChequeApprovalAction.ApproveAccounting, Assert.Single(_events).ACTION);
    }

    [Theory]
    [InlineData(Preparer)]
    [InlineData(Accountant)]
    public async Task Manager_must_differ_from_previous_signers(string userId)
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.PendingManager, accountingBy: Accountant));
        ActAs(userId);
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.ApproveManager, null, c.ID), default));
    }

    [Fact]
    public async Task Manager_approval_confirms()
    {
        var a = Approval(ChequeApprovalState.PendingManager, accountingBy: Accountant);
        var c = Cheque(approval: a);
        ActAs(Manager);

        await Handlers().Handle(Cmd(ChequeApprovalAction.ApproveAccounting, null, c.ID), default);

        Assert.Equal(ChequeApprovalState.Confirmed, a.STATE);
        Assert.Equal(Manager, a.MANAGER_BY);
        Assert.Equal(ChequeApprovalAction.ApproveManager, Assert.Single(_events).ACTION);
        _roles.Verify(r => r.EnsureHasRoleAsync(Vahed, It.Is<IReadOnlyCollection<TreasuryRole>>(x => x.Single() == TreasuryRole.UnitManager), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Approve_confirmed_conflicts()
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.Confirmed));
        ActAs(Manager);
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.ApproveManager, null, c.ID), default));
    }

    [Fact]
    public async Task Return_requires_note()
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.PendingManager));
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.Return, "  ", c.ID), default));
    }

    [Fact]
    public async Task Return_printed_cheque_conflicts()
    {
        var c = Cheque(print: CheckPrintStatus.Printed, approval: Approval(ChequeApprovalState.Confirmed));
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.Return, "علت", c.ID), default));
    }

    [Fact]
    public async Task Return_sets_returned_with_note()
    {
        var a = Approval(ChequeApprovalState.Confirmed);
        var c = Cheque(approval: a);
        ActAs(Manager);

        await Handlers().Handle(Cmd(ChequeApprovalAction.Return, " مبلغ اشتباه ", c.ID), default);

        Assert.Equal(ChequeApprovalState.Returned, a.STATE);
        Assert.Equal("مبلغ اشتباه", a.NOTE);
    }

    [Fact]
    public async Task Return_not_in_cartable_conflicts()
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.Returned));
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.Return, "x", c.ID), default));
    }

    [Fact]
    public async Task Failure_in_batch_saves_nothing()
    {
        var ok = Cheque();
        var bad = Cheque(cancel: CheckCancelStatus.Canceled);
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(Cmd(ChequeApprovalAction.IssuePaymentOrder, null, ok.ID, bad.ID), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Print_requires_confirmed_approval()
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.PendingManager));
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(new MarkChequePrintedCommand(c.ID) { VahedCode = Vahed }, default));
        Assert.Equal(CheckPrintStatus.None, c.PRINT);
    }

    [Fact]
    public async Task Print_canceled_cheque_conflicts()
    {
        var c = Cheque(cancel: CheckCancelStatus.Canceled, approval: Approval(ChequeApprovalState.Confirmed));
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(new GetChequePrintQuery(c.ID) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Mark_printed_after_confirmation()
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.Confirmed));
        await Handlers().Handle(new MarkChequePrintedCommand(c.ID) { VahedCode = Vahed }, default);
        Assert.Equal(CheckPrintStatus.Printed, c.PRINT);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Print_data_missing_conflicts()
    {
        var c = Cheque(approval: Approval(ChequeApprovalState.Confirmed));
        await Assert.ThrowsAsync<ChequeConflictException>(() =>
            Handlers().Handle(new GetChequePrintQuery(c.ID) { VahedCode = Vahed }, default));
    }

    [Theory]
    [InlineData(true, CheckCancelStatus.Canceled)]
    [InlineData(false, CheckCancelStatus.NotCanceled)]
    public async Task SetCanceled_toggles(bool canceled, CheckCancelStatus expected)
    {
        var c = Cheque(cancel: canceled ? CheckCancelStatus.NotCanceled : CheckCancelStatus.Canceled);
        await Handlers().Handle(new SetChequeCanceledCommand(c.ID, canceled) { VahedCode = Vahed }, default);
        Assert.Equal(expected, c.EBTAL);
    }

    [Fact]
    public async Task Events_of_other_unit_cheque_are_not_found()
        => await Assert.ThrowsAsync<NotFoundException>(() =>
            Handlers().Handle(new GetChequeApprovalEventsQuery(Guid.NewGuid()) { VahedCode = Vahed }, default));

    [Fact]
    public void Validator_rejects_empty_and_oversized_batches()
    {
        var v = new ChequeApprovalCommandValidator();
        Assert.False(v.Validate(new ChequeApprovalCommand(ChequeApprovalAction.Return, Array.Empty<Guid>(), null)).IsValid);
        Assert.False(v.Validate(new ChequeApprovalCommand(ChequeApprovalAction.Return,
            Enumerable.Range(0, 201).Select(_ => Guid.NewGuid()).ToList(), null)).IsValid);
        Assert.True(v.Validate(new ChequeApprovalCommand(ChequeApprovalAction.Return, new[] { Guid.NewGuid() }, null)).IsValid);
    }
}
