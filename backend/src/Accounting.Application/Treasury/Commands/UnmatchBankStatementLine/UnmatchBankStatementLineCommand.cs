using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UnmatchBankStatementLine;

/// <summary><c>POST api/treasury/statements/{id}/lines/{lineId}/unmatch</c>.</summary>
public sealed record UnmatchBankStatementLineCommand(Guid StatementId, Guid LineId) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
