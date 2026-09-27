namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Audit-trail action recorded on <c>TB_PC_DOC_EVENT.ACTION</c> for a صورت‌هزینهٔ تنخواه. Chunk 1
/// (this file) only ever writes <see cref="Create"/>, <see cref="Update"/>, <see cref="Submit"/>
/// and <see cref="Delete"/>. Values 5+ are deliberately reserved — not guessed at — for the بخش ۲
/// review actions (<c>docs/tankhah-khazaneh-module.md</c> §۷: بررسی، برگشت، رد، تأیید) so that
/// chunk 2 can add them without renumbering anything chunk 1 already persisted.
/// </summary>
public enum PettyCashDocAction
{
    /// <summary>سند صورت‌هزینه ساخته شد (پیش‌نویس یا مستقیماً ارسال‌شده).</summary>
    Create = 1,

    /// <summary>سند صورت‌هزینه (در وضعیت پیش‌نویس یا برگشتی) ویرایش شد.</summary>
    Update = 2,

    /// <summary>سند از پیش‌نویس/برگشتی به «جدید» ارسال شد.</summary>
    Submit = 3,

    /// <summary>سند (فقط از پیش‌نویس) حذف نرم شد.</summary>
    Delete = 4,

    // 5..: Review / Return / Reject / Approve / Settle — بخش ۲/۳، عمداً هنوز تعریف نشده‌اند.
}
