namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Wraps <c>TafsiliLevelRuleException</c> into a بخش-۴-ب-specific <b>409</b> — same reasoning as
/// <c>PettyCashSettlementTafsiliMissingException</c>: every line of the «شناسایی بدهی»/«پرداخت»
/// vouchers is server-derived (cost-center links copied from the request, بستانکاران تفصیلی
/// resolved from configuration, بانک تفصیلی copied from <c>TB_ACCOUNT_LINK_TAFSILI</c>) — a rule
/// violation here is a configuration conflict in existing data, not something the caller's request
/// body could fix, so 409 rather than the 400 <c>IVoucherTafsiliLevelGuard</c> callers normally get
/// mapped to.
/// </summary>
public sealed class PaymentRequestVoucherTafsiliMissingException : Exception
{
    public PaymentRequestVoucherTafsiliMissingException(Guid accountCodeId, string ruleDetail)
        : base($"بخش ۴-ب voucher line for account {accountCodeId} fails تفصیلی الزامی: {ruleDetail}")
    {
        AccountCodeId = accountCodeId;
        PublicDetail = $"برای حساب مرتبط با یکی از ردیف‌های سند: {ruleDetail}";
    }

    public Guid AccountCodeId { get; }

    public string PublicDetail { get; }
}
