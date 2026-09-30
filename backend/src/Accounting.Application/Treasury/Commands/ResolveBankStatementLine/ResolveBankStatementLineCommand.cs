using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ResolveBankStatementLine;

/// <summary><c>POST api/treasury/statements/{id}/lines/{lineId}/resolve</c> — see
/// <c>IBankStatementLineResolutionService.ResolveAsync</c> XML doc for the per-<see cref="Type"/>
/// rules.</summary>
public sealed record ResolveBankStatementLineCommand(
    Guid StatementId,
    Guid LineId,
    BankStatementLineResolutionType Type,
    Guid? ReceiptId,
    string? Note) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
