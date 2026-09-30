using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdateBankStatement;

/// <summary><c>POST api/treasury/statements/{id}/update</c> — full replace. Only while
/// <see cref="Accounting.Domain.ValueObjects.BankStatementState.Open"/>.</summary>
public sealed record UpdateBankStatementCommand(
    Guid Id,
    Guid BankAccountId,
    string FromDate,
    string ToDate,
    decimal ClosingBalance,
    string? Description) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
