using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.ApprovePettyCashExpenseDoc;

public sealed class ApprovePettyCashExpenseDocCommandHandler : IRequestHandler<ApprovePettyCashExpenseDocCommand>
{
    private readonly IPettyCashReviewTransitionService _transitionService;
    private readonly IUnitOfWork _unitOfWork;

    public ApprovePettyCashExpenseDocCommandHandler(
        IPettyCashReviewTransitionService transitionService,
        IUnitOfWork unitOfWork)
    {
        _transitionService = transitionService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ApprovePettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        await _transitionService.TransitionAsync(
            request.Id,
            request.VahedCode,
            PettyCashDocState.PendingReview,
            PettyCashDocState.Approved,
            PettyCashDocAction.Approve,
            request.Note,
            returnReasonsCsv: null,
            returnDeadline: null,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
