using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;

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

    public async Task EnsureCanReviewAsync(
        Guid expenseDocId,
        Guid fundId,
        string documentCreatorUserId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;

        var reviewer = await _reviewerRepository.GetByFundAndUserIdAsync(fundId, userId, cancellationToken);

        if (reviewer is null || reviewer.ISDELETED)
        {
            throw new PettyCashReviewerAccessDeniedException(expenseDocId, fundId);
        }

        if (string.Equals(userId, documentCreatorUserId, StringComparison.Ordinal))
        {
            throw new PettyCashSelfReviewConflictException(expenseDocId);
        }
    }
}
