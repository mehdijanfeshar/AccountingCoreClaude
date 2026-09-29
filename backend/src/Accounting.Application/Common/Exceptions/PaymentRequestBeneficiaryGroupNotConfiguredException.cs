namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a caller sends a non-null <c>BeneficiaryTafsiliId</c> but the unit's
/// <c>TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID</c> is not configured — اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹،
/// صاحب پروژه؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰). <b>409</b>: the request itself is
/// well-formed, it conflicts with the unit's missing configuration — same shape as
/// <see cref="PaymentRequestSettingsMissingException"/>.
/// </summary>
public sealed class PaymentRequestBeneficiaryGroupNotConfiguredException : Exception
{
    public PaymentRequestBeneficiaryGroupNotConfiguredException(string vahedCode)
        : base($"Unit {vahedCode} has no TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID configured.")
    {
        VahedCode = vahedCode;
    }

    public string VahedCode { get; }

    public string PublicDetail => "گروه تفصیلی ذی‌نفع برای این واحد توسط مدیر مالی تعریف نشده است.";
}
