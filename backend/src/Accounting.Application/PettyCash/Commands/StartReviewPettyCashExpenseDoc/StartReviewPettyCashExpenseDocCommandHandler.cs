using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.StartReviewPettyCashExpenseDoc;

public sealed class StartReviewPettyCashExpenseDocCommandHandler : IRequestHandler<StartReviewPettyCashExpenseDocCommand>
{
    private readonly IPettyCashReviewTransitionService _transitionService;
    private readonly IUnitOfWork _unitOfWork;

    public StartReviewPettyCashExpenseDocCommandHandler(
        IPettyCashReviewTransitionService transitionService,
        IUnitOfWork unitOfWork)
    {
        _transitionService = transitionService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(StartReviewPettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        await _transitionService.TransitionAsync(
            request.Id,
            request.VahedCode,
            PettyCashDocState.New,
            PettyCashDocState.PendingReview,
            PettyCashDocAction.StartReview,
            new[] { PettyCashRole.Inspector },
            note: null,
            returnReasonsCsv: null,
            returnDeadline: null,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
