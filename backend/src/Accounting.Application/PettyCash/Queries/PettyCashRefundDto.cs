namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// <c>GET funds/{fundId}/refunds</c> row shape — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>،
/// صفحهٔ ۱۱ پاورپوینت).
/// </summary>
/// <param name="Id">TB_PC_REFUND.ID.</param>
/// <param name="FundId">TB_PC_REFUND.FUND_ID.</param>
/// <param name="Code">TB_PC_REFUND.CODE ("REF-xxxxx").</param>
/// <param name="Amount">TB_PC_REFUND.AMOUNT.</param>
/// <param name="Reason">TB_PC_REFUND.REASON.</param>
/// <param name="RefundDate">TB_PC_REFUND.REFUND_DATE (شمسی YYYYMMDD).</param>
/// <param name="RecordedByUserId">TB_PC_REFUND.RECORDED_BY_USERID.</param>
/// <param name="CreatedDate">TB_PC_REFUND.CREATEDDATE.</param>
public sealed record PettyCashRefundDto(
    Guid Id,
    Guid FundId,
    string Code,
    decimal Amount,
    string? Reason,
    string RefundDate,
    string RecordedByUserId,
    DateTime CreatedDate);
