using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// Read-side projection of one <c>TB_PC_FUND</c> row plus its computed §2 balance summary. Used by
/// both <c>GET api/petty-cash/funds</c> (list) and <c>GET api/petty-cash/funds/{fundId}</c>
/// (single) — per <c>docs/tankhah-khazaneh-module.md</c> §5. The Domain entity never crosses the
/// Application boundary (CLAUDE.md rule 6).
/// </summary>
/// <param name="Id">TB_PC_FUND.ID.</param>
/// <param name="Code">TB_PC_FUND.CODE.</param>
/// <param name="Name">TB_PC_FUND.NAME.</param>
/// <param name="CustodianUserId">TB_PC_FUND.CUSTODIAN_USERID — تنخواه‌دار مسئول.</param>
/// <param name="CustodianName">TB_PC_FUND.CUSTODIAN_NAME.</param>
/// <param name="Ceiling">TB_PC_FUND.CEILING — سقف تنخواه.</param>
/// <param name="PerDocLimit">TB_PC_FUND.PER_DOC_LIMIT — سقف هر سند.</param>
/// <param name="FinanceManagerApprovalLimit">TB_PC_FUND.FINANCE_MANAGER_APPROVAL_LIMIT — سقف اختیار
/// تأیید نهایی نقش مدیر مالی؛ بیشتر از این فقط مدیرعامل می‌تواند تأیید نهایی کند (تکمیل بخش ۲،
/// ۲۰۲۶-۰۹-۲۸).</param>
/// <param name="AlertThresholdPercent">TB_PC_FUND.ALERT_THRESHOLD_PERCENT.</param>
/// <param name="AccountCodeId">TB_PC_FUND.ACCOUNTCODE_ID.</param>
/// <param name="AccountCodeTitle">Display-only: <c>TB_ACCOUNTCODE.ACCCODENAME</c> of the linked معین.</param>
/// <param name="SettlementPeriod">TB_PC_FUND.SETTLEMENT_PERIOD.</param>
/// <param name="IsActive">TB_PC_FUND.IS_ACTIVE — false blocks new صورت‌هزینه creation/Submit.</param>
/// <param name="IsDeleted">TB_PC_FUND.ISDELETED. Exposed as-is, same reasoning as <c>RevolvingFundDto.IsDeleted</c>.</param>
/// <param name="CashBalance">§2: <c>Ceiling − (ApprovedAmount + InFlightAmount)</c>.</param>
/// <param name="ApprovedAmount">Sum of documents currently تأییدشده (منتظر ترمیم).</param>
/// <param name="ApprovedCount">Count of the same set.</param>
/// <param name="InFlightAmount">Sum of documents currently جدید/در انتظار بررسی/برگشتی.</param>
/// <param name="InFlightCount">Count of the same set.</param>
/// <param name="RefundRecorder">TB_PC_FUND.REFUND_RECORDER — بخش ۳-الف (۲۰۲۶-۰۹-۲۸).</param>
public sealed record PettyCashFundDto(
    Guid Id,
    string Code,
    string Name,
    string CustodianUserId,
    string? CustodianName,
    decimal Ceiling,
    decimal PerDocLimit,
    decimal FinanceManagerApprovalLimit,
    int? AlertThresholdPercent,
    Guid? AccountCodeId,
    string? AccountCodeTitle,
    PettyCashSettlementPeriod? SettlementPeriod,
    bool IsActive,
    bool IsDeleted,
    decimal CashBalance,
    decimal ApprovedAmount,
    int ApprovedCount,
    decimal InFlightAmount,
    int InFlightCount,
    PettyCashRefundRecorder RefundRecorder);
