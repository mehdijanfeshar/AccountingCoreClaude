using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Commands.PettyCash.ApprovePettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/approve</c> — «تأیید نهایی» (تکمیل بخش ۲، ۲۰۲۶-۰۹-۲۸):
/// moves a صورت‌هزینه from <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.PendingReview"/>
/// to <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Approved"/> ("منتظر ترمیم") —
/// only once an inspector has already <c>Verify</c>-ed it. See
/// <see cref="Accounting.Application.Commands.PettyCash.Common.IPettyCashFinalApprovalService"/>
/// for the full rule set (verified-check, second SoD against the verifier, amount-vs-authority).
/// </summary>
public sealed record ApprovePettyCashExpenseDocCommand(Guid Id, string? Note) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
