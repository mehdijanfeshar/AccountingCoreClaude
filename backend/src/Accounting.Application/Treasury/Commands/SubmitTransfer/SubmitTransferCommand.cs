using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.SubmitTransfer;

/// <summary>
/// <c>POST api/treasury/transfers/{id}/submit</c> — Draft/Returned →
/// <see cref="Accounting.Domain.ValueObjects.TransferState.PendingTreasurer"/>. بدون بدنه.
/// </summary>
public sealed record SubmitTransferCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
