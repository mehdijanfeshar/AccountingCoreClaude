using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_RABET_TYPE
{
    public Guid ID { get; set; }

    public string? RABETCODE { get; set; }

    public string? TITLE { get; set; }

    public virtual ICollection<TB_RABET> TB_RABETs { get; set; } = new List<TB_RABET>();
}
