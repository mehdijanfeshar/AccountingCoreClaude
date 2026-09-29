namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller's <c>BeneficiaryTafsiliId</c> is not an active member (via
/// <c>TB_TAFSIL_LINK_TAFSILGROUP</c>) of the unit's configured
/// <c>TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID</c> — اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹، صاحب پروژه؛
/// <c>docs/tankhah-khazaneh-module.md</c> §۱۰). <b>400</b>: a value-level input error, not a
/// configuration conflict — the group IS configured, the caller's value just is not in it.
/// </summary>
public sealed class PaymentRequestBeneficiaryTafsiliNotInGroupException : Exception
{
    public PaymentRequestBeneficiaryTafsiliNotInGroupException(Guid tafsiliId, Guid tafsilGroupId)
        : base($"Tafsili {tafsiliId} is not a member of tafsil group {tafsilGroupId}.")
    {
        TafsiliId = tafsiliId;
        TafsilGroupId = tafsilGroupId;
    }

    public Guid TafsiliId { get; }

    public Guid TafsilGroupId { get; }

    public string PublicDetail => "تفصیلی انتخاب‌شده عضو گروه تفصیلی ذی‌نفعِ تعریف‌شده برای این واحد نیست.";
}
