using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Treasury.Commands.Common;
using MediatR;

namespace Accounting.Application.Treasury.Commands.AutoMatchBankStatement;

/// <summary><c>POST api/treasury/statements/{id}/auto-match</c> — see
/// <c>IBankStatementAutoMatchService</c> XML doc for the priority rules.</summary>
public sealed record AutoMatchBankStatementCommand(Guid StatementId) : IRequest<BankStatementAutoMatchResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
