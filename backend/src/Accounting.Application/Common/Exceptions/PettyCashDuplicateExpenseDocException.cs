namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// A Create/Update on <c>TB_PC_EXPENSE_DOC</c> whose (vendor national id, invoice number) pair
/// already matches another non-deleted, non-rejected document for the same unit
/// (<c>docs/tankhah-khazaneh-module.md</c> §4: "تکراری نبودن ... بین اسناد حذف‌نشده و ردنشده").
///
/// This is an application-level duplicate check (unlike <see cref="DuplicateKeyException"/>,
/// which wraps an Oracle ORA-00001 unique-constraint violation): there is no database UNIQUE
/// constraint backing it, because a rejected or soft-deleted document must NOT block reuse of the
/// same invoice number, which a plain UNIQUE index could not express. 409, since the request
/// itself is well-formed — it conflicts with another existing document, not with its own shape.
/// </summary>
public sealed class PettyCashDuplicateExpenseDocException : Exception
{
    public PettyCashDuplicateExpenseDocException(string vendorNationalId, string invoiceNo)
        : base($"An active petty-cash expense document already exists for vendor national id " +
               $"'{vendorNationalId}' and invoice number '{invoiceNo}'.")
    {
        VendorNationalId = vendorNationalId;
        InvoiceNo = invoiceNo;
    }

    public string VendorNationalId { get; }

    public string InvoiceNo { get; }

    /// <summary>Echoes the invoice number only — it is exactly what the caller just typed into
    /// the form, not a Legacy identifier.</summary>
    public string PublicDetail =>
        $"سند فعالی با همین شناسهٔ ملی فروشنده و شمارهٔ فاکتور «{InvoiceNo}» از قبل ثبت شده است.";
}
