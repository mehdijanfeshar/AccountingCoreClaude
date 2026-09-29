namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Validates a دریافت وجه's <c>PAYER_TAFSILI_ID</c> against the unit's configured
/// <c>TB_TR_SETTING.CUSTOMER_TAFSIL_GROUP_ID</c> and returns the تفصیلی's current name, to be
/// snapshotted onto <c>TB_TR_RECEIPT.PAYER_NAME</c> — شخص‌سازی همان الگوی
/// <c>IPaymentRequestTafsiliValidator.EnsureBeneficiaryTafsiliValidAsync</c> (بخش ۴-الف) but for a
/// separate («مشتریان») تفصیلی گروه, mandatory here (not optional). Shared by
/// <c>CreateReceiptCommandHandler</c> and <c>UpdateReceiptCommandHandler</c>.
/// </summary>
public interface IReceiptPayerValidator
{
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasurySettingValueMissingException">
    /// The unit has not configured <c>CUSTOMER_TAFSIL_GROUP_ID</c> yet.
    /// </exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryTafsiliNotInGroupException">
    /// <paramref name="payerTafsiliId"/> is not a member of the configured group.
    /// </exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.NotFoundException">
    /// <paramref name="payerTafsiliId"/> does not exist.
    /// </exception>
    /// <returns>The تفصیلی's current <c>TAFSILI_NAME</c>, to snapshot onto <c>PAYER_NAME</c>.</returns>
    Task<string> EnsurePayerValidAsync(Guid payerTafsiliId, string vahedCode, CancellationToken cancellationToken = default);
}
