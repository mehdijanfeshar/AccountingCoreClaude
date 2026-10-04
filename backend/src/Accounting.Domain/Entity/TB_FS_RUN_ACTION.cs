using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>یک قدم گردش تأیید اجرا (فقط درج). فاز ۴۵-ه.</summary>
public partial class TB_FS_RUN_ACTION
{
    public Guid ID { get; set; }

    public Guid RUN_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public FsRunAction ACTION { get; set; }

    public FsRunState FROM_STATE { get; set; }

    public FsRunState TO_STATE { get; set; }

    public string USERID { get; set; } = null!;

    public string? COMMENTS { get; set; }

    /// <summary>ح-۴ — شمارهٔ مرحلهٔ گردش برای اقدام «تأیید» (DDL 061).</summary>
    public int? STEP_NO { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public virtual TB_FS_RUN RUN { get; set; } = null!;
}
