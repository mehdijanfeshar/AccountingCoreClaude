using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UnresolveBankStatementLine;

/// <summary><c>POST api/treasury/statements/{id}/lines/{lineId}/unresolve</c>.</summary>
public sealed record UnresolveBankStatementLineCommand(Guid StatementId, Guid LineId) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
