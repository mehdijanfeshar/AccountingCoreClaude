using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.RejectPettyCashExpenseDoc;

public sealed class RejectPettyCashExpenseDocCommandHandler : IRequestHandler<RejectPettyCashExpenseDocCommand>
{
    private readonly IPettyCashReviewTransitionService _transitionService;
    private readonly IUnitOfWork _unitOfWork;

    public RejectPettyCashExpenseDocCommandHandler(
        IPettyCashReviewTransitionService transitionService,
        IUnitOfWork unitOfWork)
    {
        _transitionService = transitionService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RejectPettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        await _transitionService.TransitionAsync(
            request.Id,
            request.VahedCode,
            PettyCashDocState.PendingReview,
            PettyCashDocState.Rejected,
            PettyCashDocAction.Reject,
            request.Note,
            returnReasonsCsv: null,
            returnDeadline: null,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
