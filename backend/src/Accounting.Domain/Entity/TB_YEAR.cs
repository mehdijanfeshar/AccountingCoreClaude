using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_YEAR
{
    public byte WORKING_YEAR { get; set; }

    public bool? ISCURRENT { get; set; }

    public decimal? LAST_NUMBER { get; set; }
}
