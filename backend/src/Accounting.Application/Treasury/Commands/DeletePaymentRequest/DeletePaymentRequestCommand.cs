using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeletePaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests/{id}/delete</c> — soft-deletes a درخواست پرداخت. Only
/// allowed while <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Draft"/>/
/// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Returned"/>, and only by its own
/// creator.
/// </summary>
public sealed record DeletePaymentRequestCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
