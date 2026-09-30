using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// قالب صورت مالی — شناسهٔ منطقی یک صورت (مثلاً «خالص دارایی‌ها»). محتوای واقعی (ردیف‌ها) در
/// <see cref="TB_FS_TEMPLATE_VERSION"/> است. فاز ۴۵-الف (<c>docs/fs-module.md</c> §۲ و §۸).
/// </summary>
public partial class TB_FS_TEMPLATE
{
    public Guid ID { get; set; }

    /// <summary>
    /// مالک قالب: <see langword="null"/> = مشترک همهٔ واحدها (فقط ستاد تغییر می‌دهد)؛ پُر = اختصاصی آن
    /// واحد/شرکت و زیرمجموعه‌هایش، و در اجرا بر قالب مشترکِ هم‌کد مقدم (تصمیم صاحب پروژه ۲۰۲۶-۰۹-۳۰).
    /// </summary>
    public string? VAHEDCODE { get; set; }

    public FsFramework FRAMEWORK { get; set; }

    /// <summary>کد یکتا و تغییرناپذیر، مثل <c>PENSION.NET_ASSETS</c>. فرمول بین صورت‌ها با همین کد ارجاع می‌دهد.</summary>
    public string CODE { get; set; } = null!;

    public string TITLE_FA { get; set; } = null!;

    public string? TITLE_EN { get; set; }

    public FsStatementType STATEMENT_TYPE { get; set; }

    /// <summary>ترتیب نمایش صورت در مجموعه (زبانه‌ها)؛ برای یادداشت‌های یک ردیف، ترتیب بین آن‌ها.</summary>
    public int ORDER_NO { get; set; }

    /// <summary>
    /// فقط یادداشت (<see cref="FsStatementType.Note"/>): کد قالب صورتی که یادداشت به ردیفی از آن وصل
    /// است. با کد، نه شناسه — در اجرا روی قالب انتخاب‌شدهٔ همان کد (مشترک یا اختصاصی) حل می‌شود.
    /// </summary>
    public string? NOTE_PARENT_TEMPLATE_CODE { get; set; }

    /// <summary>فقط یادداشت: کد ردیف صورت والد که ستون «یادداشت»ش شمارهٔ این یادداشت را می‌گیرد.</summary>
    public string? NOTE_PARENT_ROW_CODE { get; set; }

    /// <summary>
    /// فقط یادداشت: ردیف جمع یادداشت که باید با ردیف صورت برابر باشد (کنترل V-08)؛ خالی = آخرین ردیف
    /// مقداری یادداشت.
    /// </summary>
    public string? NOTE_TOTAL_ROW_CODE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual ICollection<TB_FS_TEMPLATE_VERSION> TB_FS_TEMPLATE_VERSIONs { get; set; } = new List<TB_FS_TEMPLATE_VERSION>();
}
