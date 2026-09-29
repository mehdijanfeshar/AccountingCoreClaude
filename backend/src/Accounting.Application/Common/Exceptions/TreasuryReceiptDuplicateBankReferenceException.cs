namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when <c>TB_TR_RECEIPT.BANK_REFERENCE</c> already exists among the live (non-deleted,
/// non-<see cref="Accounting.Domain.ValueObjects.ReceiptState.Cancelled"/>) receipts of the same
/// <c>BANK_ACCOUNT_ID</c> — application-level duplicate rule, no DB UNIQUE constraint backs it
/// (owner decision ۲۰۲۶-۰۹-۲۹؛ same posture as every other application-level uniqueness rule in
/// this project).
/// </summary>
public sealed class TreasuryReceiptDuplicateBankReferenceException : Exception
{
    public TreasuryReceiptDuplicateBankReferenceException(string bankReference, Guid bankAccountId)
        : base($"Bank reference '{bankReference}' already used by a live receipt of bank account {bankAccountId}.")
    {
        BankReference = bankReference;
        BankAccountId = bankAccountId;
    }

    public string BankReference { get; }

    public Guid BankAccountId { get; }

    public string PublicDetail => "این شمارهٔ مرجع بانکی قبلاً برای دریافت دیگری از همین حساب بانکی ثبت شده است.";
}
