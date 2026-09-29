namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a خزانه‌داری بخش ۴-ج operation (دریافت وجه <c>register</c>, انتقال وجه
/// <c>approve</c>) needs a <c>TB_TR_SETTING</c> value the unit's مدیر مالی has not configured yet
/// — <c>RECEIVABLES_ACCOUNT_ID</c>, <c>CUSTOMER_TAFSIL_GROUP_ID</c> or
/// <c>DAILY_TRANSFER_LIMIT</c>. Same 409 shape as
/// <c>PaymentRequestTreasurySettingAccountMissingException</c> (بخش ۴-ب) but deliberately its own
/// type rather than reused — that exception's wording/XML doc is scoped to درخواست پرداخت's own
/// three GL accounts.
/// </summary>
public sealed class TreasurySettingValueMissingException : Exception
{
    public TreasurySettingValueMissingException(string settingLabel)
        : base($"Treasury setting '{settingLabel}' is not configured for this unit.")
    {
        SettingLabel = settingLabel;
    }

    public string SettingLabel { get; }

    public string PublicDetail => $"«{SettingLabel}» هنوز برای این واحد تعریف نشده است.";
}
