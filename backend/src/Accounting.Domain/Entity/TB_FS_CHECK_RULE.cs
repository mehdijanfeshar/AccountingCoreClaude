using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// قاعدهٔ کنترل تساوی بین صورت‌ها به‌صورت داده (سند منبع §۱۰): <see cref="LEFT_EXPR"/> = <see cref="RIGHT_EXPR"/>
/// با اختلاف مجاز <see cref="TOLERANCE"/>. عبارت‌ها زبان فرمول قالب‌اند و فقط <c>STMT(قالب, ردیف)</c> ارجاع
/// می‌دهند. مالکیت مثل قالب: <see cref="VAHEDCODE"/> خالی = مشترک (فقط ستاد). فاز ۴۵-ه.
/// </summary>
public partial class TB_FS_CHECK_RULE
{
    public Guid ID { get; set; }

    public string? VAHEDCODE { get; set; }

    public FsFramework FRAMEWORK { get; set; }

    /// <summary>کد کنترل، مثل <c>V-04</c>.</summary>
    public string CODE { get; set; } = null!;

    public string TITLE_FA { get; set; } = null!;

    public string LEFT_EXPR { get; set; } = null!;

    public string RIGHT_EXPR { get; set; } = null!;

    /// <summary>اختلاف مجاز (ریال).</summary>
    public decimal TOLERANCE { get; set; }

    public FsCheckSeverity SEVERITY { get; set; }

    public bool IS_ACTIVE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }
}
