using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="IReceiptPayerValidator"/> XML doc.</summary>
public sealed class ReceiptPayerValidator : IReceiptPayerValidator
{
    private const string CustomerGroupLabel = "گروه تفصیلی مشتریان";

    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly ITreasuryBeneficiaryTafsiliReadRepository _tafsiliGroupReadRepository;
    private readonly ITafsiliReadRepository _tafsiliReadRepository;

    public ReceiptPayerValidator(
        ITreasurySettingReadRepository settingReadRepository,
        ITreasuryBeneficiaryTafsiliReadRepository tafsiliGroupReadRepository,
        ITafsiliReadRepository tafsiliReadRepository)
    {
        _settingReadRepository = settingReadRepository;
        _tafsiliGroupReadRepository = tafsiliGroupReadRepository;
        _tafsiliReadRepository = tafsiliReadRepository;
    }

    public async Task<string> EnsurePayerValidAsync(Guid payerTafsiliId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var setting = await _settingReadRepository.GetByVahedAsync(vahedCode, cancellationToken);

        var customerGroupId = setting?.CustomerTafsilGroupId
            ?? throw new TreasurySettingValueMissingException(CustomerGroupLabel);

        var isMember = await _tafsiliGroupReadRepository.IsMemberOfGroupAsync(payerTafsiliId, customerGroupId, cancellationToken);

        if (!isMember)
        {
            throw new TreasuryTafsiliNotInGroupException(payerTafsiliId, customerGroupId);
        }

        var tafsili = await _tafsiliReadRepository.GetByIdAsync(payerTafsiliId, vahedCode, cancellationToken)
            ?? throw new NotFoundException("Tafsili", payerTafsiliId);

        return tafsili.TafsiliName ?? string.Empty;
    }
}
