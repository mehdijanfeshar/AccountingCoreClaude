using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// <c>GET api/petty-cash/expense-docs/{id}</c> — the list item shape (see
/// <see cref="PettyCashExpenseDocListItemDto"/>) plus the fields only needed on the single-document
/// view/edit form. Per <c>docs/tankhah-khazaneh-module.md</c> §5.
/// </summary>
/// <param name="VendorNationalId">TB_PC_EXPENSE_DOC.VENDOR_NATIONAL_ID.</param>
/// <param name="InvoiceNo">TB_PC_EXPENSE_DOC.INVOICE_NO.</param>
/// <param name="InvoiceDate">TB_PC_EXPENSE_DOC.INVOICE_DATE (YYYYMMDD).</param>
/// <param name="EvidenceType">TB_PC_EXPENSE_DOC.EVIDENCE_TYPE.</param>
/// <param name="AmountBeforeTax">TB_PC_EXPENSE_DOC.AMOUNT_BEFORE_TAX.</param>
/// <param name="VatAmount">TB_PC_EXPENSE_DOC.VAT_AMOUNT.</param>
/// <param name="ReturnDeadline">TB_PC_EXPENSE_DOC.RETURN_DEADLINE (YYYYMMDD) — بخش ۲ only; always null in chunk 1.</param>
/// <param name="VerifiedByUserId">TB_PC_EXPENSE_DOC.VERIFIED_BY_USERID — تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸).</param>
/// <param name="VerifiedDate">TB_PC_EXPENSE_DOC.VERIFIED_DATE.</param>
/// <param name="EditableFields">تکمیل بخش ۲، «قفل فیلدبه‌فیلد» (صفحهٔ ۸): the exact camelCase body
/// field names (plus <c>"attachments"</c>) the caller may currently change, computed from the
/// document's most recent Return event's reasons via
/// <c>Accounting.Application.Common.Security.PettyCashReturnFieldPolicy</c> — only ever non-null
/// while <paramref name="State"/> is <see cref="PettyCashDocState.Returned"/>.
/// <see langword="null"/> means unrestricted (the normal Draft/Returned editability rule
/// applies).</param>
public sealed record PettyCashExpenseDocDto(
    Guid Id,
    string DocNumber,
    string Code,
    string? RegisterDate,
    Guid FundId,
    string? FundName,
    Guid ExpenseId,
    string? ExpenseName,
    string? VendorName,
    string? Description,
    decimal TotalAmount,
    PettyCashDocState State,
    DateTime? SubmittedDate,
    int? AgeDays,
    string AddUserId,
    string? VendorNationalId,
    string? InvoiceNo,
    string? InvoiceDate,
    PettyCashEvidenceType? EvidenceType,
    decimal? AmountBeforeTax,
    decimal? VatAmount,
    string? ReturnDeadline,
    string? VerifiedByUserId,
    DateTime? VerifiedDate,
    IReadOnlyList<string>? EditableFields);
