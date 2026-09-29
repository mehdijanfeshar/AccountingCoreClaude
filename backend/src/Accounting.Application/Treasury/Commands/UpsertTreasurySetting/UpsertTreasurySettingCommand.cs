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
/// <param name="PayablesAccountId">
/// بخش ۴-ب (۲۰۲۶-۰۹-۲۹) — <c>TB_TR_SETTING.PAYABLES_ACCOUNT_ID</c> («حساب بستانکاران»).
/// <see langword="null"/> clears it (صدور سند شناسایی بدهی با ۴۰۹ رد می‌شود تا وقتی دوباره
/// تعریف شود). When provided, the handler validates it exists in <c>TB_ACCOUNTCODE</c> (404
/// otherwise).
/// </param>
/// <param name="VatCreditAccountId">بخش ۴-ب — <c>TB_TR_SETTING.VAT_CREDIT_ACCOUNT_ID</c> («حساب اعتبار مالیات بر ارزش‌افزوده»)، همان قاعدهٔ اعتبارسنجی بالا.</param>
/// <param name="InsurancePayableAccountId">بخش ۴-ب — <c>TB_TR_SETTING.INSURANCE_PAYABLE_ACCOUNT_ID</c> («حساب بستانکاران بیمه»)، همان قاعدهٔ اعتبارسنجی بالا.</param>
public sealed record UpsertTreasurySettingCommand(
    decimal CeoApprovalThreshold,
    decimal BulkApproveLimit,
    Guid? BeneficiaryTafsilGroupId,
    Guid? PayablesAccountId,
    Guid? VatCreditAccountId,
    Guid? InsurancePayableAccountId) : IRequest<TreasurySettingDto>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
