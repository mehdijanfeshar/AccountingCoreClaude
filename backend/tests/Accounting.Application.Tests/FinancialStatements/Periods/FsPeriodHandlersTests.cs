using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Periods;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.FinancialStatements.Periods;

/// <summary>ماشین حالت بستن دورهٔ صورت‌ها: باز ← بستهٔ موقت ← قفل، و بازگشایی با تأیید ستاد.</summary>
public sealed class FsPeriodHandlersTests
{
    private readonly Mock<IFsPeriodRepository> _periods = new();
    private readonly Mock<IFsRunRepository> _runs = new();
    private readonly Mock<IUnitAccessReadRepository> _unitAccess = new();
    private readonly Mock<IFsUnitScopeProvider> _scopes = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public FsPeriodHandlersTests()
    {
        _user.SetupGet(u => u.UserId).Returns("u1");
        _unitAccess.Setup(a => a.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(FsTestData.Units());
    }

    private FsPeriodHandlers Sut(FsUnitScope scope)
    {
        _scopes.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(scope);
        return new FsPeriodHandlers(_periods.Object, _runs.Object, _unitAccess.Object, _scopes.Object, _uow.Object, _user.Object);
    }

    private TB_FS_PERIOD ExistingPeriod(string unit, FsPeriodState state, string? requestedBy = null, string? reason = null)
    {
        var p = new TB_FS_PERIOD
        {
            ID = Guid.NewGuid(), VAHEDCODE = unit, YEAR = "1404", STATE = state,
            REOPEN_REQUESTED_BY = requestedBy, REOPEN_REASON = reason,
        };
        _periods.Setup(r => r.GetForUpdateAsync(unit, "1404", It.IsAny<CancellationToken>())).ReturnsAsync(p);
        return p;
    }

    private static TransitionFsPeriodCommand Cmd(string unit, FsPeriodAction action, string? reason = null)
        => new(unit, "1404", action, reason) { VahedCode = "1" };

    [Fact]
    public async Task Close_WhenNoRowYet_CreatesRowAndMovesToSoftClosed()
    {
        _periods.Setup(r => r.GetForUpdateAsync("101", "1404", It.IsAny<CancellationToken>())).ReturnsAsync((TB_FS_PERIOD?)null);
        TB_FS_PERIOD? added = null;
        _periods.Setup(r => r.AddAsync(It.IsAny<TB_FS_PERIOD>(), It.IsAny<CancellationToken>())).Callback<TB_FS_PERIOD, CancellationToken>((p, _) => added = p);
        TB_FS_PERIOD_LOG? log = null;
        _periods.Setup(r => r.AddLogAsync(It.IsAny<TB_FS_PERIOD_LOG>(), It.IsAny<CancellationToken>())).Callback<TB_FS_PERIOD_LOG, CancellationToken>((l, _) => log = l);

        var state = await Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.Close), default);

        Assert.Equal(FsPeriodState.SoftClosed, state);
        Assert.NotNull(added);
        Assert.Equal("101", added!.VAHEDCODE);
        Assert.Equal("u1", added.ADDUSERID);
        Assert.Equal(FsPeriodState.Open, log!.FROM_STATE);
        Assert.Equal(FsPeriodState.SoftClosed, log.TO_STATE);
        Assert.Equal(FsPeriodAction.Close, log.ACTION);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(FsPeriodState.SoftClosed)]
    [InlineData(FsPeriodState.Locked)]
    public async Task Close_FromNonOpen_Conflicts(FsPeriodState from)
    {
        ExistingPeriod("101", from);

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.Close), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Lock_FromSoftClosed_Locks()
    {
        var p = ExistingPeriod("101", FsPeriodState.SoftClosed);

        var state = await Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.Lock), default);

        Assert.Equal(FsPeriodState.Locked, state);
        Assert.Equal("u1", p.CHANGEUSERID);
    }

    [Fact]
    public async Task Lock_FromOpen_Conflicts()
    {
        ExistingPeriod("101", FsPeriodState.Open);

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.Lock), default));
    }

    [Fact]
    public async Task Reopen_FromSoftClosed_GoesBackToOpen()
    {
        ExistingPeriod("101", FsPeriodState.SoftClosed);

        var state = await Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.Reopen, "اصلاح"), default);

        Assert.Equal(FsPeriodState.Open, state);
    }

    [Fact]
    public async Task Reopen_FromLocked_Conflicts_MustGoThroughRequest()
    {
        ExistingPeriod("101", FsPeriodState.Locked);

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.Reopen, "دلیل"), default));
    }

    [Fact]
    public async Task UnitOutsideScope_IsDenied()
    {
        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Sut(FsTestData.ProvinceScope()).Handle(Cmd("201", FsPeriodAction.Close), default));
        _periods.Verify(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestReopen_OnLocked_RecordsRequesterAndTrimmedReason()
    {
        var p = ExistingPeriod("101", FsPeriodState.Locked);

        var state = await Sut(FsTestData.ProvinceScope()).Handle(Cmd("101", FsPeriodAction.RequestReopen, "  خطای ثبت  "), default);

        Assert.Equal(FsPeriodState.Locked, state);
        Assert.Equal("u1", p.REOPEN_REQUESTED_BY);
        Assert.Equal("خطای ثبت", p.REOPEN_REASON);
        Assert.NotNull(p.REOPEN_REQUESTED_DATE);
    }

    [Fact]
    public async Task RequestReopen_WhenAlreadyRequested_Conflicts()
    {
        ExistingPeriod("101", FsPeriodState.Locked, requestedBy: "other", reason: "قبلی");

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Sut(FsTestData.ProvinceScope()).Handle(Cmd("101", FsPeriodAction.RequestReopen, "دلیل"), default));
    }

    [Fact]
    public async Task RequestReopen_WhenNotLocked_Conflicts()
    {
        ExistingPeriod("101", FsPeriodState.SoftClosed);

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Sut(FsTestData.ProvinceScope()).Handle(Cmd("101", FsPeriodAction.RequestReopen, "دلیل"), default));
    }

    [Fact]
    public async Task ApproveReopen_ByHeadquarters_MovesToSoftClosed_AndClearsRequest()
    {
        var p = ExistingPeriod("101", FsPeriodState.Locked, requestedBy: "requester", reason: "دلیل درخواست");
        TB_FS_PERIOD_LOG? log = null;
        _periods.Setup(r => r.AddLogAsync(It.IsAny<TB_FS_PERIOD_LOG>(), It.IsAny<CancellationToken>())).Callback<TB_FS_PERIOD_LOG, CancellationToken>((l, _) => log = l);

        var state = await Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.ApproveReopen), default);

        Assert.Equal(FsPeriodState.SoftClosed, state);
        Assert.Null(p.REOPEN_REQUESTED_BY);
        Assert.Null(p.REOPEN_REASON);
        Assert.Null(p.REOPEN_REQUESTED_DATE);
        Assert.Equal("دلیل درخواست", log!.REASON);   // دلیل درخواست در لاگ می‌ماند
    }

    [Fact]
    public async Task ApproveReopen_ByRequester_IsDenied()
    {
        ExistingPeriod("101", FsPeriodState.Locked, requestedBy: "u1", reason: "دلیل");

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.ApproveReopen), default));
    }

    [Theory]
    [InlineData(FsPeriodAction.ApproveReopen)]
    [InlineData(FsPeriodAction.RejectReopen)]
    public async Task ApproveOrRejectReopen_ByNonHeadquarters_IsDenied(FsPeriodAction action)
    {
        ExistingPeriod("101", FsPeriodState.Locked, requestedBy: "requester", reason: "دلیل");

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Sut(FsTestData.ProvinceScope()).Handle(Cmd("101", action, "رد"), default));
    }

    [Theory]
    [InlineData(FsPeriodAction.ApproveReopen)]
    [InlineData(FsPeriodAction.RejectReopen)]
    public async Task ApproveOrRejectReopen_WithoutOpenRequest_Conflicts(FsPeriodAction action)
    {
        ExistingPeriod("101", FsPeriodState.Locked);

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Sut(FsTestData.HqScope()).Handle(Cmd("101", action, "رد"), default));
    }

    [Fact]
    public async Task RejectReopen_ByHeadquarters_StaysLocked_AndClearsRequest()
    {
        var p = ExistingPeriod("101", FsPeriodState.Locked, requestedBy: "requester", reason: "دلیل");

        var state = await Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.RejectReopen, "مردود"), default);

        Assert.Equal(FsPeriodState.Locked, state);
        Assert.Null(p.REOPEN_REQUESTED_BY);
    }

    [Fact]
    public async Task RejectReopen_ByRequesterWhoIsHeadquarters_IsAllowed()
    {
        // فقط «تأیید» برای خود درخواست‌کننده ممنوع است، رد نه.
        ExistingPeriod("101", FsPeriodState.Locked, requestedBy: "u1", reason: "دلیل");

        var state = await Sut(FsTestData.HqScope()).Handle(Cmd("101", FsPeriodAction.RejectReopen, "رد"), default);

        Assert.Equal(FsPeriodState.Locked, state);
    }

    [Fact]
    public async Task UndefinedAction_Conflicts()
    {
        ExistingPeriod("101", FsPeriodState.Open);

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Sut(FsTestData.HqScope()).Handle(Cmd("101", (FsPeriodAction)99), default));
    }

    [Fact]
    public async Task Log_Query_UnitOutsideScope_IsDenied()
    {
        var q = new GetFsPeriodLogQuery("201", "1404") { VahedCode = "10" };

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Sut(FsTestData.ProvinceScope()).Handle(q, default));
    }

    [Fact]
    public async Task Log_Query_MapsRows()
    {
        var when = new DateTime(2026, 1, 1);
        _periods.Setup(r => r.GetLogsAsync("101", "1404", It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERIOD_LOG>
        {
            new() { ACTION = FsPeriodAction.Close, FROM_STATE = FsPeriodState.Open, TO_STATE = FsPeriodState.SoftClosed, USERID = "u1", REASON = "r", CREATEDDATE = when },
        });

        var result = await Sut(FsTestData.ProvinceScope()).Handle(new GetFsPeriodLogQuery("101", "1404") { VahedCode = "10" }, default);

        var dto = Assert.Single(result);
        Assert.Equal(FsPeriodAction.Close, dto.Action);
        Assert.Equal(FsPeriodState.SoftClosed, dto.ToState);
        Assert.Equal(when, dto.CreatedDate);
    }

    [Fact]
    public async Task Board_ShowsOwnUnitAndChildren_WithLockedViaForInheritedLock()
    {
        _periods.Setup(r => r.GetForYearAsync("1404", It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERIOD>
        {
            new() { VAHEDCODE = "10", YEAR = "1404", STATE = FsPeriodState.Locked },
            new() { VAHEDCODE = "102", YEAR = "1404", STATE = FsPeriodState.SoftClosed, REOPEN_REQUESTED_BY = "x", REOPEN_REASON = "y" },
        });
        _runs.Setup(r => r.GetLatestRunsAsync(It.IsAny<IReadOnlyCollection<string>>(), "1404", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Accounting.Application.Queries.FinancialStatements.FsUnitRunStatusDto>());

        var board = await Sut(FsTestData.ProvinceScope()).Handle(new GetFsPeriodBoardQuery("1404") { VahedCode = "10" }, default);

        Assert.False(board.IsHeadquarters);
        Assert.Equal(new[] { "10", "101", "102" }, board.Units.Select(u => u.VahedCode).ToArray());
        var self = board.Units[0];
        Assert.True(self.IsSelf);
        Assert.Equal(FsPeriodState.Locked, self.State);
        Assert.Null(self.LockedVia);                       // قفل خودش است، نه ارثی
        var child = board.Units[1];
        Assert.Equal(FsPeriodState.Open, child.State);      // ردیف ندارد = باز
        Assert.Equal("10", child.LockedVia);               // قفل از والد
        Assert.True(board.Units[2].ReopenRequested);
    }
}
