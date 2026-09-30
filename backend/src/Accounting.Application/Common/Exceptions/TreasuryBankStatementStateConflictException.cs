using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a صورت‌حساب بانکی is not in the <c>STATE</c> an action (header/line CRUD, match/
/// unmatch/resolve/unresolve, auto-match, close) requires — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Same state-based-refusal shape as
/// <c>TreasuryReceiptStateConflictException</c>. <b>409.</b>
/// </summary>
public sealed class TreasuryBankStatementStateConflictException : Exception
{
    public TreasuryBankStatementStateConflictException(Guid statementId, BankStatementState actualState, string requiredStateDescription)
        : base($"BankStatement {statementId} is in state {actualState}, but this action requires {requiredStateDescription}.")
    {
        StatementId = statementId;
        ActualState = actualState;
    }

    public Guid StatementId { get; }

    public BankStatementState ActualState { get; }

    public string PublicDetail => $"صورت‌حساب بانکی در وضعیت «{StateLabel(ActualState)}» است و این اقدام روی آن ممکن نیست.";

    internal static string StateLabel(BankStatementState state) => state switch
    {
        BankStatementState.Open => "باز",
        BankStatementState.Closed => "بسته",
        _ => "نامشخص",
    };
}
