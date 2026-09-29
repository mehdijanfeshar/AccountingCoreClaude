namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Computes <c>TB_TR_PAYMENT_REQUEST.VAT_AMOUNT</c>/<c>INSURANCE_DEDUCTION_AMOUNT</c>/
/// <c>NET_PAYABLE_AMOUNT</c> — خزانه‌داری، بخش ۴-الف (<c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// Shared by <c>CreatePaymentRequestCommandHandler</c> and
/// <c>UpdatePaymentRequestCommandHandler</c> so the two rules can never drift apart — same "one
/// rule, one home" shape as <c>VoucherTafsiliLevelGuard</c>.
///
/// <b>Design decision (this implementation, documented per the module brief's open question):</b>
/// when a percent is supplied, the server derives the amount from it and the caller-sent amount
/// for that line is ignored entirely (never trusted, never echoed back as an input value) — same
/// "server always derives from percent when one exists" precedent as every other Legacy
/// percent/amount pair in this codebase. When no percent is supplied, the caller-sent amount is
/// used as-is (validated non-negative by the command's own FluentValidation rule) — this lets a
/// caller record a flat VAT/insurance deduction amount when no clean percent applies (e.g. a
/// negotiated lump-sum withholding), without forcing a fabricated percent into the request.
/// <c>NET_PAYABLE_AMOUNT</c> is never a caller input either way — always
/// <c>AmountBeforeTax + VatAmount - InsuranceDeductionAmount</c>.
/// </summary>
public static class PaymentRequestAmountCalculator
{
    public static (decimal VatAmount, decimal InsuranceDeductionAmount, decimal NetPayableAmount) Compute(
        decimal amountBeforeTax,
        decimal? vatPercent,
        decimal? vatAmountInput,
        decimal? insuranceDeductionPercent,
        decimal? insuranceDeductionAmountInput)
    {
        var vatAmount = vatPercent.HasValue
            ? Math.Round(amountBeforeTax * vatPercent.Value / 100m, 0, MidpointRounding.AwayFromZero)
            : vatAmountInput ?? 0m;

        var insuranceDeductionAmount = insuranceDeductionPercent.HasValue
            ? Math.Round(amountBeforeTax * insuranceDeductionPercent.Value / 100m, 0, MidpointRounding.AwayFromZero)
            : insuranceDeductionAmountInput ?? 0m;

        var netPayableAmount = amountBeforeTax + vatAmount - insuranceDeductionAmount;

        return (vatAmount, insuranceDeductionAmount, netPayableAmount);
    }
}
