using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.RejectPettyCashReplenishment;

public sealed class RejectPettyCashReplenishmentCommandHandler : IRequestHandler<RejectPettyCashReplenishmentCommand>
{
    private readonly IPettyCashReplenishmentRepository _replenishmentRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashReplenishmentAuthorizer _authorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RejectPettyCashReplenishmentCommandHandler(
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

    public async Task Handle(RejectPettyCashReplenishmentCommand request, CancellationToken cancellationToken)
    {
        var replenishment = await _replenishmentRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (replenishment is null || replenishment.ISDELETED)
        {
            throw new NotFoundException("PettyCashReplenishment", request.Id);
        }

        if (replenishment.STATE is not (PettyCashReplenishmentState.PendingFinanceManager or PettyCashReplenishmentState.PendingTreasurer))
        {
            throw new PettyCashReplenishmentStateConflictException(
                replenishment.ID, replenishment.STATE, PettyCashReplenishmentState.PendingFinanceManager);
        }

        await _authorizer.EnsureHasRoleAsync(
            replenishment.FUND_ID, new[] { PettyCashRole.FinanceManager, PettyCashRole.Treasurer }, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        replenishment.STATE = PettyCashReplenishmentState.Rejected;

        // No dedicated "reject reason" column exists on TB_PC_REPLENISHMENT (design §۳-الف only
        // defines one free-text NOTE) — appended, never overwritten, so the creation note survives.
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            replenishment.NOTE = string.IsNullOrWhiteSpace(replenishment.NOTE)
                ? request.Note
                : $"{replenishment.NOTE} | رد: {request.Note}";
        }

        replenishment.CHANGEUSERID = userId;
        replenishment.UPDATEDDATE = now;

        // «لینک‌های اسناد این ترمیم نرم‌حذف شوند تا اسناد دوباره قابل ترمیم باشند».
        var links = await _chargeAndCostRepository.GetActiveLinksByChargeIdAsync(replenishment.CHARGEANDCOSTHEAD_ID, cancellationToken);

        foreach (var link in links)
        {
            link.ISDELETED = true;
            link.CHANGEUSERID = userId;
            link.UPDATEDDATE = now;
        }

        var head = await _chargeAndCostRepository.GetHeadForUpdateAsync(replenishment.CHARGEANDCOSTHEAD_ID, request.VahedCode, cancellationToken);

        if (head is not null)
        {
            head.STATUS = PettyCashStatusMap.ToLegacyStatus(PettyCashReplenishmentState.Rejected);
            head.CHANGEUSERID = userId;
            head.UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
