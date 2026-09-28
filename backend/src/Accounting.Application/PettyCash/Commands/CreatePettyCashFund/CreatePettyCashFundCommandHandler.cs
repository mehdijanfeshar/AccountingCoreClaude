using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashFund;

/// <summary>
/// Verifies the (unit-scoped) code is not already taken, verifies the optional معین link actually
/// exists, constructs the <see cref="TB_PC_FUND"/> Domain entity, stages it, and owns the
/// transaction boundary by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once.
/// <c>ADDUSERID</c> is sourced from <see cref="ICurrentUser"/> — never from the request.
/// </summary>
public sealed class CreatePettyCashFundCommandHandler : IRequestHandler<CreatePettyCashFundCommand, Guid>
{
    private readonly IPettyCashFundRepository _pettyCashFundRepository;
    private readonly IAccountCodeReadRepository _accountCodeReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreatePettyCashFundCommandHandler(
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

    public async Task<Guid> Handle(CreatePettyCashFundCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _pettyCashFundRepository.ExistsByCodeAsync(
            request.Code, request.VahedCode, excludeId: null, cancellationToken);

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

        var entity = new TB_PC_FUND
        {
            ID = Guid.NewGuid(),
            CODE = request.Code,
            NAME = request.Name,
            CUSTODIAN_USERID = request.CustodianUserId,
            CUSTODIAN_NAME = request.CustodianName,
            CEILING = request.Ceiling,
            PER_DOC_LIMIT = request.PerDocLimit,
            FINANCE_MANAGER_APPROVAL_LIMIT = request.FinanceManagerApprovalLimit,
            ALERT_THRESHOLD_PERCENT = request.AlertThresholdPercent,
            ACCOUNTCODE_ID = request.AccountCodeId,
            SETTLEMENT_PERIOD = request.SettlementPeriod,
            IS_ACTIVE = request.IsActive,
            VAHEDCODE = request.VahedCode,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _pettyCashFundRepository.AddAsync(entity, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
