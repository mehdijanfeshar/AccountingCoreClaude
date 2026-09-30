using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a صورت‌حساب بانکی line's <c>MATCH_STATE</c> does not allow the requested action
/// (<c>match</c> requires <see cref="BankStatementLineMatchState.Unmatched"/>; <c>unmatch</c>
/// requires <see cref="BankStatementLineMatchState.AutoMatched"/>/<see cref="BankStatementLineMatchState.ManualMatched"/>;
/// <c>resolve</c> requires <see cref="BankStatementLineMatchState.Unmatched"/>; <c>unresolve</c>
/// requires <see cref="BankStatementLineMatchState.Resolved"/>) — خزانه‌داری، بخش ۴-د. <b>409.</b>
/// </summary>
public sealed class TreasuryBankStatementLineStateConflictException : Exception
{
    public TreasuryBankStatementLineStateConflictException(Guid lineId, BankStatementLineMatchState actualState, string requiredStateDescription)
        : base($"BankStatementLine {lineId} is in match state {actualState}, but this action requires {requiredStateDescription}.")
    {
        LineId = lineId;
        ActualState = actualState;
    }

    public Guid LineId { get; }

    public BankStatementLineMatchState ActualState { get; }

    public string PublicDetail => $"ردیف صورت‌حساب در وضعیت تطبیق «{StateLabel(ActualState)}» است و این اقدام روی آن ممکن نیست.";

    internal static string StateLabel(BankStatementLineMatchState state) => state switch
    {
        BankStatementLineMatchState.Unmatched => "نامنطبق",
        BankStatementLineMatchState.AutoMatched => "تطبیق‌خودکار",
        BankStatementLineMatchState.ManualMatched => "تطبیق‌دستی",
        BankStatementLineMatchState.Resolved => "حل‌شده",
        _ => "نامشخص",
    };
}
