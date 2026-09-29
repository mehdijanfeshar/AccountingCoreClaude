using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Application.Treasury.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpsertTreasurySetting;

public sealed class UpsertTreasurySettingCommandHandler : IRequestHandler<UpsertTreasurySettingCommand, TreasurySettingDto>
{
    private readonly ITreasurySettingRepository _settingRepository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly ITafsilGroupReadRepository _tafsilGroupReadRepository;
    private readonly IAccountCodeReadRepository _accountCodeReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpsertTreasurySettingCommandHandler(
        ITreasurySettingRepository settingRepository,
        ITreasuryRoleAuthorizer roleAuthorizer,
        ITafsilGroupReadRepository tafsilGroupReadRepository,
        IAccountCodeReadRepository accountCodeReadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _settingRepository = settingRepository;
        _roleAuthorizer = roleAuthorizer;
        _tafsilGroupReadRepository = tafsilGroupReadRepository;
        _accountCodeReadRepository = accountCodeReadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<TreasurySettingDto> Handle(UpsertTreasurySettingCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, new[] { TreasuryRole.FinanceManager }, cancellationToken);

        string? groupCode = null;
        string? groupName = null;

        if (request.BeneficiaryTafsilGroupId is { } groupId)
        {
            var group = await _tafsilGroupReadRepository.GetByIdAsync(groupId, cancellationToken);

            if (group is null || group.IsDeleted)
            {
                throw new NotFoundException("TafsilGroup", groupId);
            }

            groupCode = group.TafsilGroupCode;
            groupName = group.TafsilGroupName;
        }

        var (payablesCode, payablesName) = await EnsureAccountCodeExistsAsync(
            "PayablesAccount", request.PayablesAccountId, cancellationToken);
        var (vatCode, vatName) = await EnsureAccountCodeExistsAsync(
            "VatCreditAccount", request.VatCreditAccountId, cancellationToken);
        var (insuranceCode, insuranceName) = await EnsureAccountCodeExistsAsync(
            "InsurancePayableAccount", request.InsurancePayableAccountId, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var setting = await _settingRepository.GetForUpdateAsync(request.VahedCode, cancellationToken);

        if (setting is null)
        {
            setting = new TB_TR_SETTING
            {
                ID = Guid.NewGuid(),
                VAHEDCODE = request.VahedCode,
                CEO_APPROVAL_THRESHOLD = request.CeoApprovalThreshold,
                BULK_APPROVE_LIMIT = request.BulkApproveLimit,
                BENEFICIARY_TAFSIL_GROUP_ID = request.BeneficiaryTafsilGroupId,
                PAYABLES_ACCOUNT_ID = request.PayablesAccountId,
                VAT_CREDIT_ACCOUNT_ID = request.VatCreditAccountId,
                INSURANCE_PAYABLE_ACCOUNT_ID = request.InsurancePayableAccountId,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            await _settingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.CEO_APPROVAL_THRESHOLD = request.CeoApprovalThreshold;
            setting.BULK_APPROVE_LIMIT = request.BulkApproveLimit;
            setting.BENEFICIARY_TAFSIL_GROUP_ID = request.BeneficiaryTafsilGroupId;
            setting.PAYABLES_ACCOUNT_ID = request.PayablesAccountId;
            setting.VAT_CREDIT_ACCOUNT_ID = request.VatCreditAccountId;
            setting.INSURANCE_PAYABLE_ACCOUNT_ID = request.InsurancePayableAccountId;
            setting.CHANGEUSERID = userId;
            setting.UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TreasurySettingDto(
            setting.ID,
            setting.CEO_APPROVAL_THRESHOLD,
            setting.BULK_APPROVE_LIMIT,
            setting.BENEFICIARY_TAFSIL_GROUP_ID,
            groupCode,
            groupName,
            setting.PAYABLES_ACCOUNT_ID,
            payablesCode,
            payablesName,
            setting.VAT_CREDIT_ACCOUNT_ID,
            vatCode,
            vatName,
            setting.INSURANCE_PAYABLE_ACCOUNT_ID,
            insuranceCode,
            insuranceName);
    }

    private async Task<(string? Code, string? Name)> EnsureAccountCodeExistsAsync(
        string entityLabel, Guid? accountCodeId, CancellationToken cancellationToken)
    {
        if (accountCodeId is not { } id)
        {
            return (null, null);
        }

        var account = await _accountCodeReadRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(entityLabel, id);

        return (account.AccCode, account.AccCodeName);
    }
}
