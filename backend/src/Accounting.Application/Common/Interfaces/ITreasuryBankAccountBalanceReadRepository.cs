namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Computes a bank account's GL balance directly from <c>TB_VOUCHERSDETAIL</c> — خزانه‌داری، بخش
/// ۴-ج (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Backs both <c>GET
/// bank-accounts/{id}/balance</c> and انتقال وجه <c>approve</c>'s blocking source-balance check, so
/// the UI's "current/after transfer" figures and the server's own blocking decision can never
/// disagree.
///
/// <b>Formula (owner decision ۲۰۲۶-۰۹-۲۹, documented choice):</b> <c>SUM(DEBTOR) - SUM(CREDITOR)</c>
/// over every non-deleted <c>TB_VOUCHERSDETAIL</c> row whose <c>ACCOUNT_ID</c> is the bank
/// account's معین (<c>TB_ACCOUNT.ACCOUNTCODE_ID</c>), scoped to one (unit, year) — <b>temporary
/// vouchers included</b> (<c>DOCLIFE</c> is not filtered). Several bank accounts usually share one
/// bank معین, so lines are further restricted to those carrying <b>every</b> تفصیلی of the bank
/// account (<c>TB_ACCOUNT_LINK_TAFSILI</c>, the same links the voucher builders write on bank
/// lines). Only a bank account with no تفصیلی link falls back to the whole معین.
/// </summary>
public interface ITreasuryBankAccountBalanceReadRepository
{
    /// <summary>
    /// <paramref name="accountCodeId"/> is the bank account's <c>TB_ACCOUNT.ACCOUNTCODE_ID</c>
    /// (معین), not <c>TB_ACCOUNT.ID</c> — the caller resolves that first via
    /// <see cref="IBankAccountReadRepository"/>. Returns <c>0</c> (never throws) when the معین has
    /// no voucher lines yet for the given year.
    /// </summary>
    Task<decimal> GetBalanceAsync(
        Guid accountCodeId,
        IReadOnlyCollection<Guid> bankTafsiliIds,
        string vahedCode,
        string year,
        CancellationToken cancellationToken = default);
}
