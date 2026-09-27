using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashFundReviewer;

public sealed class DeletePettyCashFundReviewerCommandHandler : IRequestHandler<DeletePettyCashFundReviewerCommand>
{
    private readonly IPettyCashFundReviewerRepository _reviewerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeletePettyCashFundReviewerCommandHandler(
        IPettyCashFundReviewerRepository reviewerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _reviewerRepository = reviewerRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePettyCashFundReviewerCommand request, CancellationToken cancellationToken)
    {
        var reviewer = await _reviewerRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        // A reviewer row that exists but belongs to a different fund than the route claims is
        // treated the same as "does not exist" — the caller has no legitimate path-based reason
        // to distinguish the two (same reasoning DeletePettyCashExpenseDoc et al. apply to a
        // mismatched/missing id).
        if (reviewer is null || reviewer.REVOLVINGFUND_ID != request.FundId)
        {
            throw new NotFoundException("PettyCashReviewer", request.Id);
        }

        if (reviewer.ISDELETED)
        {
            return;
        }

        reviewer.ISDELETED = true;
        reviewer.CHANGEUSERID = _currentUser.UserId;
        reviewer.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
