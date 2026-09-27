using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpsertPettyCashFundSetting;

/// <summary>
/// Verifies <see cref="UpsertPettyCashFundSettingCommand.FundId"/> belongs to the caller's unit
/// (via <see cref="IRevolvingFundRepository.GetForUpdateAsync"/>, throwing
/// <see cref="NotFoundException"/> — 404 — when it does not exist or belongs to another unit,
/// which surfaces as <c>UnitAccessDeniedException</c> — 403 — from that call instead), then either
/// creates a new <see cref="TB_PC_FUND_SETTING"/> row or mutates the existing tracked one in
/// place, and owns the transaction boundary with a single <see cref="IUnitOfWork.SaveChangesAsync"/>
/// call.
/// </summary>
public sealed class UpsertPettyCashFundSettingCommandHandler : IRequestHandler<UpsertPettyCashFundSettingCommand>
{
    private readonly IRevolvingFundRepository _revolvingFundRepository;
    private readonly IPettyCashFundSettingRepository _fundSettingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpsertPettyCashFundSettingCommandHandler(
        IRevolvingFundRepository revolvingFundRepository,
        IPettyCashFundSettingRepository fundSettingRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _revolvingFundRepository = revolvingFundRepository;
        _fundSettingRepository = fundSettingRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpsertPettyCashFundSettingCommand request, CancellationToken cancellationToken)
    {
        var fund = await _revolvingFundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken);

        if (fund is null)
        {
            throw new NotFoundException("RevolvingFund", request.FundId);
        }

        var now = DateTime.UtcNow;
        var existing = await _fundSettingRepository.GetByFundIdAsync(request.FundId, cancellationToken);

        if (existing is null)
        {
            await _fundSettingRepository.AddAsync(
                new TB_PC_FUND_SETTING
                {
                    ID = Guid.NewGuid(),
                    REVOLVINGFUND_ID = request.FundId,
                    CUSTODIAN_USERID = request.CustodianUserId,
                    CUSTODIAN_NAME = request.CustodianName,
                    PER_DOC_LIMIT = request.PerDocLimit,
                    ALERT_THRESHOLD_PERCENT = request.AlertThresholdPercent,
                    SETTLEMENT_PERIOD = request.SettlementPeriod,
                    VAHEDCODE = request.VahedCode,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                },
                cancellationToken);
        }
        else
        {
            existing.CUSTODIAN_USERID = request.CustodianUserId;
            existing.CUSTODIAN_NAME = request.CustodianName;
            existing.PER_DOC_LIMIT = request.PerDocLimit;
            existing.ALERT_THRESHOLD_PERCENT = request.AlertThresholdPercent;
            existing.SETTLEMENT_PERIOD = request.SettlementPeriod;
            existing.ISDELETED = false;
            existing.CHANGEUSERID = _currentUser.UserId;
            existing.UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
