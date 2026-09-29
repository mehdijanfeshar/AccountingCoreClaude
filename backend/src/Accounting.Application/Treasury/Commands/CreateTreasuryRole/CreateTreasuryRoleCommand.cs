using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreateTreasuryRole;

/// <summary>
/// <c>POST api/treasury/roles</c> — creates a new خزانه‌داری role for a user in the caller's unit,
/// or reactivates/renames an existing (possibly soft-deleted) one holding the same
/// (VahedCode, UserId, Role) triple — same create-or-reactivate shape as
/// <c>UpsertPettyCashFundReviewerCommand</c>. Only a caller holding
/// <see cref="TreasuryRole.FinanceManager"/> for the unit may call this, <b>except</b> a bootstrap
/// exception: if the unit has no active <see cref="TreasuryRole.FinanceManager"/> row yet, any
/// authenticated caller in the unit may register a role — the only way a unit gets its first
/// FinanceManager (صاحب پروژه، ۲۰۲۶-۰۹-۲۸؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
public sealed record CreateTreasuryRoleCommand(
    string UserId,
    string? UserName,
    TreasuryRole Role) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
