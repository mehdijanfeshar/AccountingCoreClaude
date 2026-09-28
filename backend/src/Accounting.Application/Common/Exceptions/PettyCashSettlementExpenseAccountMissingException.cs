namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// A مادهٔ هزینه (<c>TB_EXPENCE</c>) whose group of approved صورت‌هزینه rows would become a
/// settlement voucher debit line has no <c>ACCOUNTCODE_ID</c> configured, so that line cannot be
/// built. Not explicitly named in the design doc (which only calls out the fund's own missing
/// حساب معین) but the same underlying problem one level down — a GL line needs an account, full
/// stop.
/// </summary>
public sealed class PettyCashSettlementExpenseAccountMissingException : Exception
{
    public PettyCashSettlementExpenseAccountMissingException(Guid expenseId)
        : base($"Expense type {expenseId} has no ACCOUNTCODE_ID configured; cannot build its settlement voucher debit line.")
    {
        ExpenseId = expenseId;
    }

    public Guid ExpenseId { get; }

    public string PublicDetail => "یکی از ماده‌های هزینهٔ منظورشده حساب معین تعریف‌شده ندارد؛ ابتدا آن را تنظیم کنید.";
}
