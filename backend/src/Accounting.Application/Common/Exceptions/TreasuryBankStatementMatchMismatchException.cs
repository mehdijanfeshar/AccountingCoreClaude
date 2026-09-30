namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by manual <c>match</c> (<c>POST statements/{id}/lines/{lineId}/match</c>) when the
/// candidate <c>TB_VOUCHERSDETAIL</c> row's direction (debit for a deposit, credit for a
/// withdrawal) or amount does not equal the صورت‌حساب line — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). <b>400</b> — an input mismatch, not a state
/// conflict.
/// </summary>
public sealed class TreasuryBankStatementMatchMismatchException : Exception
{
    public TreasuryBankStatementMatchMismatchException(string reason)
        : base($"Manual match rejected: {reason}")
    {
        Reason = reason;
    }

    public string Reason { get; }

    public string PublicDetail => Reason;
}
