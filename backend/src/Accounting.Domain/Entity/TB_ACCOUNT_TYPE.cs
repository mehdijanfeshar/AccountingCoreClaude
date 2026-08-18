using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_ACCOUNT_TYPE
{
    public Guid ID { get; set; }

    public string TITLE { get; set; } = null!;
}
