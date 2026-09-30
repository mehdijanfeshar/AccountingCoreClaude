using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreateBankStatement;

/// <summary>
/// <c>POST api/treasury/statements</c> — creates a صورت‌حساب بانکی as
/// <see cref="Accounting.Domain.ValueObjects.BankStatementState.Open"/>, always with
/// <see cref="Accounting.Domain.ValueObjects.BankStatementSource.Manual"/> (owner decision
/// ۲۰۲۶-۰۹-۲۹ — this endpoint is the manual-entry path; <c>POST statements/{id}/import</c> is the
/// separate pluggable-file path, خزانه‌داری بخش ۴-د، <c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
public sealed record CreateBankStatementCommand(
    Guid BankAccountId,
    string FromDate,
    string ToDate,
    decimal ClosingBalance,
    string? Description,
    string Year) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
