using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeleteBankStatementLine;

/// <summary><c>POST api/treasury/statements/{id}/lines/{lineId}/delete</c> — soft delete. Only
/// while the statement is Open AND the line is still
/// <see cref="Accounting.Domain.ValueObjects.BankStatementLineMatchState.Unmatched"/>.</summary>
public sealed record DeleteBankStatementLineCommand(Guid StatementId, Guid LineId) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
