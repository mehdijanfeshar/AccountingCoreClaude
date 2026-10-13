using Accounting.Application.Commands.PettyCash.ApprovePettyCashExpenseDoc;
using Accounting.Application.Commands.PettyCash.BulkApprovePettyCashExpenseDocs;
using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.PettyCash.RejectPettyCashExpenseDoc;
using Accounting.Application.Commands.PettyCash.ReturnPettyCashExpenseDoc;
using Accounting.Application.Commands.PettyCash.StartReviewPettyCashExpenseDoc;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.Review;

public class ReviewDelegatingHandlersTests
{
    private readonly Mock<IPettyCashReviewTransitionService> _transition = new();
    private readonly Mock<IPettyCashFinalApprovalService> _approval = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _id = Guid.NewGuid();

    private void TransitionReturns(TB_PC_EXPENSE_DOC doc)
        => _transition.Setup(t => t.TransitionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<PettyCashDocState>(),
                It.IsAny<PettyCashDocState>(), It.IsAny<PettyCashDocAction>(), It.IsAny<IReadOnlyCollection<PettyCashRole>>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

    [Fact]
    public async Task StartReview_TransitionsNewToPendingReviewForInspectorOnly()
    {
        TransitionReturns(new TB_PC_EXPENSE_DOC());
        var cmd = new StartReviewPettyCashExpenseDocCommand(_id) { VahedCode = "0101" };

        await new StartReviewPettyCashExpenseDocCommandHandler(_transition.Object, _uow.Object).Handle(cmd, default);

        _transition.Verify(t => t.TransitionAsync(_id, "0101", PettyCashDocState.New, PettyCashDocState.PendingReview,
            PettyCashDocAction.StartReview,
            It.Is<IReadOnlyCollection<PettyCashRole>>(r => r.SequenceEqual(new[] { PettyCashRole.Inspector })),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartReview_TransitionThrows_DoesNotSave()
    {
        _transition.Setup(t => t.TransitionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<PettyCashDocState>(),
                It.IsAny<PettyCashDocState>(), It.IsAny<PettyCashDocAction>(), It.IsAny<IReadOnlyCollection<PettyCashRole>>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("PettyCashExpenseDoc", _id));
        var cmd = new StartReviewPettyCashExpenseDocCommand(_id) { VahedCode = "0101" };

        await Assert.ThrowsAsync<NotFoundException>(
            () => new StartReviewPettyCashExpenseDocCommandHandler(_transition.Object, _uow.Object).Handle(cmd, default));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reject_TransitionsPendingReviewToRejectedWithNote()
    {
        TransitionReturns(new TB_PC_EXPENSE_DOC());
        var cmd = new RejectPettyCashExpenseDocCommand(_id, "دلیل") { VahedCode = "0101" };

        await new RejectPettyCashExpenseDocCommandHandler(_transition.Object, _uow.Object).Handle(cmd, default);

        _transition.Verify(t => t.TransitionAsync(_id, "0101", PettyCashDocState.PendingReview, PettyCashDocState.Rejected,
            PettyCashDocAction.Reject,
            It.Is<IReadOnlyCollection<PettyCashRole>>(r => r.Count == 3 && !r.Contains(PettyCashRole.Treasurer)),
            "دلیل", null, null, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Return_JoinsReasonsClearsVerificationAndSaves()
    {
        var doc = new TB_PC_EXPENSE_DOC { VERIFIED_BY_USERID = "insp", VERIFIED_DATE = DateTime.UtcNow };
        TransitionReturns(doc);
        var cmd = new ReturnPettyCashExpenseDocCommand(_id, new[] { 1, 3 }, "14091201", "توضیح") { VahedCode = "0101" };

        await new ReturnPettyCashExpenseDocCommandHandler(_transition.Object, _uow.Object).Handle(cmd, default);

        _transition.Verify(t => t.TransitionAsync(_id, "0101", PettyCashDocState.PendingReview, PettyCashDocState.Returned,
            PettyCashDocAction.Return, It.IsAny<IReadOnlyCollection<PettyCashRole>>(),
            "توضیح", "1,3", "14091201", It.IsAny<CancellationToken>()), Times.Once);
        // سند برگشتی باید از ابتدا دوباره کنترل شود
        Assert.Null(doc.VERIFIED_BY_USERID);
        Assert.Null(doc.VERIFIED_DATE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_DelegatesToFinalApprovalAndSaves()
    {
        var cmd = new ApprovePettyCashExpenseDocCommand(_id, "ok") { VahedCode = "0101" };

        await new ApprovePettyCashExpenseDocCommandHandler(_approval.Object, _uow.Object).Handle(cmd, default);

        _approval.Verify(a => a.FinalApproveAsync(_id, "0101", "ok", It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_ServiceThrows_DoesNotSave()
    {
        _approval.Setup(a => a.FinalApproveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashNotVerifiedException(_id));
        var cmd = new ApprovePettyCashExpenseDocCommand(_id, null) { VahedCode = "0101" };

        await Assert.ThrowsAsync<PettyCashNotVerifiedException>(
            () => new ApprovePettyCashExpenseDocCommandHandler(_approval.Object, _uow.Object).Handle(cmd, default));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private BulkApprovePettyCashExpenseDocsCommandHandler Bulk() => new(_approval.Object, _uow.Object);

    [Fact]
    public async Task BulkApprove_AllSucceed_DeduplicatesIdsAndSavesOnce()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var cmd = new BulkApprovePettyCashExpenseDocsCommand(new[] { a, b, a }) { VahedCode = "0101" };

        await Bulk().Handle(cmd, default);

        _approval.Verify(s => s.FinalApproveAsync(It.IsAny<Guid>(), "0101", null, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkApprove_SomeFail_ThrowsConflictWithMappedReasonsAndNeverSaves()
    {
        var ok = Guid.NewGuid();
        var notFound = Guid.NewGuid();
        var forbidden = Guid.NewGuid();
        var self = Guid.NewGuid();
        var verifier = Guid.NewGuid();
        var state = Guid.NewGuid();
        var notVerified = Guid.NewGuid();
        var over = Guid.NewGuid();
        var unit = Guid.NewGuid();

        void Fail(Guid id, Exception ex)
            => _approval.Setup(s => s.FinalApproveAsync(id, "0101", null, It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        Fail(notFound, new NotFoundException("PettyCashExpenseDoc", notFound));
        Fail(forbidden, new PettyCashReviewerAccessDeniedException(forbidden, Guid.NewGuid()));
        Fail(self, new PettyCashSelfReviewConflictException(self));
        Fail(verifier, new PettyCashVerifierCannotApproveException(verifier));
        Fail(state, new PettyCashReviewStateConflictException(state, PettyCashDocState.Draft, PettyCashDocState.PendingReview, PettyCashDocAction.FinalApprove));
        Fail(notVerified, new PettyCashNotVerifiedException(notVerified));
        Fail(over, new PettyCashApprovalAuthorityExceededException(over, 10, 5));
        Fail(unit, new UnitAccessDeniedException("Doc", Guid.NewGuid(), "0202", "0101"));

        var cmd = new BulkApprovePettyCashExpenseDocsCommand(
            new[] { ok, notFound, forbidden, self, verifier, state, notVerified, over, unit }) { VahedCode = "0101" };

        var ex = await Assert.ThrowsAsync<PettyCashBulkApproveConflictException>(() => Bulk().Handle(cmd, default));

        Assert.Equal(8, ex.Failures.Count);
        Assert.False(ex.Failures.ContainsKey(ok));
        Assert.Equal("not-found", ex.Failures[notFound]);
        Assert.Equal("forbidden", ex.Failures[forbidden]);
        Assert.Equal("forbidden", ex.Failures[unit]);
        Assert.Equal("self-review", ex.Failures[self]);
        Assert.Equal("self-review", ex.Failures[verifier]);
        Assert.Equal("invalid-state", ex.Failures[state]);
        Assert.Equal("not-verified", ex.Failures[notVerified]);
        Assert.Equal("over-authority", ex.Failures[over]);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkApprove_UnexpectedException_Propagates()
    {
        _approval.Setup(s => s.FinalApproveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        var cmd = new BulkApprovePettyCashExpenseDocsCommand(new[] { Guid.NewGuid() }) { VahedCode = "0101" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Bulk().Handle(cmd, default));
    }
}
