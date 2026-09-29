namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// <c>GET/POST api/treasury/settings</c> response — خزانه‌داری، بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
public sealed record TreasurySettingDto(
    Guid Id,
    decimal CeoApprovalThreshold,
    decimal BulkApproveLimit);
