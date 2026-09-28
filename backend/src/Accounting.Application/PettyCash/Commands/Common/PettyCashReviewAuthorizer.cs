using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashReviewAuthorizer : IPettyCashReviewAuthorizer
{
    private readonly IPettyCashFundReviewerRepository _reviewerRepository;
    private readonly ICurrentUser _currentUser;

    public PettyCashReviewAuthorizer(
        IPettyCashFundReviewerRepository reviewerRepository,
        ICurrentUser currentUser)
    {
        _reviewerRepository = reviewerRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<PettyCashRole>> EnsureCanReviewAsync(
        Guid expenseDocId,
        Guid fundId,
        string documentCreatorUserId,
        IReadOnlyCollection<PettyCashRole> allowedRoles,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;

        var activeRoles = await _reviewerRepository.GetActiveRolesAsync(fundId, userId, cancellationToken);

        if (activeRoles.Count == 0)
        {
            throw new PettyCashReviewerAccessDeniedException(expenseDocId, fundId);
        }

        var matchingRoles = activeRoles.Where(allowedRoles.Contains).ToList();

        if (matchingRoles.Count == 0)
        {
            // The caller IS an active reviewer for this fund, just not in a role this specific
            // action allows — reported the same as "not a reviewer at all" rather than leaking
            // which roles exist, same shape as every other 403 in this module.
            throw new PettyCashReviewerAccessDeniedException(expenseDocId, fundId);
        }

        if (string.Equals(userId, documentCreatorUserId, StringComparison.Ordinal))
        {
            throw new PettyCashSelfReviewConflictException(expenseDocId);
        }

        return matchingRoles;
    }
}
