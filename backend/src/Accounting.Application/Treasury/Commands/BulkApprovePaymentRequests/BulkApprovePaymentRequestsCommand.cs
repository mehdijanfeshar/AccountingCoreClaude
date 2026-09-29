using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.BulkApprovePaymentRequests;

/// <summary>
/// <c>POST api/treasury/payment-requests/bulk-approve</c> — applies the same transition as
/// <c>ApprovePaymentRequestCommand</c> to every id in <see cref="Ids"/>, in one all-or-nothing
/// transaction, additionally refusing any id whose <c>NET_PAYABLE_AMOUNT</c> exceeds the unit's
/// <c>TB_TR_SETTING.BULK_APPROVE_LIMIT</c>. A 409 response's <c>failedIds</c> extension lists which
/// ids failed and why; none of the batch is approved when any one of them fails — same shape as
/// <c>BulkApprovePettyCashExpenseDocsCommand</c>.
/// </summary>
public sealed record BulkApprovePaymentRequestsCommand(IReadOnlyList<Guid> Ids) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
