using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.ApprovePettyCashReplenishment;

public sealed class ApprovePettyCashReplenishmentCommandHandler : IRequestHandler<ApprovePettyCashReplenishmentCommand>
{
    private readonly IPettyCashReplenishmentRepository _replenishmentRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashReplenishmentAuthorizer _authorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ApprovePettyCashReplenishmentCommandHandler(
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

    public async Task Handle(ApprovePettyCashReplenishmentCommand request, CancellationToken cancellationToken)
    {
        var replenishment = await _replenishmentRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (replenishment is null || replenishment.ISDELETED)
        {
            throw new NotFoundException("PettyCashReplenishment", request.Id);
        }

        if (replenishment.STATE != PettyCashReplenishmentState.PendingFinanceManager)
        {
            throw new PettyCashReplenishmentStateConflictException(
                replenishment.ID, replenishment.STATE, PettyCashReplenishmentState.PendingFinanceManager);
        }

        await _authorizer.EnsureHasRoleAsync(replenishment.FUND_ID, new[] { PettyCashRole.FinanceManager }, cancellationToken);

        var userId = _currentUser.UserId;

        if (string.Equals(userId, replenishment.ADDUSERID, StringComparison.Ordinal))
        {
            throw new PettyCashReplenishmentApproverConflictException(replenishment.ID);
        }

        var now = DateTime.UtcNow;

        replenishment.STATE = PettyCashReplenishmentState.PendingTreasurer;
        replenishment.APPROVED_BY_USERID = userId;
        replenishment.CHANGEUSERID = userId;
        replenishment.UPDATEDDATE = now;

        var head = await _chargeAndCostRepository.GetHeadForUpdateAsync(replenishment.CHARGEANDCOSTHEAD_ID, request.VahedCode, cancellationToken);

        if (head is not null)
        {
            head.STATUS = PettyCashStatusMap.ToLegacyStatus(PettyCashReplenishmentState.PendingTreasurer);
            head.CHANGEUSERID = userId;
            head.UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
