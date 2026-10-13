using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.PettyCash.VerifyPettyCashExpenseDoc;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.Review;

public class VerifyPettyCashExpenseDocCommandHandlerTests
{
    private readonly Mock<IPettyCashExpenseDocRepository> _docs = new();
    private readonly Mock<IPettyCashDocEventRepository> _events = new();
    private readonly Mock<IPettyCashReviewAuthorizer> _auth = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();
    private readonly Guid _id = Guid.NewGuid();

    private VerifyPettyCashExpenseDocCommandHandler Sut()
    {
        _user.SetupGet(u => u.UserId).Returns("inspector");
        _client.SetupGet(c => c.ClientIp).Returns("5.5.5.5");
        return new(_docs.Object, _events.Object, _auth.Object, _uow.Object, _user.Object, _client.Object);
    }

    private TB_PC_EXPENSE_DOC SetupDoc(PettyCashDocState state = PettyCashDocState.PendingReview,
        string? verifier = null, bool deleted = false)
    {
        var doc = new TB_PC_EXPENSE_DOC
        {
            ID = _id, FUND_ID = Guid.NewGuid(), DOC_STATE = state, ADDUSERID = "creator",
            VERIFIED_BY_USERID = verifier, YEAR = "1404", ISDELETED = deleted,
        };
        _docs.Setup(r => r.GetForUpdateAsync(_id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(doc);
        return doc;
    }

    private static VerifyPettyCashExpenseDocCommand Cmd(Guid id) => new(id, "کنترل شد") { VahedCode = "0101" };

    [Fact]
    public async Task Handle_DocMissing_ThrowsNotFound()
    {
        _docs.Setup(r => r.GetForUpdateAsync(_id, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TB_PC_EXPENSE_DOC?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().Handle(Cmd(_id), default));
    }

    [Fact]
    public async Task Handle_DocDeleted_ThrowsNotFound()
    {
        SetupDoc(deleted: true);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().Handle(Cmd(_id), default));
    }

    [Fact]
    public async Task Handle_AuthorizerDenies_Propagates()
    {
        SetupDoc();
        _auth.Setup(a => a.EnsureCanReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<PettyCashRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashSelfReviewConflictException(_id));

        await Assert.ThrowsAsync<PettyCashSelfReviewConflictException>(() => Sut().Handle(Cmd(_id), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(PettyCashDocState.Draft)]
    [InlineData(PettyCashDocState.New)]
    [InlineData(PettyCashDocState.Approved)]
    public async Task Handle_StateNotPendingReview_ThrowsStateConflict(PettyCashDocState state)
    {
        SetupDoc(state);

        await Assert.ThrowsAsync<PettyCashReviewStateConflictException>(() => Sut().Handle(Cmd(_id), default));
    }

    [Fact]
    public async Task Handle_AlreadyVerified_ThrowsAlreadyVerified()
    {
        SetupDoc(verifier: "someone");

        await Assert.ThrowsAsync<PettyCashAlreadyVerifiedException>(() => Sut().Handle(Cmd(_id), default));
    }

    [Fact]
    public async Task Handle_Valid_StampsVerifierKeepsStateAndWritesEvent()
    {
        var doc = SetupDoc();
        TB_PC_DOC_EVENT? ev = null;
        _events.Setup(e => e.AddAsync(It.IsAny<TB_PC_DOC_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_DOC_EVENT, CancellationToken>((x, _) => ev = x).Returns(Task.CompletedTask);

        await Sut().Handle(Cmd(_id), default);

        Assert.Equal("inspector", doc.VERIFIED_BY_USERID);
        Assert.NotNull(doc.VERIFIED_DATE);
        Assert.Equal(PettyCashDocState.PendingReview, doc.DOC_STATE);
        Assert.Equal(PettyCashDocAction.Verify, ev!.ACTION);
        Assert.Equal(PettyCashDocState.PendingReview, ev.FROM_STATE);
        Assert.Equal(PettyCashDocState.PendingReview, ev.TO_STATE);
        Assert.Equal("0101", ev.VAHEDCODE);
        Assert.Equal("5.5.5.5", ev.CLIENT_IP);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Valid_OnlyInspectorRoleIsAllowed()
    {
        SetupDoc();

        await Sut().Handle(Cmd(_id), default);

        _auth.Verify(a => a.EnsureCanReviewAsync(_id, It.IsAny<Guid>(), "creator",
            It.Is<IReadOnlyCollection<PettyCashRole>>(r => r.SequenceEqual(new[] { PettyCashRole.Inspector })),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
