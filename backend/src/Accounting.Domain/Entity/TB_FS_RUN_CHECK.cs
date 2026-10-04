using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>نتیجهٔ یک کنترل در یک اجرا (فقط درج). فاز ۴۵-ه.</summary>
public partial class TB_FS_RUN_CHECK
{
    public Guid ID { get; set; }

    public Guid RUN_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string CODE { get; set; } = null!;

    public string TITLE_FA { get; set; } = null!;

    public FsCheckSeverity SEVERITY { get; set; }

    public bool PASSED { get; set; }

    public string? MESSAGE { get; set; }

    /// <summary>اختلاف (ریال) برای کنترل‌های تساوی.</summary>
    public decimal? DIFFERENCE { get; set; }

    /// <summary>«کد قالب/کد ردیف» مرتبط، اگر کنترل به یک ردیف برمی‌گردد.</summary>
    public string? ROW_REF { get; set; }

    /// <summary>ح-۳ — کد کاربری مسئول رفع این کنترل ناموفق (DDL 061).</summary>
    public string? ASSIGNEE_USERID { get; set; }

    public string? ASSIGNEE_NAME { get; set; }

    /// <summary>مهلت رفع، تاریخ شمسی YYYYMMDD.</summary>
    public string? DUE_DATE { get; set; }

    public FsCheckAssignState? ASSIGN_STATE { get; set; }

    public string? ASSIGNED_BY { get; set; }

    public virtual TB_FS_RUN RUN { get; set; } = null!;
}
