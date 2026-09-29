using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

public sealed class TreasuryTreasurerAuthorizer : ITreasuryTreasurerAuthorizer
{
    private readonly ITreasuryRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser;

    public TreasuryTreasurerAuthorizer(ITreasuryRoleRepository roleRepository, ICurrentUser currentUser)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
    }

    public async Task EnsureTreasurerAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        var activeRoles = await _roleRepository.GetActiveRolesAsync(vahedCode, _currentUser.UserId, cancellationToken);

        if (!activeRoles.Contains(TreasuryRole.Treasurer))
        {
            throw new TreasuryTreasurerRoleRequiredException();
        }
    }
}
