namespace Accounting.Domain.ValueObjects;

/// <summary>شدت کنترل صورت مالی (سند منبع §۱۰). <see cref="Blocking"/> ناموفق مانع ارسال برای تأیید است.</summary>
public enum FsCheckSeverity
{
    Info = 1,
    Warning = 2,
    Blocking = 3,
}
