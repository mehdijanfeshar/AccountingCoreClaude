using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpsertPettyCashFundReviewer;

public sealed class UpsertPettyCashFundReviewerCommandHandler : IRequestHandler<UpsertPettyCashFundReviewerCommand, Guid>
{
    private readonly IRevolvingFundRepository _revolvingFundRepository;
    private readonly IPettyCashFundReviewerRepository _reviewerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpsertPettyCashFundReviewerCommandHandler(
        IRevolvingFundRepository revolvingFundRepository,
        IPettyCashFundReviewerRepository reviewerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _revolvingFundRepository = revolvingFundRepository;
        _reviewerRepository = reviewerRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(UpsertPettyCashFundReviewerCommand request, CancellationToken cancellationToken)
    {
        var fund = await _revolvingFundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken);

        if (fund is null)
        {
            throw new NotFoundException("RevolvingFund", request.FundId);
        }

        var now = DateTime.UtcNow;
        var existing = await _reviewerRepository.GetByFundAndUserIdAsync(request.FundId, request.ReviewerUserId, cancellationToken);

        if (existing is null)
        {
            var reviewer = new TB_PC_REVIEWER
            {
                ID = Guid.NewGuid(),
                REVOLVINGFUND_ID = request.FundId,
                REVIEWER_USERID = request.ReviewerUserId,
                REVIEWER_NAME = request.ReviewerName,
                VAHEDCODE = request.VahedCode,
                ADDUSERID = _currentUser.UserId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            await _reviewerRepository.AddAsync(reviewer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return reviewer.ID;
        }

        existing.REVIEWER_NAME = request.ReviewerName;
        existing.ISDELETED = false;
        existing.CHANGEUSERID = _currentUser.UserId;
        existing.UPDATEDDATE = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return existing.ID;
    }
}
