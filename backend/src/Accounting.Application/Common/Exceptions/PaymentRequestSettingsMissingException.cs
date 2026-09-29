namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown on Submit when the unit has no <c>TB_TR_SETTING</c> row yet (no
/// <c>CEO_APPROVAL_THRESHOLD</c>/<c>BULK_APPROVE_LIMIT</c> defined) — خزانه‌داری، بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). <b>409</b>: the request itself is well-formed, it
/// conflicts with the unit's missing configuration.
/// </summary>
public sealed class PaymentRequestSettingsMissingException : Exception
{
    public PaymentRequestSettingsMissingException(string vahedCode)
        : base($"Unit {vahedCode} has no TB_TR_SETTING row.")
    {
        VahedCode = vahedCode;
    }

    public string VahedCode { get; }

    public string PublicDetail => "تنظیمات خزانه (آستانهٔ تأیید مدیرعامل) توسط مدیر مالی تعریف نشده است.";
}
