using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>لاگ تغییرناپذیر اقدام‌های دورهٔ صورت‌ها (ح-۵) — فقط درج. DDL 061.</summary>
public partial class TB_FS_PERIOD_LOG
{
    public Guid ID { get; set; }

    public Guid PERIOD_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public FsPeriodAction ACTION { get; set; }

    public FsPeriodState FROM_STATE { get; set; }

    public FsPeriodState TO_STATE { get; set; }

    public string USERID { get; set; } = null!;

    public string? REASON { get; set; }

    public DateTime CREATEDDATE { get; set; }
}
