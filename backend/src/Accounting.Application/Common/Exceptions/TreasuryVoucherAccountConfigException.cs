namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a خزانه‌داری بخش ۴-ج automatic voucher's حساب معین is configured with more تفصیلی
/// levels than the (receipt/transfer) tafsili-resolution rule can express (more than one required
/// level) — same shape/reasoning as <c>PaymentRequestVoucherAccountConfigException</c> (بخش ۴-ب),
/// deliberately its own type so this module never needs to touch بخش ۴-ب's exception.
/// </summary>
public sealed class TreasuryVoucherAccountConfigException : Exception
{
    public TreasuryVoucherAccountConfigException(string accountLabel, Guid accountCodeId, string reason)
        : base($"Account '{accountLabel}' ({accountCodeId}) has an unsupported تفصیلی configuration: {reason}")
    {
        AccountLabel = accountLabel;
        AccountCodeId = accountCodeId;
        Reason = reason;
    }

    public string AccountLabel { get; }

    public Guid AccountCodeId { get; }

    public string Reason { get; }

    public string PublicDetail => $"پیکربندی تفصیلی حساب «{AccountLabel}» پشتیبانی نمی‌شود: {Reason}";
}
