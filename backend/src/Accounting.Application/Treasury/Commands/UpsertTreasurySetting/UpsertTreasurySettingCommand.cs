using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Treasury.Queries;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpsertTreasurySetting;

/// <summary>
/// <c>POST api/treasury/settings</c> — creates the unit's <c>TB_TR_SETTING</c> row if none exists
/// yet, or replaces its values otherwise. Only a caller holding
/// <see cref="Accounting.Domain.ValueObjects.TreasuryRole.FinanceManager"/> for the unit may call
/// this — no bootstrap exception here (unlike <c>CreateTreasuryRoleCommand</c>), since by the time
/// a unit needs settings it must already have used the roles bootstrap to create its first
/// FinanceManager (خزانه‌داری، بخش ۴-الف، <c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
/// <param name="BeneficiaryTafsilGroupId">
/// اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹) — <c>TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID</c>. <see langword="null"/>
/// clears the group (removes the ability to set <c>BeneficiaryTafsiliId</c> on new/updated
/// requests). When provided, the handler validates the group exists and is not soft-deleted (404
/// otherwise).
/// </param>
public sealed record UpsertTreasurySettingCommand(
    decimal CeoApprovalThreshold,
    decimal BulkApproveLimit,
    Guid? BeneficiaryTafsilGroupId) : IRequest<TreasurySettingDto>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
