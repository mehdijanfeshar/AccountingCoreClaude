using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ReopenBankStatement;

/// <summary><c>POST api/treasury/statements/{id}/reopen</c> — <see cref="Accounting.Domain.ValueObjects.BankStatementState.Closed"/>
/// → <see cref="Accounting.Domain.ValueObjects.BankStatementState.Open"/>.</summary>
public sealed record ReopenBankStatementCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
