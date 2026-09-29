using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdateTransfer;

/// <summary>
/// <c>POST api/treasury/transfers/{id}/update</c> — full replace. Only allowed while
/// <see cref="Accounting.Domain.ValueObjects.TransferState.Draft"/>/<see cref="Accounting.Domain.ValueObjects.TransferState.Returned"/>.
/// <c>Id</c> is taken from the route.
/// </summary>
public sealed record UpdateTransferCommand(
    Guid Id,
    Guid SourceBankAccountId,
    Guid DestBankAccountId,
    decimal Amount,
    string TransferDate,
    TreasuryPaymentMethod TransferMethod,
    string Reason) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
