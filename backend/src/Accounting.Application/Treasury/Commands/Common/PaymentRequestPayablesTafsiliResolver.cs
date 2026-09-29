using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;

namespace Accounting.Application.Treasury.Commands.Common;

public sealed class PaymentRequestPayablesTafsiliResolver : IPaymentRequestPayablesTafsiliResolver
{
    private readonly ITafsiliLookupReadRepository _tafsiliLookupReadRepository;

    public PaymentRequestPayablesTafsiliResolver(ITafsiliLookupReadRepository tafsiliLookupReadRepository)
    {
        _tafsiliLookupReadRepository = tafsiliLookupReadRepository;
    }

    public async Task<IReadOnlyList<VoucherDetailTafsiliLinkInput>> ResolvePayablesLinksAsync(
        Guid payablesAccountId,
        Guid paymentRequestId,
        Guid? beneficiaryTafsiliId,
        CancellationToken cancellationToken = default)
    {
        var levels = await _tafsiliLookupReadRepository.GetActiveLevelsAsync(payablesAccountId, cancellationToken);

        if (levels.Count == 0)
        {
            return Array.Empty<VoucherDetailTafsiliLinkInput>();
        }

        if (levels.Count > 1)
        {
            throw new PaymentRequestVoucherAccountConfigException(
                "حساب بستانکاران", payablesAccountId, "بیش از یک سطح تفصیلی الزامی است.");
        }

        if (beneficiaryTafsiliId is not { } tafsiliId)
        {
            throw new PaymentRequestPayablesBeneficiaryRequiredException(paymentRequestId);
        }

        return new[] { new VoucherDetailTafsiliLinkInput(tafsiliId, levels[0].LevelId) };
    }

    public async Task EnsureNoTafsiliRequiredAsync(
        Guid accountCodeId, string accountLabel, CancellationToken cancellationToken = default)
    {
        var levels = await _tafsiliLookupReadRepository.GetActiveLevelsAsync(accountCodeId, cancellationToken);

        if (levels.Count > 0)
        {
            throw new PaymentRequestVoucherAccountConfigException(
                accountLabel, accountCodeId, "این حساب نباید سطح تفصیلی الزامی داشته باشد.");
        }
    }
}
