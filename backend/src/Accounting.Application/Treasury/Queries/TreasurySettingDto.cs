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
/// <param name="ReceivablesAccountId">
/// بخش ۴-ج (۲۰۲۶-۰۹-۲۹) — <c>TB_TR_SETTING.RECEIVABLES_ACCOUNT_ID</c> («حساب‌های دریافتنی»).
/// <see langword="null"/> یعنی هنوز تعریف نشده — <c>register</c> دریافت با ۴۰۹ رد می‌شود.
/// </param>
/// <param name="ReceivablesAccountCode">نمایشی — <c>TB_ACCOUNTCODE.ACCCODE</c>، فقط وقتی <paramref name="ReceivablesAccountId"/> مقدار دارد.</param>
/// <param name="ReceivablesAccountName">نمایشی — <c>TB_ACCOUNTCODE.ACCCODENAME</c>، فقط وقتی <paramref name="ReceivablesAccountId"/> مقدار دارد.</param>
/// <param name="CustomerTafsilGroupId">
/// بخش ۴-ج — <c>TB_TR_SETTING.CUSTOMER_TAFSIL_GROUP_ID</c> — گروه تفصیلی مشتریان (پرداخت‌کنندگان
/// دریافت وجه)، جدا از <paramref name="BeneficiaryTafsilGroupId"/>. <see langword="null"/> یعنی
/// هنوز تعریف نشده.
/// </param>
/// <param name="CustomerTafsilGroupCode">نمایشی، فقط وقتی <paramref name="CustomerTafsilGroupId"/> مقدار دارد.</param>
/// <param name="CustomerTafsilGroupName">نمایشی، فقط وقتی <paramref name="CustomerTafsilGroupId"/> مقدار دارد.</param>
/// <param name="DailyTransferLimit">
/// بخش ۴-ج — <c>TB_TR_SETTING.DAILY_TRANSFER_LIMIT</c> — سقف مجموع انتقال‌های اجراشده از یک حساب
/// مبدأ در یک روز. <see langword="null"/> یعنی هنوز تعریف نشده — <c>approve</c> انتقال با ۴۰۹ رد
/// می‌شود.
/// </param>
/// <param name="BankFeeAccountId">
/// بخش ۴-د (۲۰۲۶-۰۹-۲۹) — <c>TB_TR_SETTING.BANK_FEE_ACCOUNT_ID</c> («کارمزد بانکی»).
/// <see langword="null"/> یعنی هنوز تعریف نشده — حل ردیف نامنطبق با نوع «سند کارمزد بانکی» با ۴۰۹
/// رد می‌شود.
/// </param>
/// <param name="BankFeeAccountCode">نمایشی، فقط وقتی <paramref name="BankFeeAccountId"/> مقدار دارد.</param>
/// <param name="BankFeeAccountName">نمایشی، فقط وقتی <paramref name="BankFeeAccountId"/> مقدار دارد.</param>
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
    string? InsurancePayableAccountName,
    Guid? ReceivablesAccountId,
    string? ReceivablesAccountCode,
    string? ReceivablesAccountName,
    Guid? CustomerTafsilGroupId,
    string? CustomerTafsilGroupCode,
    string? CustomerTafsilGroupName,
    decimal? DailyTransferLimit,
    Guid? BankFeeAccountId,
    string? BankFeeAccountCode,
    string? BankFeeAccountName);
