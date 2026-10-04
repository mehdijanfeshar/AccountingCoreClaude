namespace Accounting.Domain.ValueObjects;

/// <summary>
/// عملیات ماژول صورت‌های مالی برای دسترسی سه‌بُعدی (ط-۲، سند منبع §۱۴: نقش × دامنهٔ واحد × عملیات) —
/// <c>TB_FS_PERMISSION.OPERATIONS</c> به‌صورت بیت‌های جمع‌شده.
/// </summary>
[Flags]
public enum FsOperation
{
    None = 0,
    View = 1,
    Prepare = 2,
    EditTemplate = 4,
    ActivateTemplate = 8,
    Approve = 16,
    Publish = 32,
    ClosePeriod = 64,
    Admin = 128,
}
