using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeleteTransfer;

/// <summary>
/// <c>POST api/treasury/transfers/{id}/delete</c> — soft-deletes an انتقال وجه. Only allowed while
/// <see cref="Accounting.Domain.ValueObjects.TransferState.Draft"/>/<see cref="Accounting.Domain.ValueObjects.TransferState.Returned"/>.
/// </summary>
public sealed record DeleteTransferCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
