using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashReplenishment;

public sealed class DeletePettyCashReplenishmentCommandHandler : IRequestHandler<DeletePettyCashReplenishmentCommand>
{
    private readonly IPettyCashReplenishmentRepository _replenishmentRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashReplenishmentAuthorizer _authorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeletePettyCashReplenishmentCommandHandler(
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

    public async Task Handle(DeletePettyCashReplenishmentCommand request, CancellationToken cancellationToken)
    {
        var replenishment = await _replenishmentRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (replenishment is null)
        {
            throw new NotFoundException("PettyCashReplenishment", request.Id);
        }

        if (replenishment.STATE != PettyCashReplenishmentState.Draft)
        {
            throw new PettyCashReplenishmentStateConflictException(
                replenishment.ID, replenishment.STATE, PettyCashReplenishmentState.Draft);
        }

        await _authorizer.EnsureHasRoleAsync(
            replenishment.FUND_ID, new[] { PettyCashRole.FinanceManager, PettyCashRole.Treasurer }, cancellationToken);

        if (replenishment.ISDELETED)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        replenishment.ISDELETED = true;
        replenishment.CHANGEUSERID = userId;
        replenishment.UPDATEDDATE = now;

        var links = await _chargeAndCostRepository.GetActiveLinksByChargeIdAsync(replenishment.CHARGEANDCOSTHEAD_ID, cancellationToken);

        foreach (var link in links)
        {
            link.ISDELETED = true;
            link.CHANGEUSERID = userId;
            link.UPDATEDDATE = now;
        }

        var head = await _chargeAndCostRepository.GetHeadForUpdateAsync(replenishment.CHARGEANDCOSTHEAD_ID, request.VahedCode, cancellationToken);

        if (head is not null && !head.ISDELETED)
        {
            head.ISDELETED = true;
            head.CHANGEUSERID = userId;
            head.UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
