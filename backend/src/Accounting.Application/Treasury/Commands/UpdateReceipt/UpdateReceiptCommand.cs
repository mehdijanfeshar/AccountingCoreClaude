using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdateReceipt;

/// <summary>
/// <c>POST api/treasury/receipts/{id}/update</c> — full replace. Only allowed while
/// <see cref="Accounting.Domain.ValueObjects.ReceiptState.Draft"/> — «keep it simple» (owner
/// decision ۲۰۲۶-۰۹-۲۹): no creator-only SoD gate the way درخواست پرداخت has, only a state check.
/// <c>Id</c> is taken from the route.
/// </summary>
public sealed record UpdateReceiptCommand(
    Guid Id,
    Guid PayerTafsiliId,
    decimal Amount,
    Guid BankAccountId,
    TreasuryPaymentMethod ReceiptMethod,
    string ReceiptDate,
    string BankReference,
    string? InvoiceRef,
    string? Description) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
