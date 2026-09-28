using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashReplenishmentAuthorizer : IPettyCashReplenishmentAuthorizer
{
    private readonly IPettyCashFundReviewerRepository _reviewerRepository;
    private readonly ICurrentUser _currentUser;

    public PettyCashReplenishmentAuthorizer(
        IPettyCashFundReviewerRepository reviewerRepository,
        ICurrentUser currentUser)
    {
        _reviewerRepository = reviewerRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<PettyCashRole>> EnsureHasRoleAsync(
        Guid fundId,
        IReadOnlyCollection<PettyCashRole> allowedRoles,
        CancellationToken cancellationToken = default)
    {
        var activeRoles = await _reviewerRepository.GetActiveRolesAsync(fundId, _currentUser.UserId, cancellationToken);

        var matchingRoles = activeRoles.Where(allowedRoles.Contains).ToList();

        if (matchingRoles.Count == 0)
        {
            throw new PettyCashReplenishmentRoleRequiredException(fundId, allowedRoles);
        }

        return matchingRoles;
    }
}
