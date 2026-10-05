using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.Common.Security;

/// <summary>
/// Default <see cref="IUnitScopeResolver"/>. See that interface for the decision table.
/// </summary>
public sealed class UnitScopeResolver : IUnitScopeResolver
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitAccessReadRepository _unitAccess;

    public UnitScopeResolver(ICurrentUser currentUser, IUnitAccessReadRepository unitAccess)
    {
        _currentUser = currentUser;
        _unitAccess = unitAccess;
    }

    public async Task<string> ResolveEffectiveVahedCodeAsync(CancellationToken cancellationToken = default)
        => (await ResolveAsync(cancellationToken)).VahedCode;

    public async Task<UnitScopeResolution> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var own = _currentUser.VahedCode;

        // Identical fail-loud contract to the pre-37 VahedScopeBehavior: a token with no usable
        // unit claim can never produce a scope, requested unit or not.
        if (string.IsNullOrWhiteSpace(own) || own.Length > VahedScopeBehavior<object, object>.MaxVahedCodeLength)
        {
            throw new MissingVahedScopeException(nameof(UnitScopeResolver));
        }

        var requested = _currentUser.RequestedVahedCode;

        // No unit requested → behave exactly as before phase 37-B. Every existing client, and
        // every server-to-server caller, lands here.
        if (string.IsNullOrWhiteSpace(requested))
        {
            return new UnitScopeResolution(own, ViewOnly: false);
        }

        // Length-check the untrusted value before it reaches a query. Same 4-char Legacy ceiling
        // the behavior has always enforced on the claim.
        if (requested.Length > VahedScopeBehavior<object, object>.MaxVahedCodeLength)
        {
            throw new UnitActAsDeniedException(requested, own);
        }

        if (string.Equals(requested, own, StringComparison.Ordinal))
        {
            return new UnitScopeResolution(own, ViewOnly: false);
        }

        if (await _unitAccess.CanActAsAsync(own, requested, cancellationToken))
        {
            return new UnitScopeResolution(requested, ViewOnly: false);
        }

        // نقش مدیریتی سطح کشور (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۵): هر واحد موجود، ولی فقط مشاهده —
        // VahedScopeBehavior فرمان‌ها را در این حالت رد می‌کند.
        if (_currentUser.IsInRole(AppRoles.National)
            && await _unitAccess.GetUnitProfileAsync(requested, cancellationToken) is not null)
        {
            return new UnitScopeResolution(requested, ViewOnly: true);
        }

        throw new UnitActAsDeniedException(requested, own);
    }
}
