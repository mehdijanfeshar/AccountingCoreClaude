using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_EXPENCEGROUP
{
    public Guid ID { get; set; }

    public string EXPENCEGROUPCODE { get; set; } = null!;

    public string EXPENCEGROUPNAME { get; set; } = null!;

    public string? DESCRIPTION { get; set; }

    public string? VAHEDCODE { get; set; }

    public virtual ICollection<TB_EXPENCE> TB_EXPENCEs { get; set; } = new List<TB_EXPENCE>();
}
