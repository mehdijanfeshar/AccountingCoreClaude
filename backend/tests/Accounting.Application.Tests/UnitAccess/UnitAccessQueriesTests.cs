using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Queries.UnitAccess;
using Accounting.Application.Queries.UnitAccess.GetAccessibleUnits;
using Accounting.Application.Queries.UnitAccess.GetCurrentUser;
using Moq;

namespace Accounting.Application.Tests.UnitAccess;

public class UnitAccessQueriesTests
{
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IUnitAccessReadRepository> _repo = new();

    public UnitAccessQueriesTests()
    {
        _user.Setup(u => u.UserId).Returns("u1");
        _user.Setup(u => u.FinancialRoles).Returns(new List<string>());
        _user.Setup(u => u.AllRoles).Returns(new List<string>());
        _user.Setup(u => u.TokenDiagnostics).Returns(new List<string>());
    }

    // ---------------- GetAccessibleUnits
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Accessible_MissingClaim_ThrowsMissingScope(string? vahed)
    {
        _user.Setup(u => u.VahedCode).Returns(vahed);
        var h = new GetAccessibleUnitsQueryHandler(_user.Object, _repo.Object);
        await Assert.ThrowsAsync<MissingVahedScopeException>(() => h.Handle(new GetAccessibleUnitsQuery(), default));
    }

    [Fact]
    public async Task Accessible_UnknownUnit_ThrowsUnknownCaller_NotEmptyList()
    {
        _user.Setup(u => u.VahedCode).Returns("0099");
        _repo.Setup(r => r.GetAccessibleUnitsAsync("0099", It.IsAny<CancellationToken>())).ReturnsAsync(new List<AccessibleUnitDto>());
        var h = new GetAccessibleUnitsQueryHandler(_user.Object, _repo.Object);
        await Assert.ThrowsAsync<UnknownCallerUnitException>(() => h.Handle(new GetAccessibleUnitsQuery(), default));
    }

    [Fact]
    public async Task Accessible_ReturnsRepositoryList()
    {
        _user.Setup(u => u.VahedCode).Returns("0002");
        IReadOnlyList<AccessibleUnitDto> units = new List<AccessibleUnitDto> { new(Guid.NewGuid(), "0002", "x", null, true) };
        _repo.Setup(r => r.GetAccessibleUnitsAsync("0002", It.IsAny<CancellationToken>())).ReturnsAsync(units);
        var res = await new GetAccessibleUnitsQueryHandler(_user.Object, _repo.Object).Handle(new GetAccessibleUnitsQuery(), default);
        Assert.Same(units, res);
    }

    // ---------------- GetCurrentUser
    [Fact]
    public async Task CurrentUser_NoVahedClaim_ReturnsIdentityWithoutThrowing()
    {
        _user.Setup(u => u.VahedCode).Returns((string?)null);
        var r = await new GetCurrentUserQueryHandler(_user.Object, _repo.Object).Handle(new GetCurrentUserQuery(), default);
        Assert.Equal("u1", r.UserId);
        Assert.Null(r.VahedCode);
        Assert.False(r.IsHeadquarters);
        _repo.Verify(x => x.GetUnitProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CurrentUser_UnknownProfile_NameNullAndNotHq()
    {
        _user.Setup(u => u.VahedCode).Returns("0099");
        _repo.Setup(x => x.GetUnitProfileAsync("0099", It.IsAny<CancellationToken>())).ReturnsAsync((UnitProfile?)null);
        var r = await new GetCurrentUserQueryHandler(_user.Object, _repo.Object).Handle(new GetCurrentUserQuery(), default);
        Assert.Equal("0099", r.VahedCode);
        Assert.Null(r.VahedName);
        Assert.False(r.IsHeadquarters);
    }

    [Fact]
    public async Task CurrentUser_WithoutStore_MenuAccessNull_AuditByRole()
    {
        _user.Setup(u => u.VahedCode).Returns("0001");
        _repo.Setup(x => x.GetUnitProfileAsync("0001", It.IsAny<CancellationToken>())).ReturnsAsync(new UnitProfile("ستاد", true));
        _user.Setup(u => u.IsInRole(AppRoles.Audit)).Returns(true);
        var r = await new GetCurrentUserQueryHandler(_user.Object, _repo.Object).Handle(new GetCurrentUserQuery(), default);
        Assert.Equal("ستاد", r.VahedName);
        Assert.True(r.IsHeadquarters);
        Assert.Null(r.MenuAccess);
        Assert.True(r.CanViewAuditLog);
    }

    [Fact]
    public async Task CurrentUser_WithoutStore_NonAuditRole_CannotViewAudit()
    {
        _user.Setup(u => u.VahedCode).Returns("0002");
        var r = await new GetCurrentUserQueryHandler(_user.Object, _repo.Object).Handle(new GetCurrentUserQuery(), default);
        Assert.False(r.CanViewAuditLog);
    }

    [Fact]
    public async Task CurrentUser_SetadAdmin_GetsEditEverywhereExceptAuditFollowsTable()
    {
        _user.Setup(u => u.VahedCode).Returns("0001");
        _user.Setup(u => u.IsInRole(AppRoles.SetadAdmin)).Returns(true);
        _user.Setup(u => u.AllRoles).Returns(new List<string> { AppRoles.SetadAdmin });
        // جدول فقط برای نقش دیگری ردیف دارد؛ مدیر ستاد لاگ را نمی‌بیند (نقش Audit جداست)
        var store = new Mock<IRoleMenuAccessStore>();
        store.Setup(s => s.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleMenuAccessSnapshot([(AppRoles.User, MenuCatalog.All.First().Key, 1)]));

        var r = await new GetCurrentUserQueryHandler(_user.Object, _repo.Object, store.Object).Handle(new GetCurrentUserQuery(), default);

        var other = MenuCatalog.All.First(m => m.Key != MenuCatalog.AuditLogKey).Key;
        Assert.Equal(RoleMenuAccessLevels.Edit, r.MenuAccess![other]);
        Assert.Equal(RoleMenuAccessLevels.None, r.MenuAccess[MenuCatalog.AuditLogKey]);
        Assert.False(r.CanViewAuditLog);
    }

    [Fact]
    public async Task CurrentUser_ConfiguredCustomRole_AppearsInRoles()
    {
        _user.Setup(u => u.VahedCode).Returns("0002");
        _user.Setup(u => u.AllRoles).Returns(new List<string> { "custom", "other" });
        var store = new Mock<IRoleMenuAccessStore>();
        var menu = MenuCatalog.All.First(m => m.Key != MenuCatalog.AuditLogKey).Key;
        store.Setup(s => s.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleMenuAccessSnapshot([("custom", menu, 2)]));

        var r = await new GetCurrentUserQueryHandler(_user.Object, _repo.Object, store.Object).Handle(new GetCurrentUserQuery(), default);

        Assert.Contains("custom", r.Roles!);
        Assert.DoesNotContain("other", r.Roles!);
        Assert.Equal(2, r.MenuAccess![menu]);
    }
}
