using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashRefund;

public sealed class DeletePettyCashRefundCommandHandler : IRequestHandler<DeletePettyCashRefundCommand>
{
    private readonly IPettyCashRefundRepository _refundRepository;
    private readonly IPettyCashFundRepository _fundRepository;
    private readonly IPettyCashRefundRecorderAuthorizer _authorizer;
    private readonly IPettyCashSettlementPeriodRepository _settlementPeriodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeletePettyCashRefundCommandHandler(
        IPettyCashRefundRepository refundRepository,
        IPettyCashFundRepository fundRepository,
        IPettyCashRefundRecorderAuthorizer authorizer,
        IPettyCashSettlementPeriodRepository settlementPeriodRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _refundRepository = refundRepository;
        _fundRepository = fundRepository;
        _authorizer = authorizer;
        _settlementPeriodRepository = settlementPeriodRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePettyCashRefundCommand request, CancellationToken cancellationToken)
    {
        var refund = await _refundRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (refund is null)
        {
            throw new NotFoundException("PettyCashRefund", request.Id);
        }

        var fund = await _fundRepository.GetForUpdateAsync(refund.FUND_ID, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("PettyCashFund", refund.FUND_ID);

        await _authorizer.EnsureCanRecordAsync(fund, cancellationToken);

        if (refund.ISDELETED)
        {
            return;
        }

        // Closes the بخش ۳-ب TODO flagged on DeletePettyCashRefundCommand: a refund whose own
        // date falls inside an already-finalized settlement period may not be deleted.
        var lockedBySettledPeriod = await _settlementPeriodRepository.ExistsFinalCoveringDateAsync(
            refund.FUND_ID, refund.REFUND_DATE, request.VahedCode, cancellationToken);

        if (lockedBySettledPeriod)
        {
            throw new PettyCashRefundLockedBySettledPeriodException(refund.ID);
        }

        refund.ISDELETED = true;
        refund.CHANGEUSERID = _currentUser.UserId;
        refund.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
