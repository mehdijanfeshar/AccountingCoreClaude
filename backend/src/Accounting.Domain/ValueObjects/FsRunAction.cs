namespace Accounting.Domain.ValueObjects;

/// <summary>اقدام گردش تأیید اجرا — <c>TB_FS_RUN_ACTION.ACTION</c>.</summary>
public enum FsRunAction
{
    Submit = 1,
    Approve = 2,
    Return = 3,
    Publish = 4,
    Supersede = 5,
}
