using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Common.ReviewTransition;

public class PettyCashReviewTransitionServiceTests
{
    private readonly Mock<IPettyCashExpenseDocRepository> _docs = new();
    private readonly Mock<IChargeAndCostRepository> _heads = new();
    private readonly Mock<IPettyCashDocEventRepository> _events = new();
    private readonly Mock<IPettyCashReviewAuthorizer> _auth = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();
    private readonly Guid _docId = Guid.NewGuid();

    private PettyCashReviewTransitionService Sut()
    {
        _user.SetupGet(u => u.UserId).Returns("inspector");
        _client.SetupGet(c => c.ClientIp).Returns("1.2.3.4");
        return new(_docs.Object, _heads.Object, _events.Object, _auth.Object, _user.Object, _client.Object);
    }

    private TB_PC_EXPENSE_DOC Doc(PettyCashDocState state, bool deleted = false) => new()
    {
        ID = _docId, FUND_ID = Guid.NewGuid(), CHARGEANDCOSTHEAD_ID = Guid.NewGuid(),
        DOC_STATE = state, ADDUSERID = "creator", YEAR = "1404", ISDELETED = deleted,
    };

    private void Setup(TB_PC_EXPENSE_DOC? doc)
        => _docs.Setup(r => r.GetForUpdateAsync(_docId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(doc);

    private Task<TB_PC_EXPENSE_DOC> Run(PettyCashDocState from = PettyCashDocState.New,
        PettyCashDocState to = PettyCashDocState.PendingReview,
        PettyCashDocAction action = PettyCashDocAction.StartReview, string? deadline = null)
        => Sut().TransitionAsync(_docId, "0101", from, to, action,
            new[] { PettyCashRole.Inspector }, "note", "1,2", deadline, default);

    [Fact]
    public async Task Handle_DocNotFound_ThrowsNotFound()
    {
        Setup(null);

        await Assert.ThrowsAsync<NotFoundException>(() => Run());
    }

    [Fact]
    public async Task Handle_DocSoftDeleted_ThrowsNotFound()
    {
        Setup(Doc(PettyCashDocState.New, deleted: true));

        await Assert.ThrowsAsync<NotFoundException>(() => Run());
    }

    [Fact]
    public async Task Handle_WrongState_ThrowsStateConflictAndWritesNoEvent()
    {
        Setup(Doc(PettyCashDocState.Draft));

        var ex = await Assert.ThrowsAsync<PettyCashReviewStateConflictException>(() => Run());

        Assert.Equal(PettyCashDocState.Draft, ex.ActualState);
        _events.Verify(e => e.AddAsync(It.IsAny<TB_PC_DOC_EVENT>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AuthorizerDenies_PropagatesBeforeStateCheck()
    {
        // کاربر بی‌صلاحیت نباید از وضعیت سند خبردار شود
        Setup(Doc(PettyCashDocState.Draft));
        _auth.Setup(a => a.EnsureCanReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<PettyCashRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashSelfReviewConflictException(_docId));

        await Assert.ThrowsAsync<PettyCashSelfReviewConflictException>(() => Run());
    }

    [Fact]
    public async Task Handle_Valid_ChangesStateSyncsHeadAndWritesEvent()
    {
        var doc = Doc(PettyCashDocState.New);
        var head = new TB_CHARGEANDCOST_HEAD();
        Setup(doc);
        _heads.Setup(r => r.GetHeadForUpdateAsync(doc.CHARGEANDCOSTHEAD_ID, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(head);
        TB_PC_DOC_EVENT? ev = null;
        _events.Setup(e => e.AddAsync(It.IsAny<TB_PC_DOC_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_DOC_EVENT, CancellationToken>((x, _) => ev = x).Returns(Task.CompletedTask);

        var result = await Run();

        Assert.Equal(PettyCashDocState.PendingReview, result.DOC_STATE);
        Assert.Equal("inspector", doc.CHANGEUSERID);
        Assert.Equal("inspector", head.CHANGEUSERID);
        Assert.NotNull(ev);
        Assert.Equal(PettyCashDocState.New, ev!.FROM_STATE);
        Assert.Equal(PettyCashDocState.PendingReview, ev.TO_STATE);
        Assert.Equal(PettyCashDocAction.StartReview, ev.ACTION);
        Assert.Equal("1.2.3.4", ev.CLIENT_IP);
        Assert.Equal("0101", ev.VAHEDCODE);
        Assert.Equal("1404", ev.YEAR);
        Assert.Equal("1,2", ev.RETURN_REASONS);
    }

    [Fact]
    public async Task Handle_ReturnAction_StoresDeadline()
    {
        var doc = Doc(PettyCashDocState.PendingReview);
        Setup(doc);

        await Run(PettyCashDocState.PendingReview, PettyCashDocState.Returned, PettyCashDocAction.Return, "14041201");

        Assert.Equal("14041201", doc.RETURN_DEADLINE);
        Assert.Equal(PettyCashDocState.Returned, doc.DOC_STATE);
    }

    [Fact]
    public async Task Handle_NonReturnAction_DoesNotTouchDeadline()
    {
        var doc = Doc(PettyCashDocState.New);
        Setup(doc);

        await Run(deadline: "14041201");

        Assert.Null(doc.RETURN_DEADLINE);
    }

    [Fact]
    public async Task Handle_HeadMissing_StillTransitions()
    {
        Setup(Doc(PettyCashDocState.New));
        _heads.Setup(r => r.GetHeadForUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TB_CHARGEANDCOST_HEAD?)null);

        var result = await Run();

        Assert.Equal(PettyCashDocState.PendingReview, result.DOC_STATE);
    }
}
