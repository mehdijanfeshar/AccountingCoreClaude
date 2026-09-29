using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ReturnTransfer;

/// <summary>
/// <c>POST api/treasury/transfers/{id}/return</c> — sends an انتقال وجه back to its creator for
/// correction. <see cref="Reason"/> is mandatory. Same role/SoD gate as
/// <c>ApproveTransferCommand</c>.
/// </summary>
public sealed record ReturnTransferCommand(Guid Id, string Reason) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
