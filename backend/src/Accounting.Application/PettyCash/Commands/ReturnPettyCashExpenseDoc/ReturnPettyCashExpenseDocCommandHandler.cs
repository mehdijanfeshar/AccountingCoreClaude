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

    private static readonly PettyCashRole[] AllowedRoles =
    {
        PettyCashRole.Inspector,
        PettyCashRole.FinanceManager,
        PettyCashRole.ChiefExecutive,
    };

    public async Task Handle(ReturnPettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        var reasonCodesCsv = string.Join(',', request.ReasonCodes);

        var doc = await _transitionService.TransitionAsync(
            request.Id,
            request.VahedCode,
            PettyCashDocState.PendingReview,
            PettyCashDocState.Returned,
            PettyCashDocAction.Return,
            AllowedRoles,
            request.Note,
            reasonCodesCsv,
            request.Deadline,
            cancellationToken);

        // تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸): a returned document must be verified again from scratch.
        doc.VERIFIED_BY_USERID = null;
        doc.VERIFIED_DATE = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
