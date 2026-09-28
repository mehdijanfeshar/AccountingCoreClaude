using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.RecordPettyCashReplenishmentPayment;

/// <summary>
/// <c>POST api/petty-cash/replenishments/{id}/record-payment</c> — moves a ترمیم from
/// <see cref="PettyCashReplenishmentState.PendingTreasurer"/> to
/// <see cref="PettyCashReplenishmentState.Paid"/>. Caller must hold
/// <see cref="Accounting.Domain.ValueObjects.PettyCashRole.Treasurer"/> for the ترمیم's fund and
/// must not be its own approver (بخش ۳-الف، <c>docs/tankhah-khazaneh-module.md</c>: «فقط
/// خزانه‌دار؛ ≠ تأییدکننده»).
///
/// <b>⚠️ Temporary, per design §۳-الف.</b> This only flips <c>STATE</c> — it does <b>not</b> issue
/// any GL voucher and does not touch a bank account balance. صفحهٔ ۱۰ پاورپوینت places the actual
/// ترمیم accounting entry (بدهکار تنخواه / بستانکار بانک) at خزانه's اجرای پرداخت step (بخش ۴+),
/// which this batch does not implement. When that lands, it should replace (or wrap) this action
/// rather than stack a second, separate "mark paid" step.
/// </summary>
/// <param name="Id">TB_PC_REPLENISHMENT.ID.</param>
/// <param name="PaidDate">Optional override for TB_PC_REPLENISHMENT.PAID_DATE — defaults to
/// <see cref="DateTime.UtcNow"/> (server time) when omitted, same as every other Legacy audit
/// timestamp in this project.</param>
public sealed record RecordPettyCashReplenishmentPaymentCommand(Guid Id, DateTime? PaidDate) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
