using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ReturnPaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests/{id}/return</c> — sends a درخواست پرداخت back to its
/// creator for correction (<see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Returned"/>).
/// <see cref="Reason"/> is mandatory. Same role/SoD gate as
/// <c>ApprovePaymentRequestCommand</c>, minus the consecutive-approver check (not a progression
/// through the chain).
/// </summary>
public sealed record ReturnPaymentRequestCommand(Guid Id, string Reason) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
