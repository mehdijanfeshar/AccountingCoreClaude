using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpdatePettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/update</c> — replaces every writable field of an
/// existing صورت‌هزینه and keeps the Legacy <c>TB_CHARGEANDCOST_HEAD</c>/<c>TB_CHARGEANDCOST_DETAIL</c>
/// pair in sync in the same transaction. Same body as
/// <see cref="Accounting.Application.PettyCash.Commands.CreatePettyCashExpenseDoc.CreatePettyCashExpenseDocCommand"/>
/// minus <c>Submit</c> — this endpoint never changes <c>DOC_STATE</c>; use
/// <c>SubmitPettyCashExpenseDocCommand</c> for that. Only allowed while the document is
/// <see cref="PettyCashDocState.Draft"/> or <see cref="PettyCashDocState.Returned"/> — see
/// <see cref="Accounting.Application.Common.Security.PettyCashDocEditability"/>.
/// </summary>
public sealed record UpdatePettyCashExpenseDocCommand(
    Guid Id,
    Guid FundId,
    Guid ExpenseId,
    string RegisterDate,
    string Year,
    string VendorName,
    string? VendorNationalId,
    string? InvoiceNo,
    string? InvoiceDate,
    PettyCashEvidenceType? EvidenceType,
    decimal AmountBeforeTax,
    decimal VatAmount,
    string? Description) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
