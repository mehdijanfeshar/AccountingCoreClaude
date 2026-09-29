namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a voucher line that بخش ۴-ب needs cannot be built because the unit's
/// <c>TB_TR_SETTING</c> has no value for the relevant account column
/// (<c>PAYABLES_ACCOUNT_ID</c>/<c>VAT_CREDIT_ACCOUNT_ID</c>/<c>INSURANCE_PAYABLE_ACCOUNT_ID</c>) —
/// خزانه‌داری، بخش ۴-ب (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). <b>409</b> — the request is
/// well-formed, it conflicts with the unit's missing configuration, same shape as
/// <c>PaymentRequestSettingsMissingException</c>.
/// </summary>
public sealed class PaymentRequestTreasurySettingAccountMissingException : Exception
{
    public PaymentRequestTreasurySettingAccountMissingException(string accountLabel)
        : base($"TB_TR_SETTING has no value configured for {accountLabel}.")
    {
        AccountLabel = accountLabel;
    }

    public string AccountLabel { get; }

    public string PublicDetail => $"{AccountLabel} در تنظیمات خزانه تعریف نشده است.";
}
