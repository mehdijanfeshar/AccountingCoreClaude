using Accounting.Application.Commands.FinancialStatements.TransitionFsRun;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Access;
using Accounting.Application.FinancialStatements.Periods;
using Accounting.Application.Queries.FinancialStatements;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;
using AppFs = Accounting.Application.FinancialStatements;

namespace Accounting.Application.Tests.FinancialStatements.Commands;

/// <summary>گردش تأیید اجرا: ارسال ← تأیید (چندمرحله‌ای) ← انتشار، برگشت، تفکیک وظایف و V-11.</summary>
public sealed class TransitionFsRunCommandHandlerTests
{
    private const string Preparer = "preparer";
    private const string Submitter = "submitter";

    private readonly Mock<IFsRunRepository> _runs = new();
    private readonly Mock<IFsApprovalStepRepository> _steps = new();
    private readonly Mock<AppFs.IFsUnitScopeProvider> _scopes = new();
    private readonly Mock<IFsPeriodRepository> _periods = new();
    private readonly Mock<IUnitAccessReadRepository> _unitAccess = new();
    private readonly Mock<IFsNarrativeRepository> _narratives = new();
    private readonly Mock<IFsPermissionRepository> _permissions = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    private readonly List<TB_FS_RUN_ACTION> _actions = [];
    private TB_FS_RUN _run = null!;
    private List<FsRunCheckDto> _checks = [];
    private List<FsRunActionDto> _history = [];

    public TransitionFsRunCommandHandlerTests()
    {
        _user.SetupGet(u => u.UserId).Returns("approver");
        _scopes.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(FsTestData.LeafScope());
        _steps.Setup(s => s.GetAllAsync(It.IsAny<FsFramework?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_APPROVAL_STEP>());
        _permissions.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERMISSION>());
        _unitAccess.Setup(a => a.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(FsTestData.Units());
        _periods.Setup(p => p.GetForYearAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_FS_PERIOD> { new() { VAHEDCODE = "101", YEAR = "1404", STATE = FsPeriodState.Locked } });
        _narratives.Setup(n => n.GetRunNarrativesAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_RUN_NARRATIVE>());
        _narratives.Setup(n => n.GetSetAsync(It.IsAny<string>(), It.IsAny<FsFramework>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_FS_NARRATIVE>());
        _runs.Setup(r => r.GetPublishedForUpdateAsync(It.IsAny<string>(), It.IsAny<FsFramework>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_FS_RUN>());
        _runs.Setup(r => r.AddActionAsync(It.IsAny<TB_FS_RUN_ACTION>(), It.IsAny<CancellationToken>()))
            .Callback<TB_FS_RUN_ACTION, CancellationToken>((a, _) => _actions.Add(a));

        Arrange(FsRunState.Draft);
    }

    private void Arrange(FsRunState state, bool usesDraft = false, int step = 0)
    {
        _run = new TB_FS_RUN
        {
            ID = Guid.NewGuid(), VAHEDCODE = "101", YEAR = "1404", TO_MONTH = 12, FRAMEWORK = FsFramework.Pension,
            STATE = state, USES_DRAFT = usesDraft, APPROVAL_STEP = step, ADDUSERID = Preparer, RUN_NO = 7,
        };
        _runs.Setup(r => r.GetForUpdateAsync(_run.ID, "101", It.IsAny<CancellationToken>())).ReturnsAsync(_run);
        _runs.Setup(r => r.GetDetailAsync(_run.ID, "101", It.IsAny<CancellationToken>())).ReturnsAsync(() => Detail());
    }

    private FsRunDetailDto Detail()
    {
        var summary = new FsRunSummaryDto(_run.ID, _run.RUN_NO, "101", null, false, 1, _run.FRAMEWORK, "1404", 12, 0, false, _run.USES_DRAFT,
            _run.STATE, null, 0, null, Preparer, DateTime.UtcNow);
        return new FsRunDetailDto(summary, "1404/01/01", "1404/12/29", null, 1, [], null, _checks, _history, []);
    }

    private TransitionFsRunCommandHandler Sut()
    {
        var access = new FsAccessService(_permissions.Object, _unitAccess.Object, _user.Object);
        var guard = new FsPeriodGuard(_periods.Object, _unitAccess.Object);
        return new TransitionFsRunCommandHandler(_runs.Object, _steps.Object, _scopes.Object, guard, _narratives.Object, access, _uow.Object, _user.Object);
    }

    private Task<FsRunState> Do(FsRunAction action, string? comments = null)
        => Sut().Handle(new TransitionFsRunCommand(_run.ID, action, comments) { VahedCode = "101" }, default);

    private void AsUser(string user) => _user.SetupGet(u => u.UserId).Returns(user);

    private void WithSteps(params TB_FS_APPROVAL_STEP[] steps)
        => _steps.Setup(s => s.GetAllAsync(It.IsAny<FsFramework?>(), It.IsAny<CancellationToken>())).ReturnsAsync(steps.ToList());

    private static TB_FS_APPROVAL_STEP Step(int no, string? approvers = null)
        => new() { ID = Guid.NewGuid(), VAHEDCODE = null, FRAMEWORK = FsFramework.Pension, STEP_NO = no, TITLE_FA = "مرحله" + no, APPROVER_USERIDS = approvers, IS_ACTIVE = true };

    private static FsRunActionDto Act(FsRunAction a, string user, int minute)
        => new(a, FsRunState.Draft, FsRunState.InReview, user, null, new DateTime(2026, 1, 1, 10, minute, 0));

    // ───────── کلی ─────────

    [Fact]
    public async Task UnknownRun_NotFound()
    {
        _runs.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((TB_FS_RUN?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().Handle(new TransitionFsRunCommand(Guid.NewGuid(), FsRunAction.Submit, null) { VahedCode = "101" }, default));
    }

    [Fact]
    public async Task UndefinedAction_Conflicts()
    {
        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Do(FsRunAction.Supersede));
    }

    [Theory]
    [InlineData(FsRunAction.Submit, FsOperation.Prepare)]
    [InlineData(FsRunAction.Approve, FsOperation.Approve)]
    [InlineData(FsRunAction.Return, FsOperation.Approve)]
    [InlineData(FsRunAction.Publish, FsOperation.Publish)]
    public async Task EachAction_RequiresItsOwnPermission(FsRunAction action, FsOperation needed)
    {
        // کاربر فقط «مشاهده» دارد؛ هر اقدام باید با ۴۰۳ رد شود، پیش از هر تغییری.
        _permissions.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERMISSION>
        {
            new() { ID = Guid.NewGuid(), USERID = "approver", VAHEDCODE = "101", OPERATIONS = FsOperation.View },
        });
        var before = _run.STATE;

        var ex = await Assert.ThrowsAsync<FsAccessDeniedException>(() => Do(action));

        Assert.Contains(FsAccessService.Label(needed), ex.PublicDetail);
        Assert.Equal(before, _run.STATE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ───────── Submit ─────────

    [Fact]
    public async Task Submit_Draft_MovesToInReview_AndLogsAction()
    {
        _run.APPROVAL_STEP = 3;

        var state = await Do(FsRunAction.Submit, "  لطفاً بررسی کنید  ");

        Assert.Equal(FsRunState.InReview, state);
        Assert.Equal(FsRunState.InReview, _run.STATE);
        Assert.Equal(0, _run.APPROVAL_STEP);
        var action = Assert.Single(_actions);
        Assert.Equal(FsRunAction.Submit, action.ACTION);
        Assert.Equal(FsRunState.Draft, action.FROM_STATE);
        Assert.Equal(FsRunState.InReview, action.TO_STATE);
        Assert.Equal("approver", action.USERID);
        Assert.Equal("لطفاً بررسی کنید", action.COMMENTS);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(FsRunState.InReview)]
    [InlineData(FsRunState.Approved)]
    [InlineData(FsRunState.Published)]
    [InlineData(FsRunState.Superseded)]
    public async Task Submit_FromNonDraft_Conflicts(FsRunState from)
    {
        Arrange(from);

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Do(FsRunAction.Submit));
    }

    [Fact]
    public async Task Submit_TrialRunWithDraftTemplate_Conflicts()
    {
        Arrange(FsRunState.Draft, usesDraft: true);

        var ex = await Assert.ThrowsAsync<FsTemplateConflictException>(() => Do(FsRunAction.Submit));

        Assert.Contains("آزمایشی", ex.Message);
    }

    [Fact]
    public async Task Submit_WithFailedBlockingCheck_Conflicts_ListingCodes()
    {
        _checks = [new FsRunCheckDto("V-01", "ت", FsCheckSeverity.Blocking, false, null, null, null)];

        var ex = await Assert.ThrowsAsync<FsTemplateConflictException>(() => Do(FsRunAction.Submit));

        Assert.Contains("V-01", ex.Message);
        Assert.Equal(FsRunState.Draft, _run.STATE);
    }

    [Fact]
    public async Task Submit_WithFailedWarningOrPassedBlocking_IsAllowed()
    {
        _checks =
        [
            new FsRunCheckDto("V-02", "ت", FsCheckSeverity.Warning, false, null, null, null),
            new FsRunCheckDto("V-03", "ت", FsCheckSeverity.Blocking, true, null, null, null),
        ];

        Assert.Equal(FsRunState.InReview, await Do(FsRunAction.Submit));
    }

    // ───────── Approve ─────────

    [Fact]
    public async Task Approve_NoSteps_SingleStage_Approves()
    {
        Arrange(FsRunState.InReview);
        _history = [Act(FsRunAction.Submit, Submitter, 1)];

        var state = await Do(FsRunAction.Approve);

        Assert.Equal(FsRunState.Approved, state);
        Assert.Null(Assert.Single(_actions).STEP_NO);
    }

    [Fact]
    public async Task Approve_FromDraft_Conflicts()
    {
        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Do(FsRunAction.Approve));
    }

    [Theory]
    [InlineData(Preparer)]
    [InlineData(Submitter)]
    public async Task Approve_ByPreparerOrSubmitter_IsDenied(string user)
    {
        Arrange(FsRunState.InReview);
        _history = [Act(FsRunAction.Submit, Submitter, 1)];
        AsUser(user);

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Do(FsRunAction.Approve));
        Assert.Equal(FsRunState.InReview, _run.STATE);
    }

    [Fact]
    public async Task Approve_SubmitterIsTakenFromLatestSubmit()
    {
        Arrange(FsRunState.InReview);
        // ارسال اول توسط «old»، ارسال دوم (آخرین) توسط «submitter». «old» حالا می‌تواند تأیید کند.
        _history = [Act(FsRunAction.Submit, "old", 1), Act(FsRunAction.Return, "x", 2), Act(FsRunAction.Submit, Submitter, 3)];
        AsUser("old");

        Assert.Equal(FsRunState.Approved, await Do(FsRunAction.Approve));
    }

    [Fact]
    public async Task Approve_MultiStep_FirstStepKeepsInReview_AndAdvancesStep()
    {
        Arrange(FsRunState.InReview);
        _history = [Act(FsRunAction.Submit, Submitter, 1)];
        WithSteps(Step(1), Step(2));

        var state = await Do(FsRunAction.Approve);

        Assert.Equal(FsRunState.InReview, state);
        Assert.Equal(1, _run.APPROVAL_STEP);
        Assert.Equal(1, Assert.Single(_actions).STEP_NO);
    }

    [Fact]
    public async Task Approve_MultiStep_LastStepApproves()
    {
        Arrange(FsRunState.InReview, step: 1);
        _history = [Act(FsRunAction.Submit, Submitter, 1), Act(FsRunAction.Approve, "first", 2)];
        WithSteps(Step(1), Step(2));

        var state = await Do(FsRunAction.Approve);

        Assert.Equal(FsRunState.Approved, state);
        Assert.Equal(2, _run.APPROVAL_STEP);
        Assert.Equal(2, Assert.Single(_actions).STEP_NO);
    }

    [Fact]
    public async Task Approve_SamePersonCannotApproveTwoStepsInSameCycle()
    {
        Arrange(FsRunState.InReview, step: 1);
        _history = [Act(FsRunAction.Submit, Submitter, 1), Act(FsRunAction.Approve, "approver", 2)];
        WithSteps(Step(1), Step(2));

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Do(FsRunAction.Approve));
    }

    [Fact]
    public async Task Approve_ApprovalFromPreviousCycle_DoesNotCount()
    {
        // «approver» در دور قبل (پیش از آخرین ارسال) تأیید کرده؛ در دور جدید آزاد است.
        Arrange(FsRunState.InReview);
        _history = [Act(FsRunAction.Submit, Submitter, 1), Act(FsRunAction.Approve, "approver", 2), Act(FsRunAction.Return, "x", 3), Act(FsRunAction.Submit, Submitter, 4)];
        WithSteps(Step(1), Step(2));

        var state = await Do(FsRunAction.Approve);

        Assert.Equal(FsRunState.InReview, state);
    }

    [Fact]
    public async Task Approve_StepRestrictedToApproverList()
    {
        Arrange(FsRunState.InReview);
        _history = [Act(FsRunAction.Submit, Submitter, 1)];
        WithSteps(Step(1, approvers: "alice,bob"));

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Do(FsRunAction.Approve));

        AsUser("alice");
        Assert.Equal(FsRunState.Approved, await Do(FsRunAction.Approve));
    }

    // ───────── Return ─────────

    [Theory]
    [InlineData(FsRunState.InReview)]
    [InlineData(FsRunState.Approved)]
    public async Task Return_FromReviewOrApproved_GoesToDraft_AndResetsStep(FsRunState from)
    {
        Arrange(from, step: 2);

        var state = await Do(FsRunAction.Return, "اصلاح شود");

        Assert.Equal(FsRunState.Draft, state);
        Assert.Equal(0, _run.APPROVAL_STEP);
        Assert.Equal("اصلاح شود", Assert.Single(_actions).COMMENTS);
    }

    [Theory]
    [InlineData(FsRunState.Draft)]
    [InlineData(FsRunState.Published)]
    [InlineData(FsRunState.Superseded)]
    public async Task Return_FromOtherStates_Conflicts(FsRunState from)
    {
        Arrange(from);

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Do(FsRunAction.Return, "دلیل"));
    }

    // ───────── Publish ─────────

    [Theory]
    [InlineData(FsRunState.Draft)]
    [InlineData(FsRunState.InReview)]
    [InlineData(FsRunState.Published)]
    public async Task Publish_FromNonApproved_Conflicts(FsRunState from)
    {
        Arrange(from);

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Do(FsRunAction.Publish));
    }

    [Fact]
    public async Task Publish_ByPreparer_IsDenied()
    {
        Arrange(FsRunState.Approved);
        AsUser(Preparer);

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Do(FsRunAction.Publish));
    }

    [Fact]
    public async Task Publish_WhenPeriodNotLocked_ViolatesV11()
    {
        Arrange(FsRunState.Approved);
        _periods.Setup(p => p.GetForYearAsync("1404", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_FS_PERIOD> { new() { VAHEDCODE = "101", YEAR = "1404", STATE = FsPeriodState.SoftClosed } });

        var ex = await Assert.ThrowsAsync<FsTemplateConflictException>(() => Do(FsRunAction.Publish));

        Assert.Contains("V-11", ex.Message);
        Assert.Equal(FsRunState.Approved, _run.STATE);
    }

    [Fact]
    public async Task Publish_WhenSubUnitsIncluded_AllUnitsInScopeMustBeLocked()
    {
        Arrange(FsRunState.Approved);
        _run.VAHEDCODE = "10";
        _run.INCLUDE_SUBUNITS = true;
        _runs.Setup(r => r.GetForUpdateAsync(_run.ID, "101", It.IsAny<CancellationToken>())).ReturnsAsync(_run);
        // فقط «10» و «101» قفل‌اند؛ «102» نه.
        _periods.Setup(p => p.GetForYearAsync("1404", It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERIOD>
        {
            new() { VAHEDCODE = "10", YEAR = "1404", STATE = FsPeriodState.SoftClosed },
            new() { VAHEDCODE = "101", YEAR = "1404", STATE = FsPeriodState.Locked },
            new() { VAHEDCODE = "102", YEAR = "1404", STATE = FsPeriodState.Open },
        });

        var ex = await Assert.ThrowsAsync<FsTemplateConflictException>(() => Do(FsRunAction.Publish));

        Assert.Contains("(10، 102)", ex.Message);
    }

    [Fact]
    public async Task Publish_LockedUnit_Publishes()
    {
        Arrange(FsRunState.Approved);

        var state = await Do(FsRunAction.Publish);

        Assert.Equal(FsRunState.Published, state);
        Assert.Equal(FsRunState.Published, _run.STATE);
    }

    [Fact]
    public async Task Publish_SupersedesPreviouslyPublishedRunOfSamePeriod()
    {
        Arrange(FsRunState.Approved);
        var old = new TB_FS_RUN { ID = Guid.NewGuid(), VAHEDCODE = "101", STATE = FsRunState.Published };
        _runs.Setup(r => r.GetPublishedForUpdateAsync("101", FsFramework.Pension, "1404", 12, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_FS_RUN> { old, _run });

        await Do(FsRunAction.Publish);

        Assert.Equal(FsRunState.Superseded, old.STATE);
        Assert.Equal(FsRunState.Published, _run.STATE);
        Assert.Equal(2, _actions.Count);
        var supersede = _actions.Single(a => a.RUN_ID == old.ID);
        Assert.Equal(FsRunAction.Supersede, supersede.ACTION);
        Assert.Contains("7", supersede.COMMENTS);
        Assert.Contains(_actions, a => a.RUN_ID == _run.ID && a.ACTION == FsRunAction.Publish);
    }

    [Fact]
    public async Task Publish_FreezesLiveNarratives_WhenRunHasNone()
    {
        Arrange(FsRunState.Approved);
        var live = new TB_FS_NARRATIVE { ID = Guid.NewGuid(), TITLE_FA = "یادداشت ۱", ORDER_NO = 1, VERSION_NO = 3 };
        _narratives.Setup(n => n.GetSetAsync("101", FsFramework.Pension, "1404", false, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_NARRATIVE> { live });
        List<TB_FS_RUN_NARRATIVE>? frozen = null;
        _narratives.Setup(n => n.AddRunNarrativesAsync(It.IsAny<IEnumerable<TB_FS_RUN_NARRATIVE>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TB_FS_RUN_NARRATIVE>, CancellationToken>((rows, _) => frozen = rows.ToList());

        await Do(FsRunAction.Publish);

        var row = Assert.Single(frozen!);
        Assert.Equal(_run.ID, row.RUN_ID);
        Assert.Equal(live.ID, row.NARRATIVE_ID);
        Assert.Equal("یادداشت ۱", row.TITLE_FA);
        Assert.Equal(3, row.VERSION_NO);
    }

    [Fact]
    public async Task Publish_DoesNotRefreezeNarratives_WhenRunAlreadyHasThem()
    {
        Arrange(FsRunState.Approved);
        _narratives.Setup(n => n.GetRunNarrativesAsync(_run.ID, "101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_FS_RUN_NARRATIVE> { new() });

        await Do(FsRunAction.Publish);

        _narratives.Verify(n => n.AddRunNarrativesAsync(It.IsAny<IEnumerable<TB_FS_RUN_NARRATIVE>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public sealed class TransitionFsRunCommandValidatorTests
{
    private readonly TransitionFsRunCommandValidator _sut = new();

    [Fact]
    public void EmptyId_Fails() => Assert.False(_sut.Validate(new TransitionFsRunCommand(Guid.Empty, FsRunAction.Submit, null)).IsValid);

    [Theory]
    [InlineData(FsRunAction.Submit)]
    [InlineData(FsRunAction.Approve)]
    [InlineData(FsRunAction.Publish)]
    public void AllowedActions_WithoutComment_Pass(FsRunAction a) => Assert.True(_sut.Validate(new TransitionFsRunCommand(Guid.NewGuid(), a, null)).IsValid);

    [Fact]
    public void Supersede_IsNotAUserAction() => Assert.False(_sut.Validate(new TransitionFsRunCommand(Guid.NewGuid(), FsRunAction.Supersede, null)).IsValid);

    [Fact]
    public void Return_NeedsReason()
    {
        Assert.False(_sut.Validate(new TransitionFsRunCommand(Guid.NewGuid(), FsRunAction.Return, null)).IsValid);
        Assert.False(_sut.Validate(new TransitionFsRunCommand(Guid.NewGuid(), FsRunAction.Return, "  ")).IsValid);
        Assert.True(_sut.Validate(new TransitionFsRunCommand(Guid.NewGuid(), FsRunAction.Return, "دلیل")).IsValid);
    }

    [Fact]
    public void CommentLongerThan1000_Fails()
        => Assert.False(_sut.Validate(new TransitionFsRunCommand(Guid.NewGuid(), FsRunAction.Approve, new string('x', 1001))).IsValid);
}
