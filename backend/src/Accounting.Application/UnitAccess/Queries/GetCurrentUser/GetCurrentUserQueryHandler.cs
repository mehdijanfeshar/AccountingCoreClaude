using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.UnitAccess.Queries;
using MediatR;

namespace Accounting.Application.UnitAccess.Queries.GetCurrentUser;

/// <summary>
/// Combines the token-derived identity (<see cref="ICurrentUser"/>) with the unit row that the
/// token's org claim names.
/// </summary>
public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitAccessReadRepository _readRepository;
    private readonly IRoleMenuAccessStore? _accessStore;
    private readonly IHeadquartersAccessService? _hq;

    public GetCurrentUserQueryHandler(
        ICurrentUser currentUser,
        IUnitAccessReadRepository readRepository,
        IRoleMenuAccessStore? accessStore = null,
        IHeadquartersAccessService? hq = null)
    {
        _currentUser = currentUser;
        _readRepository = readRepository;
        _accessStore = accessStore;
        _hq = hq;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var vahedCode = _currentUser.VahedCode;
        var (roles, menuAccess) = await RolesAndMenuAccessAsync(cancellationToken);
        var abilities = _hq is null ? null : await _hq.GrantedAbilitiesAsync(cancellationToken);

        // Unlike GetAccessibleUnitsQuery this does NOT throw on a missing org claim. This endpoint
        // is what a client calls to find out what state it is in; answering "you are authenticated
        // but your token names no unit" is strictly more useful than a 403 that looks identical to
        // a permissions problem.
        if (string.IsNullOrWhiteSpace(vahedCode))
        {
            return new CurrentUserDto(_currentUser.UserId, VahedCode: null, VahedName: null, IsHeadquarters: false, roles, _currentUser.TokenDiagnostics, menuAccess, abilities);
        }

        var profile = await _readRepository.GetUnitProfileAsync(vahedCode, cancellationToken);

        return new CurrentUserDto(
            _currentUser.UserId,
            vahedCode,
            profile?.VahedName,
            profile?.IsHeadquarters ?? false,
            roles,
            _currentUser.TokenDiagnostics,
            menuAccess,
            abilities);
    }

    /// <summary>
    /// نقش‌های مالی = ۸ نقش ثابت به‌علاوهٔ نقش‌هایی که در «دسترسی نقش‌ها» تعریف شده‌اند؛ و سطح هر منو وقتی جدول پیکربندی
    /// شده (مدیر ستاد: همه «ثبت و تغییر»).
    /// </summary>
    private async Task<(IReadOnlyList<string> Roles, IReadOnlyDictionary<string, int>? MenuAccess)> RolesAndMenuAccessAsync(
        CancellationToken cancellationToken)
    {
        var fixedRoles = _currentUser.FinancialRoles;
        var snapshot = _accessStore is null ? null : await _accessStore.GetSnapshotAsync(cancellationToken);
        if (snapshot is null)
            return (fixedRoles, null);

        var all = _currentUser.AllRoles;
        var roles = fixedRoles.Concat(all.Where(snapshot.IsConfiguredRole))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var menuAccess = _currentUser.IsInRole(AppRoles.SetadAdmin)
            ? MenuCatalog.All.ToDictionary(m => m.Key, _ => RoleMenuAccessLevels.Edit, StringComparer.Ordinal)
            : snapshot.MenuAccessFor(all);
        return (roles, menuAccess);
    }
}
