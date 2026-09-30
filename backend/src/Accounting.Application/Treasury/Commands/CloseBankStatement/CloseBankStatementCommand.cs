using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CloseBankStatement;

/// <summary><c>POST api/treasury/statements/{id}/close</c> — <see cref="Accounting.Domain.ValueObjects.BankStatementState.Open"/>
/// → <see cref="Accounting.Domain.ValueObjects.BankStatementState.Closed"/>. Allowed regardless of
/// unresolved lines (owner did not require blocking on that — این پیاده‌سازی).</summary>
public sealed record CloseBankStatementCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
