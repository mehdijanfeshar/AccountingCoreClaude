using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// یک اجرای «تهیهٔ صورت‌های مالی» — Snapshot تغییرناپذیر مبالغ صورت‌های یک مجموعه برای یک واحد
/// (و اختیاری زیرمجموعه‌هایش) و یک دوره (از ابتدای <see cref="YEAR"/> تا پایان ماه
/// <see cref="TO_MONTH"/>). فاز ۴۵-ب (<c>docs/fs-module.md</c> §۷).
/// </summary>
public partial class TB_FS_RUN
{
    public Guid ID { get; set; }

    /// <summary>شمارهٔ اجرا، یکتا در کل سیستم.</summary>
    public int RUN_NO { get; set; }

    /// <summary>واحد دامنه (از هدر <c>X-Vahed-Code</c>) — اجرا فقط برای همین واحد قابل دیدن است.</summary>
    public string VAHEDCODE { get; set; } = null!;

    public string? VAHEDNAME { get; set; }

    /// <summary>آیا اسناد زیرمجموعه‌های واحد هم جمع شده‌اند (صورت ترکیبی).</summary>
    public bool INCLUDE_SUBUNITS { get; set; }

    /// <summary>تعداد واحدهایی که اسنادشان در این اجرا جمع شد.</summary>
    public int UNIT_COUNT { get; set; }

    public FsFramework FRAMEWORK { get; set; }

    public string YEAR { get; set; } = null!;

    /// <summary>ماه پایان دوره (۱ تا ۱۲)؛ دوره همیشه از ابتدای سال است.</summary>
    public int TO_MONTH { get; set; }

    /// <summary>ابتدای دوره، جلالی <c>YYYYMMDD</c>.</summary>
    public string FROM_DATE { get; set; } = null!;

    /// <summary>پایان دوره (شامل)، جلالی <c>YYYYMMDD</c>.</summary>
    public string TO_DATE { get; set; } = null!;

    /// <summary>کمینهٔ <c>DOCLIFE</c> اسنادی که جمع شدند.</summary>
    public int MIN_DOCLIFE { get; set; }

    /// <summary>ستون سال قبل (همان ماه) محاسبه شده است.</summary>
    public bool HAS_PRIOR { get; set; }

    /// <summary>برچسب «تجدید ارائه‌شده» روی ستون سال قبل (فقط برچسب؛ تعدیلات سنواتی محاسبه نمی‌شود). DDL 061.</summary>
    public bool PRIOR_RESTATED { get; set; }

    /// <summary>ط-۵ — تلفیق با شرکت‌های تابعهٔ واحد اجرا (تراز واردشده از Excel). DDL 062.</summary>
    public bool INCLUDE_ENTITIES { get; set; }

    /// <summary>شمارهٔ اولین یادداشت عددی (یادداشت‌های پیش از آن معمولاً متنی‌اند: تاریخچه، مبنا، رویه‌ها).</summary>
    public int NOTE_START_NO { get; set; }

    /// <summary>حداقل یک صورت با نسخهٔ پیش‌نویس محاسبه شد — اجرای «آزمایشی».</summary>
    public bool USES_DRAFT { get; set; }

    public FsRunState STATE { get; set; }

    public string? DESCRIPTION { get; set; }

    /// <summary>SHA-256 مبالغ همهٔ ردیف‌ها (hex).</summary>
    public string? CONTENT_HASH { get; set; }

    public int? DURATION_MS { get; set; }

    /// <summary>بخش ۴۵-ه — SHA-256 مانده‌های منبع در لحظهٔ اجرا؛ تفاوت با محاسبهٔ امروز = «کهنه».</summary>
    public string? BALANCE_HASH { get; set; }

    /// <summary>بخش ۴۵-ه — اجرایی که این یکی از رویش ساخته شد (مثلاً با ورود مقادیر دستی).</summary>
    public Guid? SOURCE_RUN_ID { get; set; }

    /// <summary>ح-۴ — تعداد مراحل تأییدشده در دور جاری بازبینی (۰ پس از ارسال یا برگشت). DDL 061.</summary>
    public int APPROVAL_STEP { get; set; }

    public virtual ICollection<TB_FS_RUN_CHECK> TB_FS_RUN_CHECKs { get; set; } = new List<TB_FS_RUN_CHECK>();

    public virtual ICollection<TB_FS_RUN_ACTION> TB_FS_RUN_ACTIONs { get; set; } = new List<TB_FS_RUN_ACTION>();

    public virtual ICollection<TB_FS_RUN_MANUAL> TB_FS_RUN_MANUALs { get; set; } = new List<TB_FS_RUN_MANUAL>();

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual ICollection<TB_FS_RUN_STATEMENT> TB_FS_RUN_STATEMENTs { get; set; } = new List<TB_FS_RUN_STATEMENT>();
}
