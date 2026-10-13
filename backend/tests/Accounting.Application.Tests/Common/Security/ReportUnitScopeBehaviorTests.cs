using Accounting.Application.AuditLogs;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Common.Security;

public class ReportUnitScopeBehaviorTests
{
    private readonly Mock<IUnitAccessReadRepository> _unitAccess = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IHeadquartersAccessService> _hq = new();
    private readonly ReportUnitScope _scope = new();

    // ساختار: ستاد(1) ← A(2) ← A1(3) ؛ B(4) مستقل
    private static readonly Guid Hq = Guid.NewGuid(), A = Guid.NewGuid(), A1 = Guid.NewGuid(), B = Guid.NewGuid();

    public ReportUnitScopeBehaviorTests()
    {
        _unitAccess.Setup(r => r.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UnitNode>
        {
            new(Hq, "0001", "ستاد", null, true, "10"),
            new(A, "0002", "A", Hq, false, "2"),
            new(A1, "0003", "A1", A, false, "1"),
            new(B, "0004", "B", Hq, false, "1"),
        });
    }

    private ReportUnitScopeBehavior<GetAuditLogsQuery, string> Behavior(bool withHq = true)
        => new(_unitAccess.Object, _scope, _user.Object, withHq ? _hq.Object : null);

    private static GetAuditLogsQuery Q(ReportUnitScopeMode mode, string vahed = "0002")
        => new(null, null, null, null, null, null, null, null) { UnitScope = mode, VahedCode = vahed };

    private static Task<string> Next(CancellationToken _) => Task.FromResult("ok");

    [Fact]
    public async Task Self_NoCategoryNoProvince_DoesNotTouchScope()
    {
        var r = await Behavior().Handle(Q(ReportUnitScopeMode.Self), Next, default);
        Assert.Equal("ok", r);
        Assert.Null(_scope.VahedCodes);
        _unitAccess.Verify(u => u.GetAllUnitsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NonMultiUnitRequest_PassesThrough()
    {
        var b = new ReportUnitScopeBehavior<string, string>(_unitAccess.Object, _scope, _user.Object);
        Assert.Equal("ok", await b.Handle("x", Next, default));
        Assert.Null(_scope.VahedCodes);
    }

    [Fact]
    public async Task WithSubUnits_SetsSelfAndDescendantsOnly()
    {
        await Behavior().Handle(Q(ReportUnitScopeMode.WithSubUnits, "0002"), Next, default);
        Assert.Equal(new[] { "0002", "0003" }, _scope.VahedCodes!.OrderBy(x => x));
    }

    [Fact]
    public async Task WithSubUnits_UnknownRoot_ThrowsNoUnits()
    {
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            Behavior().Handle(Q(ReportUnitScopeMode.WithSubUnits, "9999"), Next, default));
        Assert.Null(_scope.VahedCodes);
    }

    [Fact]
    public async Task AllUnits_WithoutAbility_Denied()
    {
        _hq.Setup(h => h.HasAbilityAsync(AbilityCatalog.ReportsUnitCategory, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            Behavior().Handle(Q(ReportUnitScopeMode.AllUnits), Next, default));
    }

    [Fact]
    public async Task AllUnits_WithAbility_SetsEveryUnit()
    {
        _hq.Setup(h => h.HasAbilityAsync(AbilityCatalog.ReportsUnitCategory, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Behavior().Handle(Q(ReportUnitScopeMode.AllUnits), Next, default);
        Assert.Equal(4, _scope.VahedCodes!.Count);
    }

    [Fact]
    public async Task AllUnits_NoHqService_NationalRoleAllowed()
    {
        _user.Setup(u => u.IsInRole(AppRoles.National)).Returns(true);
        await Behavior(withHq: false).Handle(Q(ReportUnitScopeMode.AllUnits), Next, default);
        Assert.Equal(4, _scope.VahedCodes!.Count);
    }

    [Fact]
    public async Task AllUnits_NoHqService_SetadAdminOfHeadquartersAllowed_OfOtherUnitDenied()
    {
        _user.Setup(u => u.IsInRole(AppRoles.SetadAdmin)).Returns(true);
        _user.Setup(u => u.VahedCode).Returns("0002");
        _unitAccess.Setup(r => r.GetUnitProfileAsync("0002", It.IsAny<CancellationToken>())).ReturnsAsync(new UnitProfile("A", false));
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            Behavior(withHq: false).Handle(Q(ReportUnitScopeMode.AllUnits), Next, default));

        _user.Setup(u => u.VahedCode).Returns("0001");
        _unitAccess.Setup(r => r.GetUnitProfileAsync("0001", It.IsAny<CancellationToken>())).ReturnsAsync(new UnitProfile("ستاد", true));
        await Behavior(withHq: false).Handle(Q(ReportUnitScopeMode.AllUnits, "0001"), Next, default);
        Assert.Equal(4, _scope.VahedCodes!.Count);
    }

    [Fact]
    public async Task AllUnits_NoHqService_PlainUserDenied()
        => await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            Behavior(withHq: false).Handle(Q(ReportUnitScopeMode.AllUnits), Next, default));

    // ---------------- UnitCategories
    [Theory]
    [InlineData("1", UnitCategory.Insurance)]
    [InlineData("9", UnitCategory.Insurance)]
    [InlineData(" 2 ", UnitCategory.Medical)]
    [InlineData("16", UnitCategory.Medical)]
    [InlineData("10", UnitCategory.Headquarters)]
    [InlineData("17", UnitCategory.Headquarters)]
    public void Categories_Of_MapsKnownTypeCodes(string code, UnitCategory expected)
        => Assert.Equal(expected, UnitCategories.Of(code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("99")]
    public void Categories_Of_UnknownIsNull(string? code)
        => Assert.Null(UnitCategories.Of(code));

    [Fact]
    public void Categories_Label_AllEnumValuesHaveLabel()
    {
        Assert.Equal("بیمه‌ای", UnitCategories.Label(UnitCategory.Insurance));
        Assert.Equal("درمانی", UnitCategories.Label(UnitCategory.Medical));
        Assert.Equal("ستادی", UnitCategories.Label(UnitCategory.Headquarters));
    }
}

public class HeadquartersAccessServiceTests
{
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IUnitAccessReadRepository> _unitAccess = new();

    private void HqAdmin()
    {
        _user.Setup(u => u.IsInRole(AppRoles.SetadAdmin)).Returns(true);
        _user.Setup(u => u.VahedCode).Returns("0001");
        _unitAccess.Setup(r => r.GetUnitProfileAsync("0001", It.IsAny<CancellationToken>())).ReturnsAsync(new UnitProfile("ستاد", true));
    }

    [Fact]
    public async Task IsHeadquartersAdmin_RequiresRoleAndHeadquartersUnit()
    {
        Assert.False(await HeadquartersAccess.IsHeadquartersAdminAsync(_user.Object, _unitAccess.Object, default));

        _user.Setup(u => u.IsInRole(AppRoles.SetadAdmin)).Returns(true);
        _user.Setup(u => u.VahedCode).Returns("");
        Assert.False(await HeadquartersAccess.IsHeadquartersAdminAsync(_user.Object, _unitAccess.Object, default));

        _user.Setup(u => u.VahedCode).Returns("0002");
        _unitAccess.Setup(r => r.GetUnitProfileAsync("0002", It.IsAny<CancellationToken>())).ReturnsAsync(new UnitProfile("A", false));
        Assert.False(await HeadquartersAccess.IsHeadquartersAdminAsync(_user.Object, _unitAccess.Object, default));

        HqAdmin();
        Assert.True(await HeadquartersAccess.IsHeadquartersAdminAsync(_user.Object, _unitAccess.Object, default));
    }

    [Fact]
    public async Task CategoryOf_UnknownUnit_Null()
    {
        _unitAccess.Setup(r => r.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UnitNode>
        {
            new(Guid.NewGuid(), "0002", "A", null, false, "2"),
        });
        Assert.Equal(UnitCategory.Medical, await HeadquartersAccess.CategoryOfAsync("0002", _unitAccess.Object, default));
        Assert.Null(await HeadquartersAccess.CategoryOfAsync("0009", _unitAccess.Object, default));
    }

    [Fact]
    public async Task CanSeeCountry_NationalOrHqAdmin_OthersNot()
    {
        var plain = new HeadquartersAccessService(_user.Object, _unitAccess.Object);
        Assert.False(await plain.CanSeeCountryAsync());

        var nat = new Mock<ICurrentUser>();
        nat.Setup(u => u.IsInRole(AppRoles.National)).Returns(true);
        Assert.True(await new HeadquartersAccessService(nat.Object, _unitAccess.Object).CanSeeCountryAsync());

        HqAdmin();
        Assert.True(await new HeadquartersAccessService(_user.Object, _unitAccess.Object).CanSeeCountryAsync());
    }

    [Fact]
    public async Task HasAbility_UnknownAbility_Throws_ForNonHq()
        => await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new HeadquartersAccessService(_user.Object, _unitAccess.Object).HasAbilityAsync("no.such.ability"));

    [Fact]
    public async Task HasAbility_HqAdmin_AlwaysTrue()
    {
        HqAdmin();
        var s = new HeadquartersAccessService(_user.Object, _unitAccess.Object);
        Assert.True(await s.HasAbilityAsync(AbilityCatalog.All.First().Key));
        Assert.Equal(AbilityCatalog.All.Count, (await s.GrantedAbilitiesAsync()).Count);
    }

    [Fact]
    public async Task HasAbility_WithConfiguredTable_FollowsTableLevel()
    {
        var ability = AbilityCatalog.All.First();
        _user.Setup(u => u.AllRoles).Returns(new List<string> { "custom" });
        var store = new Mock<IRoleMenuAccessStore>();
        store.Setup(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleMenuAccessSnapshot([("custom", AbilityCatalog.StorageKey(ability.Key), RoleMenuAccessLevels.View)]));
        Assert.True(await new HeadquartersAccessService(_user.Object, _unitAccess.Object, store.Object).HasAbilityAsync(ability.Key));

        store.Setup(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleMenuAccessSnapshot([("custom", AbilityCatalog.StorageKey(ability.Key), RoleMenuAccessLevels.None)]));
        Assert.False(await new HeadquartersAccessService(_user.Object, _unitAccess.Object, store.Object).HasAbilityAsync(ability.Key));
    }
}
