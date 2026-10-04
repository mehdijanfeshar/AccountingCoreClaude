using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// یک مرحلهٔ گردش تأیید صورت‌های یک مجموعه (ح-۴، سند منبع §۱۱: تهیه‌کننده ← بازبین ← مدیرکل ← معاون).
/// <see cref="APPROVER_USERIDS"/> = کدهای کاربری مجاز این مرحله با ویرگول؛ خالی = هر کاربری جز تهیه‌کننده،
/// ارسال‌کننده و تأییدکنندگان مراحل قبل. مالکیت مثل قواعد کنترل: <see cref="VAHEDCODE"/> خالی = مشترک (فقط ستاد)؛
/// برای یک اجرا زنجیرهٔ نزدیک‌ترین مالکی که مرحله دارد به‌کار می‌رود (مخلوط نمی‌شود). DDL 061.
/// </summary>
public partial class TB_FS_APPROVAL_STEP
{
    public Guid ID { get; set; }

    public string? VAHEDCODE { get; set; }

    public FsFramework FRAMEWORK { get; set; }

    public int STEP_NO { get; set; }

    public string TITLE_FA { get; set; } = null!;

    public string? APPROVER_USERIDS { get; set; }

    public bool IS_ACTIVE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }
}
