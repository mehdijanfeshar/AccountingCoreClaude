namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Defensive guard: the settlement voucher's total debtor does not equal its total credit —
/// closes open risk #3 for this write path (<c>CLAUDE.md</c>). By construction (the single credit
/// line's amount is always set to the sum of every debit line —
/// <c>PettyCashSettlementVoucherBuilder</c>) this can never actually fire; it exists so a future
/// change to that construction cannot silently post an unbalanced GL voucher.
/// </summary>
public sealed class PettyCashSettlementUnbalancedException : Exception
{
    public PettyCashSettlementUnbalancedException(decimal totalDebtor, decimal totalCredit)
        : base($"Settlement voucher is unbalanced: debtor {totalDebtor} != credit {totalCredit}.")
    {
        TotalDebtor = totalDebtor;
        TotalCredit = totalCredit;
    }

    public decimal TotalDebtor { get; }

    public decimal TotalCredit { get; }

    public string PublicDetail => "سند تسویه تراز نیست؛ عملیات متوقف شد.";
}
