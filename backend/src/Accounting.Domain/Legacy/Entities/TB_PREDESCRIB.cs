using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_PREDESCRIB
{
    public string ID { get; set; } = null!;

    public string? ACCOUNTID { get; set; }

    public string? DESCRIP { get; set; }

    public string? ADDUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    /// <summary>
    /// head=0 Detail=1
    /// </summary>
    public bool? FLAGVOUCHER { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNT { get; set; }
}
