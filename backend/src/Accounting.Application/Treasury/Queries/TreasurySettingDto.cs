namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// <c>GET/POST api/treasury/settings</c> response — خزانه‌داری، بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
/// <param name="BeneficiaryTafsilGroupId">
/// اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹) — <c>TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID</c>. <see langword="null"/>
/// یعنی هنوز تعریف نشده — ثبت درخواست پرداخت با تفصیلی ذی‌نفع رد می‌شود.
/// </param>
/// <param name="BeneficiaryTafsilGroupCode">نمایشی — <c>TB_TAFSIL_GROUP.TAFSILGROUP_CODE</c>، فقط وقتی <paramref name="BeneficiaryTafsilGroupId"/> مقدار دارد.</param>
/// <param name="BeneficiaryTafsilGroupName">نمایشی — <c>TB_TAFSIL_GROUP.TAFSILGROUP_NAME</c>، فقط وقتی <paramref name="BeneficiaryTafsilGroupId"/> مقدار دارد.</param>
/// <param name="PayablesAccountId">
/// بخش ۴-ب (۲۰۲۶-۰۹-۲۹) — <c>TB_TR_SETTING.PAYABLES_ACCOUNT_ID</c> («حساب بستانکاران»).
/// <see langword="null"/> یعنی هنوز تعریف نشده — صدور سند شناسایی بدهی با ۴۰۹ رد می‌شود.
/// </param>
/// <param name="PayablesAccountCode">نمایشی — <c>TB_ACCOUNTCODE.ACCCODE</c>، فقط وقتی <paramref name="PayablesAccountId"/> مقدار دارد.</param>
/// <param name="PayablesAccountName">نمایشی — <c>TB_ACCOUNTCODE.ACCCODENAME</c>، فقط وقتی <paramref name="PayablesAccountId"/> مقدار دارد.</param>
/// <param name="VatCreditAccountId">بخش ۴-ب — <c>TB_TR_SETTING.VAT_CREDIT_ACCOUNT_ID</c> («حساب اعتبار مالیات بر ارزش‌افزوده»).</param>
/// <param name="VatCreditAccountCode">نمایشی، فقط وقتی <paramref name="VatCreditAccountId"/> مقدار دارد.</param>
/// <param name="VatCreditAccountName">نمایشی، فقط وقتی <paramref name="VatCreditAccountId"/> مقدار دارد.</param>
/// <param name="InsurancePayableAccountId">بخش ۴-ب — <c>TB_TR_SETTING.INSURANCE_PAYABLE_ACCOUNT_ID</c> («حساب بستانکاران بیمه»).</param>
/// <param name="InsurancePayableAccountCode">نمایشی، فقط وقتی <paramref name="InsurancePayableAccountId"/> مقدار دارد.</param>
/// <param name="InsurancePayableAccountName">نمایشی، فقط وقتی <paramref name="InsurancePayableAccountId"/> مقدار دارد.</param>
public sealed record TreasurySettingDto(
    Guid Id,
    decimal CeoApprovalThreshold,
    decimal BulkApproveLimit,
    Guid? BeneficiaryTafsilGroupId,
    string? BeneficiaryTafsilGroupCode,
    string? BeneficiaryTafsilGroupName,
    Guid? PayablesAccountId,
    string? PayablesAccountCode,
    string? PayablesAccountName,
    Guid? VatCreditAccountId,
    string? VatCreditAccountCode,
    string? VatCreditAccountName,
    Guid? InsurancePayableAccountId,
    string? InsurancePayableAccountCode,
    string? InsurancePayableAccountName);
