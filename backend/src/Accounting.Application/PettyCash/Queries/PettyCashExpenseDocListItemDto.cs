using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// One row of <c>GET api/petty-cash/expense-docs</c> — the کارتابل list. Per
/// <c>docs/tankhah-khazaneh-module.md</c> §5. See <see cref="PettyCashExpenseDocDto"/> for the
/// superset shape returned by the single-document GET.
/// </summary>
/// <param name="Id">TB_PC_EXPENSE_DOC.ID.</param>
/// <param name="DocNumber"><c>"TH-" + Code</c>.</param>
/// <param name="Code">TB_CHARGEANDCOST_HEAD.CHARGEANDCOST_CODE (zero-padded, 5 digits).</param>
/// <param name="RegisterDate">TB_CHARGEANDCOST_HEAD.CHARGEANDCOST_DATE (YYYYMMDD).</param>
/// <param name="FundId">TB_PC_EXPENSE_DOC.FUND_ID.</param>
/// <param name="FundName">Display-only: TB_PC_FUND.NAME.</param>
/// <param name="ExpenseId">TB_CHARGEANDCOST_DETAIL.EXPENSE_ID.</param>
/// <param name="ExpenseName">Display-only: TB_EXPENCE.EXPENCENAME.</param>
/// <param name="VendorName">TB_PC_EXPENSE_DOC.VENDOR_NAME.</param>
/// <param name="Description">TB_CHARGEANDCOST_HEAD.DESCRIPTION.</param>
/// <param name="TotalAmount">AmountBeforeTax + VatAmount.</param>
/// <param name="State">TB_PC_EXPENSE_DOC.DOC_STATE.</param>
/// <param name="SubmittedDate">TB_PC_EXPENSE_DOC.SUBMITTED_DATE.</param>
/// <param name="AgeDays">Days since <paramref name="SubmittedDate"/>, or <see langword="null"/> for <see cref="PettyCashDocState.Draft"/> (which is never submitted).</param>
/// <param name="AddUserId">TB_PC_EXPENSE_DOC.ADDUSERID.</param>
/// <param name="VerifiedByUserId">TB_PC_EXPENSE_DOC.VERIFIED_BY_USERID — تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸).</param>
/// <param name="VerifiedDate">TB_PC_EXPENSE_DOC.VERIFIED_DATE.</param>
public sealed record PettyCashExpenseDocListItemDto(
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
    string? VerifiedByUserId,
    DateTime? VerifiedDate);
