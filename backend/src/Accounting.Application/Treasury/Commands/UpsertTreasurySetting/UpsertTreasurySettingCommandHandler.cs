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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpsertTreasurySettingCommandHandler(
        ITreasurySettingRepository settingRepository,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _settingRepository = settingRepository;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<TreasurySettingDto> Handle(UpsertTreasurySettingCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, new[] { TreasuryRole.FinanceManager }, cancellationToken);

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
            setting.CHANGEUSERID = userId;
            setting.UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TreasurySettingDto(setting.ID, setting.CEO_APPROVAL_THRESHOLD, setting.BULK_APPROVE_LIMIT);
    }
}
