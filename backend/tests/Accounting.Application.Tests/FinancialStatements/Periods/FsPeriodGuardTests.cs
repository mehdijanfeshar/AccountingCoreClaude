using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Periods;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.FinancialStatements.Periods;

/// <summary>V-11: واحد قفل است اگر خودش یا یکی از بالادستی‌هایش قفل باشد.</summary>
public sealed class FsPeriodGuardTests
{
    private static FsPeriodGuard Create(params (string Unit, FsPeriodState State)[] states)
    {
        var periods = new Mock<IFsPeriodRepository>();
        periods.Setup(p => p.GetForYearAsync("1404", It.IsAny<CancellationToken>()))
            .ReturnsAsync(states.Select(s => new TB_FS_PERIOD { VAHEDCODE = s.Unit, YEAR = "1404", STATE = s.State }).ToList());

        var access = new Mock<IUnitAccessReadRepository>();
        access.Setup(a => a.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(FsTestData.Units());

        return new FsPeriodGuard(periods.Object, access.Object);
    }

    [Fact]
    public async Task NoPeriodRows_EverythingIsUnlocked_SingleUnit()
    {
        var result = await Create().GetUnlockedAsync("101", false, "1404", default);

        Assert.Equal(new[] { "101" }, result.ToArray());
    }

    [Fact]
    public async Task IncludeSubUnits_ReturnsWholeSubtree_Sorted()
    {
        var result = await Create().GetUnlockedAsync("10", true, "1404", default);

        Assert.Equal(new[] { "10", "101", "102" }, result.ToArray());
    }

    [Fact]
    public async Task SelfLocked_IsExcluded()
    {
        var result = await Create(("101", FsPeriodState.Locked)).GetUnlockedAsync("101", false, "1404", default);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(FsPeriodState.Open)]
    [InlineData(FsPeriodState.SoftClosed)]
    public async Task OpenOrSoftClosed_CountsAsUnlocked(FsPeriodState state)
    {
        var result = await Create(("101", state)).GetUnlockedAsync("101", false, "1404", default);

        Assert.Equal(new[] { "101" }, result.ToArray());
    }

    [Fact]
    public async Task LockedParent_LocksAllDescendants()
    {
        var result = await Create(("10", FsPeriodState.Locked)).GetUnlockedAsync("10", true, "1404", default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task LockedGrandParent_LocksLeaf_ButNotOtherBranch()
    {
        var guard = Create(("10", FsPeriodState.Locked));

        Assert.Empty(await guard.GetUnlockedAsync("101", false, "1404", default));
        Assert.Equal(new[] { "201" }, (await guard.GetUnlockedAsync("201", false, "1404", default)).ToArray());
    }

    [Fact]
    public async Task OneChildLocked_OthersStayUnlocked_InSubtree()
    {
        var result = await Create(("101", FsPeriodState.Locked)).GetUnlockedAsync("10", true, "1404", default);

        Assert.Equal(new[] { "10", "102" }, result.ToArray());
    }

    [Fact]
    public async Task UnknownUnit_IsTreatedAsStandalone()
    {
        var result = await Create().GetUnlockedAsync("999", true, "1404", default);

        Assert.Equal(new[] { "999" }, result.ToArray());
    }
}
