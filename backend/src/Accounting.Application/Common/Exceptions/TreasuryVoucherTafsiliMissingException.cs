namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Translated from <c>TafsiliLevelRuleException</c> when
/// <c>IVoucherTafsiliLevelGuard.EnsureSatisfiedAsync</c> rejects a بخش-۴-ج automatic voucher line's
/// resolved تفصیلی assignment — defense-in-depth check, same shape as
/// <c>PaymentRequestVoucherTafsiliMissingException</c> (بخش ۴-ب).
/// </summary>
public sealed class TreasuryVoucherTafsiliMissingException : Exception
{
    public TreasuryVoucherTafsiliMissingException(Guid accountCodeId, string publicDetail)
        : base($"Voucher line for account {accountCodeId} failed تفصیلی level validation: {publicDetail}")
    {
        AccountCodeId = accountCodeId;
        PublicDetail = publicDetail;
    }

    public Guid AccountCodeId { get; }

    public string PublicDetail { get; }
}
