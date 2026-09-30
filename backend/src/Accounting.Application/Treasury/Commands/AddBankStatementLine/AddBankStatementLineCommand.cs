using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.AddBankStatementLine;

/// <summary><c>POST api/treasury/statements/{id}/lines</c> — manual entry of one line. Only while
/// the statement is <see cref="Accounting.Domain.ValueObjects.BankStatementState.Open"/>. Exactly
/// one of <see cref="Withdrawal"/>/<see cref="Deposit"/> must be greater than zero.</summary>
public sealed record AddBankStatementLineCommand(
    Guid StatementId,
    string LineDate,
    string? BankReference,
    string? Description,
    decimal Withdrawal,
    decimal Deposit,
    decimal? Balance) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
