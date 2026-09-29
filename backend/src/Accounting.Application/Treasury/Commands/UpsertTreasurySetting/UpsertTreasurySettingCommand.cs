using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Treasury.Queries;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpsertTreasurySetting;

/// <summary>
/// <c>POST api/treasury/settings</c> — creates the unit's <c>TB_TR_SETTING</c> row if none exists
/// yet, or replaces its two values otherwise. Only a caller holding
/// <see cref="Accounting.Domain.ValueObjects.TreasuryRole.FinanceManager"/> for the unit may call
/// this — no bootstrap exception here (unlike <c>CreateTreasuryRoleCommand</c>), since by the time
/// a unit needs settings it must already have used the roles bootstrap to create its first
/// FinanceManager (خزانه‌داری، بخش ۴-الف، <c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
public sealed record UpsertTreasurySettingCommand(
    decimal CeoApprovalThreshold,
    decimal BulkApproveLimit) : IRequest<TreasurySettingDto>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
