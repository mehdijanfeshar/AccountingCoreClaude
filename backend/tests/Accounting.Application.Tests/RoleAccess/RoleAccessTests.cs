using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Security;
using Accounting.Application.RoleAccess;
using Moq;

namespace Accounting.Application.Tests.RoleAccess;

public class RoleAccessTests
{
    private readonly Mock<Accounting.Application.Common.Interfaces.ICurrentUser> _user = new();
    private readonly Mock<IRoleMenuAccessStore> _store = new();

    public RoleAccessTests()
    {
        _user.Setup(u => u.UserId).Returns("u1");
        _store.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private void AsSetadAdmin() => _user.Setup(u => u.IsInRole(AppRoles.SetadAdmin)).Returns(true);

    // ---------------- Validator
    [Fact]
    public void Validator_SetadAdminRole_Rejected_CaseInsensitive()
    {
        var v = new SaveRoleAccessCommandValidator();
        Assert.False(v.Validate(new SaveRoleAccessCommand(AppRoles.SetadAdmin, new Dictionary<string, int>())).IsValid);
        Assert.False(v.Validate(new SaveRoleAccessCommand(" " + AppRoles.SetadAdmin.ToLowerInvariant() + " ", new Dictionary<string, int>())).IsValid);
        Assert.True(v.Validate(new SaveRoleAccessCommand(AppRoles.User, new Dictionary<string, int>())).IsValid);
    }

    [Fact]
    public void Validator_EmptyOrLongRole_Rejected()
    {
        var v = new SaveRoleAccessCommandValidator();
        Assert.False(v.Validate(new SaveRoleAccessCommand("", new Dictionary<string, int>())).IsValid);
        Assert.False(v.Validate(new SaveRoleAccessCommand(new string('r', 101), new Dictionary<string, int>())).IsValid);
    }

    [Fact]
    public void Validator_UnknownKeyOrBadLevel_Rejected()
    {
        var v = new SaveRoleAccessCommandValidator();
        var menu = MenuCatalog.All.First().Key;
        Assert.False(v.Validate(new SaveRoleAccessCommand("r", new Dictionary<string, int> { ["/nope"] = 1 })).IsValid);
        Assert.False(v.Validate(new SaveRoleAccessCommand("r", new Dictionary<string, int> { [menu] = 3 })).IsValid);
        Assert.False(v.Validate(new SaveRoleAccessCommand("r", new Dictionary<string, int> { [menu] = -1 })).IsValid);
        Assert.True(v.Validate(new SaveRoleAccessCommand("r", new Dictionary<string, int> { [menu] = 2 })).IsValid);
    }

    // ---------------- Get
    [Fact]
    public async Task Get_NotSetadAdmin_Throws()
        => await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            new GetRoleAccessQueryHandler(_user.Object, _store.Object).Handle(new GetRoleAccessQuery(), default));

    [Fact]
    public async Task Get_TableUnavailable_ReturnsDefaultsForBuiltInRolesWithoutSetad()
    {
        AsSetadAdmin();
        _store.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var r = await new GetRoleAccessQueryHandler(_user.Object, _store.Object).Handle(new GetRoleAccessQuery(), default);

        Assert.False(r.Available);
        Assert.False(r.Configured);
        Assert.DoesNotContain(r.Roles, x => x.Name == AppRoles.SetadAdmin);
        Assert.Equal(AppRoles.All.Length - 1, r.Roles.Count);
        Assert.All(r.Roles, x => { Assert.True(x.BuiltIn); Assert.False(x.Configured); });
        Assert.True(r.Matrix.ContainsKey(AppRoles.User));
        _store.Verify(s => s.GetSnapshotAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Get_CustomRoleFromSnapshot_AppearsNotBuiltIn_WithStoredLevels()
    {
        AsSetadAdmin();
        var menu = MenuCatalog.All.First().Key;
        _store.Setup(s => s.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleMenuAccessSnapshot([("custom-role", menu, RoleMenuAccessLevels.Edit)]));

        var r = await new GetRoleAccessQueryHandler(_user.Object, _store.Object).Handle(new GetRoleAccessQuery(), default);

        Assert.True(r.Configured);
        var custom = r.Roles.Single(x => x.Name == "custom-role");
        Assert.False(custom.BuiltIn);
        Assert.True(custom.Configured);
        Assert.Equal(RoleMenuAccessLevels.Edit, r.Matrix["custom-role"][menu]);
        // سایر کلیدها برای نقش پیکربندی‌شده صفر است
        Assert.Equal(0, r.Matrix["custom-role"].Where(kv => kv.Key != menu && !kv.Key.StartsWith(menu + "#")).Max(kv => kv.Value));
    }

    // ---------------- Save
    private SaveRoleAccessCommandHandler Save() => new(_user.Object, _store.Object);

    [Fact]
    public async Task Save_NotSetadAdmin_Throws_NothingSaved()
    {
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() => Save().Handle(new SaveRoleAccessCommand("r", new Dictionary<string, int>()), default));
        _store.Verify(s => s.SaveRoleAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Save_TableUnavailable_ThrowsBusinessRule()
    {
        AsSetadAdmin();
        _store.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Save().Handle(new SaveRoleAccessCommand("r", new Dictionary<string, int>()), default));
    }

    [Fact]
    public async Task Save_ClampsReadOnlyMenusAndAbilitiesToView_TrimsRole_OmitsUnsentTabs()
    {
        AsSetadAdmin();
        var readOnly = MenuCatalog.All.First(m => !m.HasWrite).Key;
        var writable = MenuCatalog.All.First(m => m.HasWrite).Key;
        var ability = AbilityCatalog.StorageKey(AbilityCatalog.All.First().Key);
        var tab = MenuCatalog.Tabs.First().Key;
        IReadOnlyDictionary<string, int>? saved = null;
        string? savedRole = null;
        _store.Setup(s => s.SaveRoleAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, int>>(), "u1", It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, int>, string, CancellationToken>((r, l, _, _) => { savedRole = r; saved = l; })
            .Returns(Task.CompletedTask);

        await Save().Handle(new SaveRoleAccessCommand(" my-role ", new Dictionary<string, int>
        {
            [readOnly] = 2, [writable] = 2, [ability] = 2,
        }), default);

        Assert.Equal("my-role", savedRole);
        Assert.Equal(RoleMenuAccessLevels.View, saved![readOnly]);
        Assert.Equal(RoleMenuAccessLevels.Edit, saved[writable]);
        Assert.Equal(RoleMenuAccessLevels.View, saved[ability]);
        Assert.False(saved.ContainsKey(tab));
        // منوی نیامده صفر ذخیره می‌شود
        var other = MenuCatalog.All.First(m => m.Key != readOnly && m.Key != writable).Key;
        Assert.Equal(0, saved[other]);
    }

    [Fact]
    public async Task Save_TabSentInRequest_IsStored()
    {
        AsSetadAdmin();
        var tab = MenuCatalog.Tabs.First().Key;
        IReadOnlyDictionary<string, int>? saved = null;
        _store.Setup(s => s.SaveRoleAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, int>, string, CancellationToken>((_, l, _, _) => saved = l)
            .Returns(Task.CompletedTask);
        await Save().Handle(new SaveRoleAccessCommand("r", new Dictionary<string, int> { [tab] = 1 }), default);
        Assert.Equal(1, saved![tab]);
    }

    // ---------------- Seed / Remove
    [Fact]
    public async Task Seed_SavesDefaultsForEveryBuiltInRoleExceptSetad()
    {
        AsSetadAdmin();
        var roles = new List<string>();
        _store.Setup(s => s.SaveRoleAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, int>>(), "u1", It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, int>, string, CancellationToken>((r, _, _, _) => roles.Add(r))
            .Returns(Task.CompletedTask);

        await new SeedRoleAccessDefaultsCommandHandler(_user.Object, _store.Object).Handle(new SeedRoleAccessDefaultsCommand(), default);

        Assert.Equal(AppRoles.All.Where(r => r != AppRoles.SetadAdmin).OrderBy(x => x), roles.OrderBy(x => x));
    }

    [Fact]
    public async Task Seed_NotSetadAdmin_Throws()
        => await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            new SeedRoleAccessDefaultsCommandHandler(_user.Object, _store.Object).Handle(new SeedRoleAccessDefaultsCommand(), default));

    [Fact]
    public async Task Remove_TrimsRoleAndDeletes()
    {
        AsSetadAdmin();
        _store.Setup(s => s.DeleteRoleAsync("r", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await new RemoveRoleAccessCommandHandler(_user.Object, _store.Object).Handle(new RemoveRoleAccessCommand(" r "), default);
        _store.Verify(s => s.DeleteRoleAsync("r", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_TableUnavailable_Throws()
    {
        AsSetadAdmin();
        _store.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new RemoveRoleAccessCommandHandler(_user.Object, _store.Object).Handle(new RemoveRoleAccessCommand("r"), default));
    }
}
