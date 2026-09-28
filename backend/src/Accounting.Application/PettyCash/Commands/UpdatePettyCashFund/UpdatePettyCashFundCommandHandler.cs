using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpdatePettyCashFund;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_PC_FUND"/> row via
/// <see cref="IPettyCashFundRepository.GetForUpdateAsync"/> (change-tracked), re-checks the
/// duplicate-code and معین-existence rules (excluding the row's own id from the duplicate check),
/// overwrites every writable field, stamps audit columns, and owns the transaction boundary with a
/// single <see cref="IUnitOfWork.SaveChangesAsync"/> call.
/// </summary>
public sealed class UpdatePettyCashFundCommandHandler : IRequestHandler<UpdatePettyCashFundCommand>
{
    private readonly IPettyCashFundRepository _pettyCashFundRepository;
    private readonly IAccountCodeReadRepository _accountCodeReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdatePettyCashFundCommandHandler(
        IPettyCashFundRepository pettyCashFundRepository,
        IAccountCodeReadRepository accountCodeReadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _pettyCashFundRepository = pettyCashFundRepository;
        _accountCodeReadRepository = accountCodeReadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdatePettyCashFundCommand request, CancellationToken cancellationToken)
    {
        var entity = await _pettyCashFundRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("PettyCashFund", request.Id);
        }

        var duplicate = await _pettyCashFundRepository.ExistsByCodeAsync(
            request.Code, request.VahedCode, excludeId: request.Id, cancellationToken);

        if (duplicate)
        {
            throw new PettyCashFundCodeDuplicateException(request.Code);
        }

        if (request.AccountCodeId is { } accountCodeId)
        {
            var accountCode = await _accountCodeReadRepository.GetByIdAsync(accountCodeId, cancellationToken);

            if (accountCode is null || accountCode.IsDeleted == true)
            {
                throw new NotFoundException("AccountCode", accountCodeId);
            }
        }

        entity.CODE = request.Code;
        entity.NAME = request.Name;
        entity.CUSTODIAN_USERID = request.CustodianUserId;
        entity.CUSTODIAN_NAME = request.CustodianName;
        entity.CEILING = request.Ceiling;
        entity.PER_DOC_LIMIT = request.PerDocLimit;
        entity.ALERT_THRESHOLD_PERCENT = request.AlertThresholdPercent;
        entity.ACCOUNTCODE_ID = request.AccountCodeId;
        entity.SETTLEMENT_PERIOD = request.SettlementPeriod;
        entity.IS_ACTIVE = request.IsActive;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
