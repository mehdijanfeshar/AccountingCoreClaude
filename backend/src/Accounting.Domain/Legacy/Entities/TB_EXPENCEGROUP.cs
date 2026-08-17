using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_EXPENCEGROUP
{
    public string ID { get; set; } = null!;

    public string EXPENCEGROUPCODE { get; set; } = null!;

    public string EXPENCEGROUPNAME { get; set; } = null!;

    public string? DESCRIPTION { get; set; }

    public string? VAHEDCODE { get; set; }

    public virtual ICollection<TB_EXPENCE> TB_EXPENCEs { get; set; } = new List<TB_EXPENCE>();
}
