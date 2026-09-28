namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// <c>TB_PC_FUND.ACCOUNTCODE_ID</c> is null — the fund has no حساب معین تنخواه configured, so the
/// settlement voucher's credit line cannot be built — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9): "اگر تنخواه حساب معین ندارد ⇒ ۴۰۹ «ابتدا
/// حساب معین تنخواه را تعریف کنید»".
/// </summary>
public sealed class PettyCashSettlementFundAccountMissingException : Exception
{
    public PettyCashSettlementFundAccountMissingException(Guid fundId)
        : base($"Fund {fundId} has no ACCOUNTCODE_ID configured; cannot build a settlement voucher credit line.")
    {
        FundId = fundId;
    }

    public Guid FundId { get; }

    public string PublicDetail => "ابتدا حساب معین تنخواه را تعریف کنید.";
}
