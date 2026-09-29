namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a caller-supplied تفصیلی id is not a member of the unit's configured تفصیلی گروه —
/// خزانه‌داری، بخش ۴-ج، used for <c>TB_TR_RECEIPT.PAYER_TAFSILI_ID</c> against
/// <c>TB_TR_SETTING.CUSTOMER_TAFSIL_GROUP_ID</c>. 400 (bad input value), not 409 (that is
/// <see cref="TreasurySettingValueMissingException"/>'s job, for "group not configured at all") —
/// same split as بخش ۴-الف's <c>PaymentRequestBeneficiaryTafsiliNotInGroupException</c> /
/// <c>PaymentRequestBeneficiaryGroupNotConfiguredException</c> pair.
/// </summary>
public sealed class TreasuryTafsiliNotInGroupException : Exception
{
    public TreasuryTafsiliNotInGroupException(Guid tafsiliId, Guid tafsilGroupId)
        : base($"Tafsili {tafsiliId} is not a member of تفصیلی گروه {tafsilGroupId}.")
    {
        TafsiliId = tafsiliId;
        TafsilGroupId = tafsilGroupId;
    }

    public Guid TafsiliId { get; }

    public Guid TafsilGroupId { get; }

    public string PublicDetail => "تفصیلی انتخاب‌شده عضو گروه تفصیلی مشتریان تعریف‌شدهٔ واحد نیست.";
}
