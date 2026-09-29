using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.SuspendPaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests/{id}/suspend</c> — خزانه‌داری، بخش ۴-ب. Moves
/// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.ReadyForExecution"/> →
/// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Suspended"/>. سند شناسایی بدهی
/// دست‌نخورده می‌ماند. Only <see cref="Accounting.Domain.ValueObjects.TreasuryRole.Treasurer"/>.
/// <see cref="Reason"/> is mandatory.
/// </summary>
public sealed record SuspendPaymentRequestCommand(Guid Id, string Reason) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
