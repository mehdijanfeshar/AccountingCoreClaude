namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>resolve</c> (<c>POST statements/{id}/lines/{lineId}/resolve</c>) when the
/// requested resolution is not valid for this line — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰): <c>BankFeeVoucher</c> requested on a deposit line
/// (or vice versa for <c>LinkedReceipt</c> on a withdrawal), <c>Ignored</c> without
/// <c>note</c>, or a <c>LinkedReceipt</c> whose target receipt is not <c>Registered</c>/does not
/// match this statement's bank account/amount. <b>400</b> — an input rule violation, not a state
/// conflict (those are <see cref="TreasuryBankStatementLineStateConflictException"/>).
/// </summary>
public sealed class TreasuryBankStatementResolutionInvalidException : Exception
{
    public TreasuryBankStatementResolutionInvalidException(string reason)
        : base($"Resolution rejected: {reason}")
    {
        Reason = reason;
    }

    public string Reason { get; }

    public string PublicDetail => Reason;
}
