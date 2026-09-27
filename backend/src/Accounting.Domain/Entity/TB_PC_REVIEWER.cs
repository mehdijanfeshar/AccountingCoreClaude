using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// «بررسی‌کنندهٔ تنخواه» — RBAC جانبی جدید، نه <see cref="TB_PERSON_ACTION"/>: آن جدول
/// <c>USERID</c>اش کد ملی است (فضای هویتی جدا از <see cref="Accounting.Application.Common.Interfaces.ICurrentUser.UserId"/>/
/// <c>ADDUSERID</c>) و <c>OPERATORROLE</c>اش یک نقش امضای مالی سازمانی بدون هیچ consumer فعلی است —
/// تحمیل معنای «بررسی‌کنندهٔ این تنخواهِ خاص» رویش حدسی و مستندنشده بود
/// (<c>docs/tankhah-khazaneh-module.md</c>، تصمیم‌های بخش ۲).
///
/// یک ردیف فعال اینجا برای (<see cref="REVOLVINGFUND_ID"/>, کاربر جاری) پیش‌نیاز هر اکشن بررسی/
/// تأیید/برگشت/رد روی یک <see cref="TB_PC_EXPENSE_DOC"/> است — رجوع به
/// <c>Accounting.Application.PettyCash.Commands.Common.IPettyCashReviewAuthorizer</c>.
/// جدول جدید «جانبی» (پیشوند <c>TB_PC_</c>)، همان استثنای صریح صاحب پروژه بر قانون «هیچ جدول
/// جدیدی» که بخش ۱ استفاده کرد. <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛ همیشه
/// application-side تولید می‌شود. <c>UK_PC_REVIEWER</c> روی (<see cref="REVOLVINGFUND_ID"/>,
/// <see cref="REVIEWER_USERID"/>) یکتاست.
/// </summary>
public partial class TB_PC_REVIEWER
{
    public Guid ID { get; set; }

    public Guid REVOLVINGFUND_ID { get; set; }

    /// <summary>فضای هویتی <c>ICurrentUser.UserId</c>/<c>ADDUSERID</c> — نه کد ملی.</summary>
    public string REVIEWER_USERID { get; set; } = null!;

    public string? REVIEWER_NAME { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_REVOLVING_FUND? REVOLVINGFUND { get; set; }
}
