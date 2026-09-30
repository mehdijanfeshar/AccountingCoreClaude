using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeleteBankStatement;

/// <summary><c>POST api/treasury/statements/{id}/delete</c> — soft delete, cascading to every
/// active line. Only while <see cref="Accounting.Domain.ValueObjects.BankStatementState.Open"/>.</summary>
public sealed record DeleteBankStatementCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
