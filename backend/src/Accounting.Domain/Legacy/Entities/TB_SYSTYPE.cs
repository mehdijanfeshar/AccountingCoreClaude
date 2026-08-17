using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_SYSTYPE
{
    public string ID { get; set; } = null!;

    public string SYS_COD { get; set; } = null!;

    public string? SYS_NAME { get; set; }

    public virtual ICollection<TB_VOUCHERSHEAD> TB_VOUCHERSHEADs { get; set; } = new List<TB_VOUCHERSHEAD>();
}
