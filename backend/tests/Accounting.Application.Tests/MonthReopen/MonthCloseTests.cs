using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.MonthReopen;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.MonthReopen;

public class MonthCloseTests
{
    private readonly Mock<IMonthCloseRepository> _repo = new();
    private readonly Mock<IUnitAccessReadRepository> _unitAccess = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private List<TB_MONTH_CLOSE_LOG> _logs = new();

    public MonthCloseTests()
    {
        _user.Setup(u => u.UserId).Returns("u1");
        _repo.Setup(r => r.GetVoucherCountsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UnitDocLifeCount>());
        _repo.Setup(r => r.GetUnsentElamCountsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        _repo.Setup(r => r.AddLogsAsync(It.IsAny<IEnumerable<TB_MONTH_CLOSE_LOG>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TB_MONTH_CLOSE_LOG>, CancellationToken>((l, _) => _logs = l.ToList());
        _repo.Setup(r => r.AcceptReviewedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        _unitAccess.Setup(r => r.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UnitNode>
        {
            new(Guid.NewGuid(), "0001", "ستاد", null, true, "10"),
            new(Guid.NewGuid(), "0002", "بیمارستان", null, false, "2"),
            new(Guid.NewGuid(), "0003", "شعبه", null, false, "1"),
        });
    }

    private void AsHq()
    {
        _user.Setup(u => u.IsInRole(AppRoles.SetadAdmin)).Returns(true);
        _user.Setup(u => u.VahedCode).Returns("0001");
        _unitAccess.Setup(r => r.GetUnitProfileAsync("0001", It.IsAny<CancellationToken>())).ReturnsAsync(new UnitProfile("ستاد", true));
    }

    private CloseMonthHandler H() => new(_repo.Object, _unitAccess.Object, _user.Object, _uow.Object);

    private void Counts(params UnitDocLifeCount[] c)
        => _repo.Setup(r => r.GetVoucherCountsAsync("1404", 3, It.IsAny<CancellationToken>())).ReturnsAsync(c.ToList());

    [Theory]
    [InlineData("1404", 3, true)]
    [InlineData("14", 3, false)]
    [InlineData("1404", 13, false)]
    public void CloseValidator(string y, int m, bool ok)
        => Assert.Equal(ok, new CloseMonthValidator().Validate(new CloseMonthCommand(y, m, null, null)).IsValid);

    [Fact]
    public void CloseValidator_InvalidCategory_Fails()
        => Assert.False(new CloseMonthValidator().Validate(new CloseMonthCommand("1404", 3, (UnitCategory)9, null)).IsValid);

    [Fact]
    public void LogValidator_MonthOptionalButRanged()
    {
        Assert.True(new GetMonthCloseLogValidator().Validate(new GetMonthCloseLogQuery("1404", null, null)).IsValid);
        Assert.False(new GetMonthCloseLogValidator().Validate(new GetMonthCloseLogQuery("1404", 13, null)).IsValid);
    }

    [Fact]
    public async Task Close_NotHeadquarters_Throws_NothingPersisted()
    {
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() => H().Handle(new("1404", 3, null, null), default));
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Close_UnknownUnitCode_Throws()
    {
        AsHq();
        await Assert.ThrowsAsync<MonthReopenException>(() => H().Handle(new("1404", 3, null, "9999"), default));
    }

    [Fact]
    public async Task Close_UnitWithOnlyReviewed_IsClosed()
    {
        AsHq();
        Counts(new UnitDocLifeCount("0002", DocLife.Reviewed, 4));

        var r = await H().Handle(new("1404", 3, UnitCategory.Medical, null), default);

        Assert.Equal(1, r.TargetUnits);
        Assert.Equal(1, r.ClosedUnits);
        Assert.Equal(7, r.AcceptedVouchers);
        _repo.Verify(x => x.AcceptReviewedAsync("1404", 3, It.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(new[] { "0002" })), "u1", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(MonthCloseResult.Closed, _logs.Single().RESULT);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DocLife.Draft)]
    [InlineData(DocLife.Temporary)]
    public async Task Close_UnitWithPendingVouchers_IsRejectedWithReason(DocLife pending)
    {
        AsHq();
        Counts(new UnitDocLifeCount("0002", DocLife.Reviewed, 4), new("0002", pending, 2));

        var r = await H().Handle(new("1404", 3, null, "0002"), default);

        Assert.Equal(1, r.RejectedUnits);
        Assert.Equal(0, r.ClosedUnits);
        var log = _logs.Single();
        Assert.Equal(MonthCloseResult.Rejected, log.RESULT);
        Assert.Equal(2, log.PENDING_VOUCHERS);
        _repo.Verify(x => x.AcceptReviewedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Close_UnknownDocLife_CountsAsPending()
    {
        AsHq();
        Counts(new UnitDocLifeCount("0002", null, 1), new UnitDocLifeCount("0002", (DocLife)9, 2));
        var r = await H().Handle(new("1404", 3, null, "0002"), default);
        Assert.Equal(1, r.RejectedUnits);
        Assert.Equal(3, _logs.Single().PENDING_VOUCHERS);
    }

    [Fact]
    public async Task Close_UnsentElam_RejectsEvenWithOnlyReviewed()
    {
        AsHq();
        Counts(new UnitDocLifeCount("0002", DocLife.Reviewed, 4));
        _repo.Setup(x => x.GetUnsentElamCountsAsync("1404", 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["0002"] = 2 });
        var r = await H().Handle(new("1404", 3, null, "0002"), default);
        Assert.Equal(1, r.RejectedUnits);
        Assert.Equal(2, _logs.Single().PENDING_ELAMS);
    }

    [Fact]
    public async Task Close_AlreadyAcceptedOrIdle_NoLogNoClose()
    {
        AsHq();
        Counts(new UnitDocLifeCount("0002", DocLife.Accepted, 5));
        var r = await H().Handle(new("1404", 3, null, null), default);
        // 0001 و 0003 بی‌سند (idle)، 0002 قبلاً بسته
        Assert.Equal(3, r.TargetUnits);
        Assert.Equal(1, r.AlreadyClosedUnits);
        Assert.Equal(2, r.IdleUnits);
        Assert.Empty(_logs);
    }

    [Fact]
    public async Task Close_CategoryFilter_OnlyThatCategory()
    {
        AsHq();
        var r = await H().Handle(new("1404", 3, UnitCategory.Insurance, null), default);
        Assert.Equal(1, r.TargetUnits);
    }

    [Fact]
    public async Task Close_RepoFails_RollsBackAndRethrows()
    {
        AsHq();
        Counts(new UnitDocLifeCount("0002", DocLife.Reviewed, 1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => H().Handle(new("1404", 3, null, "0002"), default));
        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogQuery_NonHq_ForcedToOwnUnit_Hq_UsesRequestedUnit()
    {
        _repo.Setup(r => r.GetLogsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_MONTH_CLOSE_LOG>());
        var h = new GetMonthCloseLogHandler(_repo.Object, _unitAccess.Object, _user.Object);

        await h.Handle(new GetMonthCloseLogQuery("1404", 3, "0003") { VahedCode = "0002" }, default);
        _repo.Verify(r => r.GetLogsAsync("1404", 3, "0002", It.IsAny<CancellationToken>()), Times.Once);

        AsHq();
        await h.Handle(new GetMonthCloseLogQuery("1404", 3, " 0003 ") { VahedCode = "0001" }, default);
        _repo.Verify(r => r.GetLogsAsync("1404", 3, "0003", It.IsAny<CancellationToken>()), Times.Once);
        await h.Handle(new GetMonthCloseLogQuery("1404", null, null) { VahedCode = "0001" }, default);
        _repo.Verify(r => r.GetLogsAsync("1404", null, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
