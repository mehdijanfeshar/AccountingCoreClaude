using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Accounting.Application.RoleAccess;

// «دسترسی نقش‌ها» (فاز ۵۴، DDL 075): مدیر ستاد برای هر نقش تعیین می‌کند هر منو را نبیند / ببیند / ثبت و تغییر کند.
// همه فقط مدیر ستاد (ماژول RoleAccess در AppRoles.ReferenceModules برای نوشتن؛ Query این‌جا خودش کنترل می‌کند).

public sealed record RoleAccessMenuDto(string Key, string Group, string Title, bool HasWrite);

/// <param name="Key">کلید ذخیره (<c>ability:…</c>) — همان کلید ماتریس.</param>
public sealed record RoleAccessAbilityDto(string Key, string Group, string? MenuKey, string Title);

public sealed record RoleAccessRoleDto(string Name, string? Label, bool BuiltIn, bool Configured);

/// <param name="Available">DDL 075 اجرا شده است.</param>
/// <param name="Configured">جدول ردیف دارد ⇒ سرور و منو از آن پیروی می‌کنند؛ وگرنه رفتار ثابت قبلی.</param>
/// <param name="Matrix">نقش ⇒ (کلید منو ⇒ سطح). برای نقش ثابت بی‌ردیف، پیش‌فرض فعلی.</param>
public sealed record RoleAccessDto(
    bool Available,
    bool Configured,
    IReadOnlyList<RoleAccessRoleDto> Roles,
    IReadOnlyList<RoleAccessMenuDto> Menus,
    IReadOnlyList<RoleAccessAbilityDto> Abilities,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> Matrix);

internal static class RoleAccessGuard
{
    public static void EnsureSetadAdmin(ICurrentUser user)
    {
        if (!user.IsInRole(AppRoles.SetadAdmin))
            throw new RoleAccessDeniedException("دسترسی نقش‌ها را فقط «مدیر ستاد» می‌تواند ببیند و تغییر دهد.");
    }

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [AppRoles.SetadAdmin] = "مدیر ستاد",
        [AppRoles.MaliAdmin] = "مسئول حسابداری",
        [AppRoles.HltAdmin] = "مدیر درمانی",
        [AppRoles.EdkAdmin] = "مدیر بیمه‌ای",
        [AppRoles.User] = "کارمند حسابداری",
        [AppRoles.Report] = "گزارش‌گیری و حسابرسی",
        [AppRoles.It] = "فناوری اطلاعات",
        [AppRoles.National] = "مدیریتی سطح کشور",
    };

    /// <summary>همهٔ کلیدهای ماتریس: منوها و قابلیت‌ها (<c>ability:…</c>).</summary>
    public static IEnumerable<string> AllKeys
        => MenuCatalog.All.Select(m => m.Key).Concat(AbilityCatalog.All.Select(a => AbilityCatalog.StorageKey(a.Key)));

    public static Dictionary<string, int> Defaults(string role)
    {
        var levels = MenuCatalog.All.ToDictionary(m => m.Key, m => MenuCatalog.DefaultLevel(role, m), StringComparer.Ordinal);
        foreach (var a in AbilityCatalog.All)
            levels[AbilityCatalog.StorageKey(a.Key)] = AbilityCatalog.DefaultFor(role, a) ? RoleMenuAccessLevels.View : RoleMenuAccessLevels.None;
        return levels;
    }

    public static bool IsKnownKey(string key)
        => MenuCatalog.Contains(key)
            || (AbilityCatalog.IsStorageKey(key) && AbilityCatalog.Contains(key[AbilityCatalog.StoragePrefix.Length..]));

    /// <summary>سقف سطح هر کلید: قابلیت ۰/۱، منوی فقط‌خواندنی ۰/۱، بقیه ۰..۲.</summary>
    public static int MaxLevel(string key)
        => AbilityCatalog.IsStorageKey(key) || MenuCatalog.All.First(m => m.Key == key).HasWrite is false
            ? RoleMenuAccessLevels.View
            : RoleMenuAccessLevels.Edit;
}

public sealed record GetRoleAccessQuery : IRequest<RoleAccessDto>;

public sealed class GetRoleAccessQueryHandler : IRequestHandler<GetRoleAccessQuery, RoleAccessDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoleMenuAccessStore _store;

    public GetRoleAccessQueryHandler(ICurrentUser currentUser, IRoleMenuAccessStore store)
    {
        _currentUser = currentUser;
        _store = store;
    }

    public async Task<RoleAccessDto> Handle(GetRoleAccessQuery request, CancellationToken cancellationToken)
    {
        RoleAccessGuard.EnsureSetadAdmin(_currentUser);
        var available = await _store.IsAvailableAsync(cancellationToken);
        var snapshot = available ? await _store.GetSnapshotAsync(cancellationToken) : null;

        var names = AppRoles.All.Where(r => r != AppRoles.SetadAdmin)
            .Concat(snapshot?.Roles.Where(r => !AppRoles.All.Contains(r, StringComparer.OrdinalIgnoreCase)).OrderBy(r => r) ?? Enumerable.Empty<string>())
            .ToList();

        var roles = names.Select(n => new RoleAccessRoleDto(
                n,
                RoleAccessGuard.Labels.GetValueOrDefault(n),
                AppRoles.All.Contains(n),
                snapshot?.IsConfiguredRole(n) ?? false))
            .ToList();

        var matrix = names.ToDictionary(
            n => n,
            n => (IReadOnlyDictionary<string, int>)(snapshot is not null && snapshot.IsConfiguredRole(n)
                ? RoleAccessGuard.AllKeys.ToDictionary(k => k, k => snapshot.ForRole(n).GetValueOrDefault(k), StringComparer.Ordinal)
                : AppRoles.All.Contains(n) ? RoleAccessGuard.Defaults(n) : RoleAccessGuard.AllKeys.ToDictionary(k => k, _ => 0, StringComparer.Ordinal)),
            StringComparer.OrdinalIgnoreCase);

        return new RoleAccessDto(
            available,
            snapshot is not null,
            roles,
            MenuCatalog.All.Select(m => new RoleAccessMenuDto(m.Key, m.Group, m.Title, m.HasWrite)).ToList(),
            AbilityCatalog.All.Select(a => new RoleAccessAbilityDto(AbilityCatalog.StorageKey(a.Key), a.Group, a.MenuKey, a.Title)).ToList(),
            matrix);
    }
}

/// <param name="Role">نام نقش عین claim توکن (Keycloak: نام Client Role روی accounting-api).</param>
/// <param name="Levels">کلید منو ⇒ سطح؛ منوهای نیامده ۰ می‌شوند.</param>
public sealed record SaveRoleAccessCommand(string Role, IReadOnlyDictionary<string, int> Levels) : IRequest;

public sealed class SaveRoleAccessCommandValidator : AbstractValidator<SaveRoleAccessCommand>
{
    public SaveRoleAccessCommandValidator()
    {
        RuleFor(x => x.Role).NotEmpty().MaximumLength(100)
            .Must(r => r is null || !string.Equals(r.Trim(), AppRoles.SetadAdmin, StringComparison.OrdinalIgnoreCase))
            .WithMessage("مدیر ستاد همیشه دسترسی کامل دارد و قابل تنظیم نیست.");
        RuleFor(x => x.Levels).NotNull();
        RuleForEach(x => x.Levels).Must(kv => RoleAccessGuard.IsKnownKey(kv.Key))
            .WithMessage("منو یا قابلیت ناشناخته.");
        RuleForEach(x => x.Levels).Must(kv => kv.Value is >= RoleMenuAccessLevels.None and <= RoleMenuAccessLevels.Edit)
            .WithMessage("سطح دسترسی باید ۰، ۱ یا ۲ باشد.");
    }
}

public sealed class SaveRoleAccessCommandHandler : IRequestHandler<SaveRoleAccessCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoleMenuAccessStore _store;

    public SaveRoleAccessCommandHandler(ICurrentUser currentUser, IRoleMenuAccessStore store)
    {
        _currentUser = currentUser;
        _store = store;
    }

    public async Task Handle(SaveRoleAccessCommand request, CancellationToken cancellationToken)
    {
        RoleAccessGuard.EnsureSetadAdmin(_currentUser);
        await EnsureAvailableAsync(_store, cancellationToken);
        // منوی فقط‌خواندنی «ثبت و تغییر» ندارد؛ قابلیت فقط دارد/ندارد.
        var levels = RoleAccessGuard.AllKeys.ToDictionary(
            k => k,
            k => Math.Min(request.Levels.GetValueOrDefault(k), RoleAccessGuard.MaxLevel(k)),
            StringComparer.Ordinal);
        await _store.SaveRoleAsync(request.Role.Trim(), levels, _currentUser.UserId, cancellationToken);
    }

    internal static async Task EnsureAvailableAsync(IRoleMenuAccessStore store, CancellationToken cancellationToken)
    {
        if (!await store.IsAvailableAsync(cancellationToken))
            throw new BusinessRuleException("جدول دسترسی نقش‌ها هنوز ساخته نشده است (اسکریپت 075 را اجرا کنید).");
    }
}

/// <summary>
/// پر کردن جدول با رفتار فعلی برای ۷ نقش ثابت (به‌جز مدیر ستاد) — نقطهٔ شروع پیکربندی؛ پس از آن سرور و منو از
/// جدول پیروی می‌کنند. نقش‌های ثابتی که از قبل ردیف دارند بازنویسی می‌شوند.
/// </summary>
public sealed record SeedRoleAccessDefaultsCommand : IRequest;

public sealed class SeedRoleAccessDefaultsCommandHandler : IRequestHandler<SeedRoleAccessDefaultsCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoleMenuAccessStore _store;

    public SeedRoleAccessDefaultsCommandHandler(ICurrentUser currentUser, IRoleMenuAccessStore store)
    {
        _currentUser = currentUser;
        _store = store;
    }

    public async Task Handle(SeedRoleAccessDefaultsCommand request, CancellationToken cancellationToken)
    {
        RoleAccessGuard.EnsureSetadAdmin(_currentUser);
        await SaveRoleAccessCommandHandler.EnsureAvailableAsync(_store, cancellationToken);
        foreach (var role in AppRoles.All.Where(r => r != AppRoles.SetadAdmin))
            await _store.SaveRoleAsync(role, RoleAccessGuard.Defaults(role), _currentUser.UserId, cancellationToken);
    }
}

public sealed record RemoveRoleAccessCommand(string Role) : IRequest;

public sealed class RemoveRoleAccessCommandHandler : IRequestHandler<RemoveRoleAccessCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoleMenuAccessStore _store;

    public RemoveRoleAccessCommandHandler(ICurrentUser currentUser, IRoleMenuAccessStore store)
    {
        _currentUser = currentUser;
        _store = store;
    }

    public async Task Handle(RemoveRoleAccessCommand request, CancellationToken cancellationToken)
    {
        RoleAccessGuard.EnsureSetadAdmin(_currentUser);
        await SaveRoleAccessCommandHandler.EnsureAvailableAsync(_store, cancellationToken);
        await _store.DeleteRoleAsync(request.Role.Trim(), cancellationToken);
    }
}
