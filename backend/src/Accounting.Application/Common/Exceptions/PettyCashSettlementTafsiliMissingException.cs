namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Wraps <c>TafsiliLevelRuleException</c> (which <c>GlobalExceptionHandler</c> maps to 400 for
/// the ordinary voucher write paths) into a settlement-specific, <b>409</b> exception — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9): "اگر گارد رد کرد ⇒ ۴۰۹ با پیام مشخص کدام
/// حساب تفصیلی کم دارد". 409 rather than 400 here specifically because the caller did not send
/// any تفصیلی data themselves — the settlement voucher's lines are entirely server-derived from
/// <c>TB_EXPENCE_LINK_TAFSILI</c>/<c>TB_PC_FUND_LINK_TAFSILI</c>, so a rule violation here is a
/// configuration conflict in existing data, not a malformed request (400 would wrongly imply the
/// caller could fix it by sending different input).
/// </summary>
public sealed class PettyCashSettlementTafsiliMissingException : Exception
{
    public PettyCashSettlementTafsiliMissingException(Guid accountCodeId, string ruleDetail)
        : base($"Settlement voucher line for account {accountCodeId} fails تفصیلی الزامی: {ruleDetail}")
    {
        AccountCodeId = accountCodeId;
        PublicDetail = $"برای حساب مرتبط با یکی از ردیف‌های سند تسویه: {ruleDetail}";
    }

    public Guid AccountCodeId { get; }

    public string PublicDetail { get; }
}
