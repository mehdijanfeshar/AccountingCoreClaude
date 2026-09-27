namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>SubmitPettyCashExpenseDocCommandHandler</c> when the document's
/// <c>INVOICE_DATE</c> does not fall in the caller's current fiscal year
/// (<c>docs/tankhah-khazaneh-module.md</c> §4: "تاریخ فاکتور در سال مالی جاری (INVOICE_DATE با
/// YEAR شروع شود)"). Dates are the project-wide zero-padded <c>YYYYMMDD</c> Jalali text
/// convention, so "starts with YEAR" is a plain string prefix check. 400: the request's own data
/// is invalid, not a conflict with another resource.
/// </summary>
public sealed class PettyCashInvoiceYearMismatchException : Exception
{
    public PettyCashInvoiceYearMismatchException(Guid expenseDocId, string? invoiceDate, string year)
        : base($"Petty-cash expense document {expenseDocId} invoice date '{invoiceDate}' does not " +
               $"fall in fiscal year '{year}'.")
    {
        ExpenseDocId = expenseDocId;
        InvoiceDate = invoiceDate;
        Year = year;
    }

    public Guid ExpenseDocId { get; }

    public string? InvoiceDate { get; }

    public string Year { get; }

    public string PublicDetail => "تاریخ فاکتور باید در سال مالی جاری باشد.";
}
