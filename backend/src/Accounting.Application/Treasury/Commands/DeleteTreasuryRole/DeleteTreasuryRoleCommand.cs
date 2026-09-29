using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeleteTreasuryRole;

/// <summary>
/// <c>POST api/treasury/roles/{id}/delete</c> — soft-deletes a خزانه‌داری role row. Only a caller
/// holding <see cref="Accounting.Domain.ValueObjects.TreasuryRole.FinanceManager"/> for the unit
/// may call this — no bootstrap exception (unlike <c>CreateTreasuryRoleCommand</c>): a unit that
/// can reach this endpoint already has at least one FinanceManager (the caller).
/// </summary>
public sealed record DeleteTreasuryRoleCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
