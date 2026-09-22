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
            return own;
        }

        // Length-check the untrusted value before it reaches a query. Same 4-char Legacy ceiling
        // the behavior has always enforced on the claim.
        if (requested.Length > VahedScopeBehavior<object, object>.MaxVahedCodeLength)
        {
            throw new UnitActAsDeniedException(requested, own);
        }

        if (string.Equals(requested, own, StringComparison.Ordinal))
        {
            return own;
        }

        if (!await _unitAccess.CanActAsAsync(own, requested, cancellationToken))
        {
            throw new UnitActAsDeniedException(requested, own);
        }

        return requested;
    }
}
