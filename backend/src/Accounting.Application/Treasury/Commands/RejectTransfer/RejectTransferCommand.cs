using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.RejectTransfer;

/// <summary>
/// <c>POST api/treasury/transfers/{id}/reject</c> — رد پایانی. <see cref="Reason"/> is mandatory.
/// Same role/SoD gate as <c>ApproveTransferCommand</c>.
/// </summary>
public sealed record RejectTransferCommand(Guid Id, string Reason) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
