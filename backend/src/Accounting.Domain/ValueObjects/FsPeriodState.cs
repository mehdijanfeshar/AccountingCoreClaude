namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت دورهٔ (سال مالی) یک واحد برای صورت‌های مالی (ح-۵، سند منبع §۱۱) — <c>TB_FS_PERIOD.STATE</c>.
/// تصمیم صاحب پروژه: فقط برای صورت‌ها (V-11 و انتشار)؛ ثبت سند در ماژول اسناد را نمی‌بندد.
/// </summary>
public enum FsPeriodState
{
    Open = 1,
    SoftClosed = 2,
    Locked = 3,
}

/// <summary>اقدام روی دورهٔ یک واحد — <c>TB_FS_PERIOD_LOG.ACTION</c>.</summary>
public enum FsPeriodAction
{
    Close = 1,
    Lock = 2,
    Reopen = 3,
    RequestReopen = 4,
    ApproveReopen = 5,
    RejectReopen = 6,
}
