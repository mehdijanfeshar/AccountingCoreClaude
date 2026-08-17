using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_PROVINCE
{
    public string ID { get; set; } = null!;

    public string? PROVINCECODE { get; set; }

    public string? PROVINCENAME { get; set; }

    public bool STATUS { get; set; }

    public virtual ICollection<TB_CITY> TB_CITies { get; set; } = new List<TB_CITY>();
}
