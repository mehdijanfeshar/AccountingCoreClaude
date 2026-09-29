using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

public sealed class PaymentRequestExecutionAuthorizer : IPaymentRequestExecutionAuthorizer
{
    private readonly ITreasuryRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser;

    public PaymentRequestExecutionAuthorizer(ITreasuryRoleRepository roleRepository, ICurrentUser currentUser)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
    }

    public async Task EnsureTreasurerAsync(Guid paymentRequestId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var activeRoles = await _roleRepository.GetActiveRolesAsync(vahedCode, _currentUser.UserId, cancellationToken);

        if (!activeRoles.Contains(TreasuryRole.Treasurer))
        {
            throw new PaymentRequestTreasurerRoleRequiredException(paymentRequestId);
        }
    }
}
