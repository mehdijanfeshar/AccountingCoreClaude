namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a تنظیمات-خزانه account configured for بخش ۴-ب's automatic vouchers
/// (<c>PAYABLES_ACCOUNT_ID</c>/<c>VAT_CREDIT_ACCOUNT_ID</c>/<c>INSURANCE_PAYABLE_ACCOUNT_ID</c>)
/// has a تفصیلی-level shape this write path cannot satisfy — خزانه‌داری، بخش ۴-ب (owner decision
/// ۲۰۲۶-۰۹-۲۹ #۴): the «حساب بستانکاران» line may require at most one تفصیلی level (filled from
/// the request's ذی‌نفع), and the VAT-credit/insurance-payable lines must require none at all.
/// <b>409</b> — a configuration conflict in existing data, not something the caller's request body
/// could have fixed (same reasoning as <c>PettyCashSettlementTafsiliMissingException</c>).
/// </summary>
public sealed class PaymentRequestVoucherAccountConfigException : Exception
{
    public PaymentRequestVoucherAccountConfigException(string accountLabel, Guid accountCodeId, string detail)
        : base($"Account {accountCodeId} ({accountLabel}) has an unsupported تفصیلی level configuration for بخش ۴-ب: {detail}")
    {
        AccountLabel = accountLabel;
        AccountCodeId = accountCodeId;
        PublicDetail = $"پیکربندی سطوح تفصیلی {accountLabel} برای این عملیات مناسب نیست: {detail}";
    }

    public string AccountLabel { get; }

    public Guid AccountCodeId { get; }

    public string PublicDetail { get; }
}
