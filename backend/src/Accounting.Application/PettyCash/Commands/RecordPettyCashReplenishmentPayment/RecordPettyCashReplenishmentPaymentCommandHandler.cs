using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.RecordPettyCashReplenishmentPayment;

public sealed class RecordPettyCashReplenishmentPaymentCommandHandler : IRequestHandler<RecordPettyCashReplenishmentPaymentCommand>
{
    private readonly IPettyCashReplenishmentRepository _replenishmentRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashReplenishmentAuthorizer _authorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RecordPettyCashReplenishmentPaymentCommandHandler(
        IPettyCashReplenishmentRepository replenishmentRepository,
        IChargeAndCostRepository chargeAndCostRepository,
        IPettyCashReplenishmentAuthorizer authorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _replenishmentRepository = replenishmentRepository;
        _chargeAndCostRepository = chargeAndCostRepository;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(RecordPettyCashReplenishmentPaymentCommand request, CancellationToken cancellationToken)
    {
        var replenishment = await _replenishmentRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (replenishment is null || replenishment.ISDELETED)
        {
            throw new NotFoundException("PettyCashReplenishment", request.Id);
        }

        if (replenishment.STATE != PettyCashReplenishmentState.PendingTreasurer)
        {
            throw new PettyCashReplenishmentStateConflictException(
                replenishment.ID, replenishment.STATE, PettyCashReplenishmentState.PendingTreasurer);
        }

        await _authorizer.EnsureHasRoleAsync(replenishment.FUND_ID, new[] { PettyCashRole.Treasurer }, cancellationToken);

        var userId = _currentUser.UserId;

        if (string.Equals(userId, replenishment.APPROVED_BY_USERID, StringComparison.Ordinal))
        {
            throw new PettyCashReplenishmentPayerConflictException(replenishment.ID);
        }

        var now = DateTime.UtcNow;

        replenishment.STATE = PettyCashReplenishmentState.Paid;
        replenishment.PAID_DATE = request.PaidDate ?? now;
        replenishment.PAID_BY_USERID = userId;
        replenishment.CHANGEUSERID = userId;
        replenishment.UPDATEDDATE = now;

        var head = await _chargeAndCostRepository.GetHeadForUpdateAsync(replenishment.CHARGEANDCOSTHEAD_ID, request.VahedCode, cancellationToken);

        if (head is not null)
        {
            head.STATUS = PettyCashStatusMap.ToLegacyStatus(PettyCashReplenishmentState.Paid);
            head.CHANGEUSERID = userId;
            head.UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
