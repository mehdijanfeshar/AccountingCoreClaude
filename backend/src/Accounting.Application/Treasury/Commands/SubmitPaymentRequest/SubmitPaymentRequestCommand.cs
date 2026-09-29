using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.SubmitPaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests/{id}/submit</c> — moves a درخواست پرداخت from
/// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Draft"/>/
/// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Returned"/> to
/// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.PendingUnitManager"/> — always
/// restarting the approval chain from the first stage, even when re-submitting after a Return
/// (خزانه‌داری، بخش ۴-الف؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰). Only its own creator may
/// call this. Takes no body.
/// </summary>
public sealed record SubmitPaymentRequestCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
