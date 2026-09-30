using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// ردیف یک نسخهٔ قالب صورت مالی (سند منبع §۶ و §۷). فرزند تعبیه‌شدهٔ
/// <see cref="TB_FS_TEMPLATE_VERSION"/> است: فقط زیر نسخهٔ پیش‌نویس ساخته/ویرایش/حذف می‌شود و
/// حذفش <b>سخت</b> است (ردیف نسخهٔ پیش‌نویس نسخهٔ کاری است، نه سابقه). فاز ۴۵-الف.
/// </summary>
public partial class TB_FS_TEMPLATE_ROW
{
    public Guid ID { get; set; }

    public Guid VERSION_ID { get; set; }

    /// <summary>کد ردیف، یکتا در نسخه (مثلاً <c>A01</c>) — فرمول‌ها با آن ارجاع می‌دهند.</summary>
    public string CODE { get; set; } = null!;

    /// <summary>ردیف عنوان والد (برای بازوبسته‌کردن گروه)، در همین نسخه.</summary>
    public Guid? PARENT_ID { get; set; }

    /// <summary>ترتیب ارائه در کل نسخه (نه فقط بین هم‌والدها) — <c>SUM(A01:A08)</c> بر همین ترتیب است.</summary>
    public int ORDER_NO { get; set; }

    public FsRowType ROW_TYPE { get; set; }

    public string? TITLE_FA { get; set; }

    public string? TITLE_EN { get; set; }

    /// <summary>شمارهٔ یادداشت، شاید مرکب مثل <c>۳۳-۲-۴</c>.</summary>
    public string? NOTE_REF { get; set; }

    public FsNormalBalance? NORMAL_BALANCE { get; set; }

    /// <summary>انتخاب‌گر حساب (فقط ردیف <see cref="FsRowType.Account"/>) — مثل <c>1301..1309 !1305</c>.</summary>
    public string? SELECTOR { get; set; }

    public FsValueType? VALUE_TYPE { get; set; }

    /// <summary>فرمول (فقط ردیف <see cref="FsRowType.Formula"/>) — مثل <c>SUM(A01:A08)</c>.</summary>
    public string? FORMULA { get; set; }

    /// <summary>قالب‌بندی نمایش به‌صورت JSON (<c>FsRowFormat</c> در Application).</summary>
    public string? FORMAT_JSON { get; set; }

    public bool IS_DRILLABLE { get; set; }

    public bool ALLOW_MANUAL_ADJUST { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public virtual TB_FS_TEMPLATE_VERSION VERSION { get; set; } = null!;
}
