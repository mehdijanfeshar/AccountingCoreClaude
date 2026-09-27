using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs</c> — creates a صورت‌هزینهٔ تنخواه (TH-xxxxx) as a
/// پیش‌نویس, or (when <see cref="Submit"/> is <see langword="true"/>) submits it straight to
/// <see cref="PettyCashDocState.New"/>, applying the same rules
/// <c>SubmitPettyCashExpenseDocCommand</c> applies (<c>docs/tankhah-khazaneh-module.md</c> §4/§5).
///
/// In a single transaction this stages: one <c>TB_CHARGEANDCOST_HEAD</c> (type هزینه‌کرد), one
/// <c>TB_CHARGEANDCOST_DETAIL</c> (Legacy detail — always exactly one, per
/// <c>docs/centralaccount-business-reference.md</c> §24-5-4), one <c>TB_PC_EXPENSE_DOC</c>, and
/// one <c>TB_PC_DOC_EVENT</c> (action <see cref="PettyCashDocAction.Create"/>).
///
/// <c>Year</c> is the fiscal year the caller selected in the session (same as voucher entry —
/// owner decision 2026-09-27), not derived from <c>RegisterDate</c>.
/// </summary>
public sealed record CreatePettyCashExpenseDocCommand(
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
    string? Description,
    bool Submit) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
