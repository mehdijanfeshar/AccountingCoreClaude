using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// <c>GET replenishments?...</c> row shape — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>).
/// </summary>
/// <param name="Id">TB_PC_REPLENISHMENT.ID.</param>
/// <param name="Code">TB_PC_REPLENISHMENT.CODE ("RCH-xxxxx").</param>
/// <param name="FundId">TB_PC_REPLENISHMENT.FUND_ID.</param>
/// <param name="FundName">Display-only: TB_PC_FUND.NAME.</param>
/// <param name="PaymentMethod">TB_PC_REPLENISHMENT.PAYMENT_METHOD.</param>
/// <param name="State">TB_PC_REPLENISHMENT.STATE.</param>
/// <param name="TotalAmount">TB_PC_REPLENISHMENT.TOTAL_AMOUNT.</param>
/// <param name="DocCount">Number of صورت‌هزینه documents linked to this ترمیم.</param>
/// <param name="CreatedDate">TB_PC_REPLENISHMENT.CREATEDDATE.</param>
/// <param name="PaidDate">TB_PC_REPLENISHMENT.PAID_DATE.</param>
/// <param name="AddUserId">TB_PC_REPLENISHMENT.ADDUSERID.</param>
public sealed record PettyCashReplenishmentListItemDto(
    Guid Id,
    string Code,
    Guid FundId,
    string FundName,
    PettyCashPaymentMethod PaymentMethod,
    PettyCashReplenishmentState State,
    decimal TotalAmount,
    int DocCount,
    DateTime CreatedDate,
    DateTime? PaidDate,
    string AddUserId);

/// <summary>
/// <c>GET replenishments/{id}</c> — full detail, including the per-حساب lines and the linked
/// صورت‌هزینه ids.
/// </summary>
/// <param name="SourceBankAccountId">حساب بانکی مبدأ — <c>TB_CHARGEANDCOST_HEAD.ACCOUNT_ID</c>
/// (design §۳-الف: «هر ترمیم یک TB_CHARGEANDCOST_HEAD نوع ۱ است که ACCOUNT_ID آن = حساب بانکی
/// مبدأ»). Stored on the Legacy head, not on this side table.</param>
/// <param name="SourceBankAccountNumber">Display-only: TB_ACCOUNT.ACCOUNTNUMBER.</param>
/// <param name="Note">TB_PC_REPLENISHMENT.NOTE.</param>
/// <param name="PaidByUserId">TB_PC_REPLENISHMENT.PAID_BY_USERID.</param>
/// <param name="ApprovedByUserId">TB_PC_REPLENISHMENT.APPROVED_BY_USERID.</param>
/// <param name="Lines">همان خطوط <c>replenishment-preview</c>، snapshot شده در لحظهٔ ساخت.</param>
/// <param name="DocIds">هر صورت‌هزینهٔ لینک‌شده به این ترمیم.</param>
public sealed record PettyCashReplenishmentDto(
    Guid Id,
    string Code,
    Guid FundId,
    string FundName,
    Guid? SourceBankAccountId,
    string? SourceBankAccountNumber,
    PettyCashPaymentMethod PaymentMethod,
    PettyCashReplenishmentState State,
    decimal TotalAmount,
    string? Note,
    DateTime? PaidDate,
    string? PaidByUserId,
    string? ApprovedByUserId,
    string AddUserId,
    DateTime CreatedDate,
    IReadOnlyList<PettyCashReplenishmentLineDto> Lines,
    IReadOnlyList<Guid> DocIds);
