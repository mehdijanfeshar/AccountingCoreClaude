using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdateBankStatementLine;

/// <summary><c>POST api/treasury/statements/{id}/lines/{lineId}/update</c> — full replace. Only
/// while the statement is Open AND the line is still
/// <see cref="Accounting.Domain.ValueObjects.BankStatementLineMatchState.Unmatched"/> (این
/// پیاده‌سازی — ویرایش مبلغ/تاریخ ردیفی که قبلاً تطبیق/حل شده خطرناک است).</summary>
public sealed record UpdateBankStatementLineCommand(
    Guid StatementId,
    Guid LineId,
    string LineDate,
    string? BankReference,
    string? Description,
    decimal Withdrawal,
    decimal Deposit,
    decimal? Balance) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
