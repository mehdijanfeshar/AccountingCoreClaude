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
public sealed record TreasurySettingDto(
    Guid Id,
    decimal CeoApprovalThreshold,
    decimal BulkApproveLimit,
    Guid? BeneficiaryTafsilGroupId,
    string? BeneficiaryTafsilGroupCode,
    string? BeneficiaryTafsilGroupName);
