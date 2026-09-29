using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.RejectPaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests/{id}/reject</c> — terminally rejects a درخواست پرداخت
/// (<see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Rejected"/>, no ترمیم/بازگشت
/// ممکن). <see cref="Reason"/> is mandatory. Same role/SoD gate as
/// <c>ApprovePaymentRequestCommand</c>, minus the consecutive-approver check.
/// </summary>
public sealed record RejectPaymentRequestCommand(Guid Id, string Reason) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
