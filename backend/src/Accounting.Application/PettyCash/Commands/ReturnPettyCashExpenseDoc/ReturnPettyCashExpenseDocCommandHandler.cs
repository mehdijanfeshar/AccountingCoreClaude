using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.ReturnPettyCashExpenseDoc;

public sealed class ReturnPettyCashExpenseDocCommandHandler : IRequestHandler<ReturnPettyCashExpenseDocCommand>
{
    private readonly IPettyCashReviewTransitionService _transitionService;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnPettyCashExpenseDocCommandHandler(
        IPettyCashReviewTransitionService transitionService,
        IUnitOfWork unitOfWork)
    {
        _transitionService = transitionService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReturnPettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        var reasonCodesCsv = string.Join(',', request.ReasonCodes);

        await _transitionService.TransitionAsync(
            request.Id,
            request.VahedCode,
            PettyCashDocState.PendingReview,
            PettyCashDocState.Returned,
            PettyCashDocAction.Return,
            request.Note,
            reasonCodesCsv,
            request.Deadline,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
