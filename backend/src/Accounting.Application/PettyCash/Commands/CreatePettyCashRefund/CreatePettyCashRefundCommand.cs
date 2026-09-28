using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashRefund;

/// <summary>
/// <c>POST api/petty-cash/refunds</c> — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>، صفحهٔ ۱۱
/// پاورپوینت). Records a استرداد وجه for a تنخواه. Who may call this is entirely determined by
/// <c>TB_PC_FUND.REFUND_RECORDER</c> — see <c>CreatePettyCashRefundCommandHandler</c>.
/// </summary>
/// <param name="FundId">TB_PC_REFUND.FUND_ID.</param>
/// <param name="Amount">TB_PC_REFUND.AMOUNT.</param>
/// <param name="RefundDate">TB_PC_REFUND.REFUND_DATE (شمسی YYYYMMDD).</param>
/// <param name="Reason">TB_PC_REFUND.REASON.</param>
public sealed record CreatePettyCashRefundCommand(
    Guid FundId,
    decimal Amount,
    string RefundDate,
    string? Reason) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
