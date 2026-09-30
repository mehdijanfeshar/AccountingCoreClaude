using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>یک صورت داخل یک اجرا، با نسخهٔ قالبی که با آن محاسبه شد. فاز ۴۵-ب.</summary>
public partial class TB_FS_RUN_STATEMENT
{
    public Guid ID { get; set; }

    public Guid RUN_ID { get; set; }

    /// <summary>واحد صاحب اجرا — تکرار <c>TB_FS_RUN.VAHEDCODE</c> برای فیلتر بدون جوین.</summary>
    public string VAHEDCODE { get; set; } = null!;

    public Guid TEMPLATE_ID { get; set; }

    public Guid VERSION_ID { get; set; }

    public string TEMPLATE_CODE { get; set; } = null!;

    public string TITLE_FA { get; set; } = null!;

    public FsStatementType STATEMENT_TYPE { get; set; }

    public int ORDER_NO { get; set; }

    public int VERSION_NO { get; set; }

    /// <summary>وضعیت نسخه در لحظهٔ اجرا — <see cref="FsTemplateVersionState.Draft"/> یعنی پیش‌نمایش.</summary>
    public FsTemplateVersionState VERSION_STATE { get; set; }

    /// <summary>این «صورت» یک یادداشت است (بخش ۴۵-ج).</summary>
    public bool IS_NOTE { get; set; }

    /// <summary>شمارهٔ یادداشت که در این اجرا داده شد (مثلاً «۵»).</summary>
    public string? NOTE_NO { get; set; }

    /// <summary>صورت و ردیفی که یادداشت به آن وصل شد؛ خالی = یادداشت بی‌والد (در انتها شماره خورد).</summary>
    public string? PARENT_TEMPLATE_CODE { get; set; }

    public string? PARENT_ROW_CODE { get; set; }

    /// <summary>ردیف جمع یادداشت که با ردیف صورت مقایسه شد.</summary>
    public string? TOTAL_ROW_CODE { get; set; }

    /// <summary>کنترل V-08: جمع یادداشت − ردیف صورت (دورهٔ جاری)؛ صفر = برابر؛ <see langword="null"/> = بی‌والد.</summary>
    public decimal? CHECK_DIFF_CUR { get; set; }

    /// <summary>همان برای ستون سال قبل.</summary>
    public decimal? CHECK_DIFF_PRV { get; set; }

    public virtual TB_FS_RUN RUN { get; set; } = null!;

    public virtual ICollection<TB_FS_RUN_ROW> TB_FS_RUN_ROWs { get; set; } = new List<TB_FS_RUN_ROW>();
}
