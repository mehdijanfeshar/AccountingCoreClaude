using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CancelReceipt;

/// <summary>
/// <c>POST api/treasury/receipts/{id}/cancel</c> — moves a دریافت وجه to
/// <see cref="Accounting.Domain.ValueObjects.ReceiptState.Cancelled"/>. Only allowed while
/// <see cref="Accounting.Domain.ValueObjects.ReceiptState.Draft"/> — a Registered receipt is
/// corrected via its own GL voucher, not reopened here (owner decision ۲۰۲۶-۰۹-۲۹: «keep it
/// simple»).
/// </summary>
public sealed record CancelReceiptCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
