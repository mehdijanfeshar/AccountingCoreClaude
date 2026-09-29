using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="IPaymentRequestTafsiliValidator"/>.</summary>
public sealed class PaymentRequestTafsiliValidator : IPaymentRequestTafsiliValidator
{
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly ITafsiliReadRepository _tafsiliReadRepository;
    private readonly ITreasurySettingReadRepository _treasurySettingReadRepository;
    private readonly ITreasuryBeneficiaryTafsiliReadRepository _beneficiaryTafsiliReadRepository;

    public PaymentRequestTafsiliValidator(
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        ITafsiliReadRepository tafsiliReadRepository,
        ITreasurySettingReadRepository treasurySettingReadRepository,
        ITreasuryBeneficiaryTafsiliReadRepository beneficiaryTafsiliReadRepository)
    {
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _tafsiliReadRepository = tafsiliReadRepository;
        _treasurySettingReadRepository = treasurySettingReadRepository;
        _beneficiaryTafsiliReadRepository = beneficiaryTafsiliReadRepository;
    }

    public async Task EnsureCostCenterTafsilisValidAsync(
        Guid expenseAccountId,
        IReadOnlyList<PaymentRequestTafsiliLinkInput> costCenterTafsilis,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        foreach (var link in costCenterTafsilis)
        {
            _ = await _tafsiliReadRepository.GetByIdAsync(link.TafsiliId, vahedCode, cancellationToken)
                ?? throw new NotFoundException("Tafsili", link.TafsiliId);
        }

        var guardInputs = costCenterTafsilis
            .Select(link => new VoucherDetailTafsiliLinkInput(link.TafsiliId, link.LevelId))
            .ToList();

        await _tafsiliLevelGuard.EnsureSatisfiedAsync(expenseAccountId, guardInputs, cancellationToken);
    }

    public async Task EnsureBeneficiaryTafsiliValidAsync(
        Guid? beneficiaryTafsiliId,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        if (beneficiaryTafsiliId is not { } tafsiliId)
        {
            return;
        }

        var setting = await _treasurySettingReadRepository.GetByVahedAsync(vahedCode, cancellationToken);

        if (setting?.BeneficiaryTafsilGroupId is not { } tafsilGroupId)
        {
            throw new PaymentRequestBeneficiaryGroupNotConfiguredException(vahedCode);
        }

        var isMember = await _beneficiaryTafsiliReadRepository.IsMemberOfGroupAsync(
            tafsiliId, tafsilGroupId, cancellationToken);

        if (!isMember)
        {
            throw new PaymentRequestBeneficiaryTafsiliNotInGroupException(tafsiliId, tafsilGroupId);
        }
    }
}
