using Accounting.Application.Commands.Vouchers.ChangeVoucherState;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Entity;
using Accounting.Domain.OperationTemplates;
using Accounting.Domain.ValueObjects;
using MediatR;
using Moq;

namespace Accounting.Application.Tests.OperationTemplates;

/// <summary>فاز ۶۰–۶۲: تفکیک وظایف، ریسک بالا، تأیید/برگشت/پاسخ و کنترل‌های کارت صف بررسی.</summary>
public class ReviewWorkflowTests
{
    private const string Vahed = "0101";
    private readonly Mock<IHesabyarAuditRepository> _repo = new();
    private readonly Mock<IVoucherHeadRepository> _heads = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly List<OperationReview> _added = [];

    public ReviewWorkflowTests()
    {
        ActAs("accountant", AppRoles.MaliAdmin);
        _repo.Setup(r => r.AddReviewAsync(It.IsAny<OperationReview>(), It.IsAny<CancellationToken>()))
            .Callback<OperationReview, CancellationToken>((r, _) => _added.Add(r));
        _repo.Setup(r => r.LatestReviewsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, OperationReview>());
        _repo.Setup(r => r.ListReviewsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private void ActAs(string userId, params string[] roles)
    {
        _user.Setup(u => u.UserId).Returns(userId);
        _user.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(r => roles.Contains(r));
    }

    private static OperationExecution Exec(Guid voucherId, string createdBy = "clerk", RiskLevel? risk = RiskLevel.Low) => new()
    {
        Id = Guid.NewGuid(), VoucherId = voucherId, VahedCode = Vahed, CreatedBy = createdBy, RiskLevel = risk,
        TemplateCode = "T", InputJson = "{}", VoucherNo = "1",
    };

    private static TB_VOUCHERSHEAD Head(DocLife life = DocLife.Temporary) => new() { ID = Guid.NewGuid(), DOCLIFE = life, DOC_NUM = "12" };

    private static ReviewVoucherRow Row(Guid executionId, Guid voucherId, DocLife life = DocLife.Temporary, string createdBy = "clerk",
        string vahed = Vahed, RiskLevel risk = RiskLevel.Low, bool requireAttachment = false, bool hasAttachment = false, int attachmentCount = 0) =>
        new(executionId, voucherId, "12", "14050720", life, "شرح", null, "T", "الگو", "Form", vahed, createdBy, DateTime.UtcNow, 1000,
            risk, null, null, null, null, null, requireAttachment, hasAttachment, attachmentCount);

    private void Executions(params OperationExecution[] e) =>
        _repo.Setup(r => r.ListExecutionsByVouchersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(e);

    private void Rows(params ReviewVoucherRow[] rows) =>
        _repo.Setup(r => r.GetRowsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(rows);

    private HesabyarVoucherStateHook Hook() => new(_repo.Object, _user.Object);

    // ─────────── قلاب تغییر وضعیت (G1) ───────────

    [Fact]
    public async Task Hook_CreatorCannotReviewOwnVoucher()
    {
        var h = Head();
        Executions(Exec(h.ID, createdBy: "accountant"));
        await Assert.ThrowsAsync<HesabyarSelfReviewDeniedException>(() => Hook().BeforeStateChangeAsync([h], DocLife.Reviewed, default));
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Hook_WholeBatchRejected_IfAnyIsOwn()
    {
        var a = Head();
        var b = Head();
        Executions(Exec(a.ID), Exec(b.ID, createdBy: "accountant"));
        await Assert.ThrowsAsync<HesabyarSelfReviewDeniedException>(() => Hook().BeforeStateChangeAsync([a, b], DocLife.Reviewed, default));
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Hook_HighRisk_NeedsFinanceManager()
    {
        ActAs("hq", AppRoles.SetadAdmin);
        var h = Head();
        Executions(Exec(h.ID, risk: RiskLevel.High));
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() => Hook().BeforeStateChangeAsync([h], DocLife.Reviewed, default));
    }

    [Fact]
    public async Task Hook_Approve_RecordsApprovedReview()
    {
        var h = Head();
        var e = Exec(h.ID, risk: RiskLevel.High);
        Executions(e);
        await Hook().BeforeStateChangeAsync([h], DocLife.Reviewed, default);
        var r = Assert.Single(_added);
        Assert.Equal(ReviewAction.Approved, r.Action);
        Assert.Equal(e.Id, r.ExecutionId);
        Assert.Equal("accountant", r.ReviewerId);
    }

    [Fact]
    public async Task Hook_ManualVoucherWithoutExecution_IsUntouched()
    {
        Executions();
        await Hook().BeforeStateChangeAsync([Head()], DocLife.Reviewed, default);
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Hook_SameStateOrUnrelatedState_DoesNothing()
    {
        await Hook().BeforeStateChangeAsync([Head(DocLife.Reviewed)], DocLife.Reviewed, default);
        await Hook().BeforeStateChangeAsync([Head()], DocLife.Accepted, default);
        _repo.Verify(r => r.ListExecutionsByVouchersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Hook_BackToTemporaryAfterReturn_RecordsResubmitted()
    {
        var h = Head(DocLife.Draft);
        var e = Exec(h.ID);
        Executions(e);
        _repo.Setup(r => r.LatestReviewsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, OperationReview> { [e.Id] = new() { ExecutionId = e.Id, Action = ReviewAction.Returned } });

        await Hook().BeforeStateChangeAsync([h], DocLife.Temporary, default);

        Assert.Equal(ReviewAction.Resubmitted, Assert.Single(_added).Action);
    }

    [Fact]
    public async Task Hook_FirstMoveToTemporary_RecordsNothing()
    {
        var h = Head(DocLife.Draft);
        Executions(Exec(h.ID));
        await Hook().BeforeStateChangeAsync([h], DocLife.Temporary, default);
        Assert.Empty(_added);
    }

    // ─────────── «بررسی شد» ───────────

    private ApproveReviewsHandler Approve() => new(_repo.Object, _user.Object, _mediator.Object);

    [Fact]
    public async Task Approve_NonReviewer_Denied()
    {
        ActAs("clerk");
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            Approve().Handle(new ApproveReviewsCommand([Guid.NewGuid()]) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Approve_OtherUnit_IsNotFound()
    {
        var id = Guid.NewGuid();
        Rows(Row(id, Guid.NewGuid(), vahed: "9999"));
        await Assert.ThrowsAsync<NotFoundException>(() => Approve().Handle(new ApproveReviewsCommand([id]) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Approve_NotTemporary_IsConflict()
    {
        var id = Guid.NewGuid();
        Rows(Row(id, Guid.NewGuid(), life: DocLife.Draft));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Approve().Handle(new ApproveReviewsCommand([id]) { VahedCode = Vahed }, default));
        _mediator.Verify(m => m.Send(It.IsAny<ChangeVoucherStateCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Approve_MovesAllVouchersToReviewedInOneCommand()
    {
        var (e1, v1, e2, v2) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Rows(Row(e1, v1), Row(e2, v2));
        ChangeVoucherStateCommand? sent = null;
        _mediator.Setup(m => m.Send(It.IsAny<ChangeVoucherStateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((c, _) => sent = (ChangeVoucherStateCommand)c);

        var res = await Approve().Handle(new ApproveReviewsCommand([e1, e2]) { VahedCode = Vahed }, default);

        Assert.Equal(2, res.Count);
        Assert.Equal(DocLife.Reviewed, sent!.NewState);
        Assert.Equal(new[] { v1, v2 }, sent.VoucherHeadIds);
    }

    [Fact]
    public void ApproveValidator_RejectsEmptyAndDuplicates()
    {
        var v = new ApproveReviewsValidator();
        var id = Guid.NewGuid();
        Assert.False(v.Validate(new ApproveReviewsCommand([])).IsValid);
        Assert.False(v.Validate(new ApproveReviewsCommand([id, id])).IsValid);
        Assert.False(v.Validate(new ApproveReviewsCommand(Enumerable.Range(0, ApproveReviewsCommand.MaxBatchSize + 1).Select(_ => Guid.NewGuid()).ToList())).IsValid);
        Assert.True(v.Validate(new ApproveReviewsCommand([id])).IsValid);
    }

    // ─────────── برگشت ───────────

    private ReturnReviewHandler Return() => new(_repo.Object, _heads.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task Return_MovesToDraft_AndRecordsReason()
    {
        var head = Head();
        var id = Guid.NewGuid();
        Rows(Row(id, head.ID));
        _heads.Setup(h => h.GetManyForUpdateAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([head]);

        await Return().Handle(new ReturnReviewCommand(id, " attachment ", " فاکتور را پیوست کنید ") { VahedCode = Vahed }, default);

        Assert.Equal(DocLife.Draft, head.DOCLIFE);
        Assert.Equal("accountant", head.CHANGEUSERID);
        var r = Assert.Single(_added);
        Assert.Equal(ReviewAction.Returned, r.Action);
        Assert.Equal("attachment", r.ReasonCode);
        Assert.Equal("فاکتور را پیوست کنید", r.Message);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Return_AlreadyReviewedConcurrently_IsConflict()
    {
        var head = Head(DocLife.Reviewed);
        var id = Guid.NewGuid();
        Rows(Row(id, head.ID));
        _heads.Setup(h => h.GetManyForUpdateAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([head]);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Return().Handle(new ReturnReviewCommand(id, "other", "x") { VahedCode = Vahed }, default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ReturnValidator_OtherNeedsMessage()
    {
        var v = new ReturnReviewValidator();
        Assert.False(v.Validate(new ReturnReviewCommand(Guid.NewGuid(), "other", null)).IsValid);
        Assert.False(v.Validate(new ReturnReviewCommand(Guid.NewGuid(), "", "x")).IsValid);
        Assert.True(v.Validate(new ReturnReviewCommand(Guid.NewGuid(), "amount", null)).IsValid);
    }

    // ─────────── پاسخ ثبت‌کننده ───────────

    [Fact]
    public async Task Reply_OnlyCreator()
    {
        var id = Guid.NewGuid();
        Rows(Row(id, Guid.NewGuid(), createdBy: "clerk"));
        var handler = new ReplyToReviewHandler(_repo.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            handler.Handle(new ReplyToReviewCommand(id, "سلام") { VahedCode = Vahed }, default));

        ActAs("clerk");
        await handler.Handle(new ReplyToReviewCommand(id, " فاکتور پیوست شد ") { VahedCode = Vahed }, default);
        var r = Assert.Single(_added);
        Assert.Equal(ReviewAction.Replied, r.Action);
        Assert.Equal("فاکتور پیوست شد", r.Message);
    }

    // ─────────── کنترل‌های کارت ───────────

    private async Task<ReviewItemDto> Detail(ReviewVoucherRow row)
    {
        Rows(row);
        return (await new GetReviewDetailHandler(_repo.Object, _user.Object)
            .Handle(new GetReviewDetailQuery(row.ExecutionId) { VahedCode = Vahed }, default)).Item;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Controls_AttachmentFollowsRealAttachments(bool has, bool passed)
    {
        var item = await Detail(Row(Guid.NewGuid(), Guid.NewGuid(), requireAttachment: true, hasAttachment: has, attachmentCount: has ? 2 : 0));
        Assert.Contains(item.Controls!, c => c.Key == "attachment" && c.Passed == passed);
        Assert.Equal(has ? 2 : 0, item.AttachmentCount);
    }

    [Fact]
    public async Task Controls_NoAttachmentControl_WhenTemplateDoesNotRequireIt()
    {
        var item = await Detail(Row(Guid.NewGuid(), Guid.NewGuid()));
        Assert.DoesNotContain(item.Controls!, c => c.Key == "attachment");
    }

    [Fact]
    public async Task Controls_OwnVoucherFailsNotCreator_HighRiskShowsFinanceManager()
    {
        var item = await Detail(Row(Guid.NewGuid(), Guid.NewGuid(), createdBy: "accountant", risk: RiskLevel.High));
        Assert.True(item.IsMine);
        Assert.Contains(item.Controls!, c => c.Key == "notCreator" && !c.Passed);
        Assert.Contains(item.Controls!, c => c.Key == "financeManager" && c.Passed);
        Assert.Equal("high", item.RiskLevel);
    }

    [Fact]
    public async Task Detail_OtherUnit_IsNotFound()
    {
        Rows(Row(Guid.NewGuid(), Guid.NewGuid(), vahed: "9999"));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetReviewDetailHandler(_repo.Object, _user.Object).Handle(new GetReviewDetailQuery(Guid.NewGuid()) { VahedCode = Vahed }, default));
    }
}
