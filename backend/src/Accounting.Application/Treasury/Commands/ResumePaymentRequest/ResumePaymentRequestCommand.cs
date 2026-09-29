using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ResumePaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests/{id}/resume</c> — خزانه‌داری، بخش ۴-ب. Moves
/// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Suspended"/> →
/// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.ReadyForExecution"/>. No body.
/// Only <see cref="Accounting.Domain.ValueObjects.TreasuryRole.Treasurer"/>.
/// </summary>
public sealed record ResumePaymentRequestCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
