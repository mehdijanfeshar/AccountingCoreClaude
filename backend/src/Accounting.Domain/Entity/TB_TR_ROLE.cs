using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// نقش خزانه‌داری یک کاربر در یک واحد — فهرست جدا و مستقل از <c>TB_PC_REVIEWER</c> (صاحب پروژه،
/// ۲۰۲۶-۰۹-۲۸؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰). <c>UNIQUE(VAHEDCODE, USERID, ROLE)</c>
/// — یک کاربر می‌تواند بیش از یک نقش در همان واحد داشته باشد.
/// </summary>
public partial class TB_TR_ROLE
{
    public Guid ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string USERID { get; set; } = null!;

    public string? USER_NAME { get; set; }

    public TreasuryRole ROLE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }
}
