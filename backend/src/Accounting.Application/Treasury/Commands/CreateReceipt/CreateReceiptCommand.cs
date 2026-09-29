using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreateReceipt;

/// <summary>
/// <c>POST api/treasury/receipts</c> — creates a دریافت وجه as پیش‌نویس, or — when
/// <see cref="Register"/> is <see langword="true"/> — immediately registers it in the same call
/// (same "create + act" shortcut شکل as <c>CreatePaymentRequestCommand.Submit</c>).
/// </summary>
public sealed record CreateReceiptCommand(
    Guid PayerTafsiliId,
    decimal Amount,
    Guid BankAccountId,
    TreasuryPaymentMethod ReceiptMethod,
    string ReceiptDate,
    string BankReference,
    string? InvoiceRef,
    string? Description,
    string Year,
    bool Register) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
