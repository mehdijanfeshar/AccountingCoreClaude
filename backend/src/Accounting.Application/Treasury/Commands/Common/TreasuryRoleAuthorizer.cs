using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

public sealed class TreasuryRoleAuthorizer : ITreasuryRoleAuthorizer
{
    private readonly ITreasuryRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser;

    public TreasuryRoleAuthorizer(ITreasuryRoleRepository roleRepository, ICurrentUser currentUser)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<TreasuryRole>> EnsureHasRoleAsync(
        string vahedCode,
        IReadOnlyCollection<TreasuryRole> allowedRoles,
        CancellationToken cancellationToken = default)
    {
        var activeRoles = await _roleRepository.GetActiveRolesAsync(vahedCode, _currentUser.UserId, cancellationToken);

        var matched = activeRoles.Where(allowedRoles.Contains).ToList();

        if (matched.Count == 0)
        {
            throw new TreasuryRoleRequiredException(vahedCode, allowedRoles.First());
        }

        return matched;
    }
}
