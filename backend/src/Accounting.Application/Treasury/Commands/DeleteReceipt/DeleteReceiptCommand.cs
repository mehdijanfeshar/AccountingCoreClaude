using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeleteReceipt;

/// <summary>
/// <c>POST api/treasury/receipts/{id}/delete</c> — soft-deletes a دریافت وجه. Only allowed while
/// <see cref="Accounting.Domain.ValueObjects.ReceiptState.Draft"/>.
/// </summary>
public sealed record DeleteReceiptCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
