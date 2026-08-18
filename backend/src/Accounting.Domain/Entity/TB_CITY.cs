using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_CITY
{
    public Guid ID { get; set; }

    public string CITYCODE { get; set; } = null!;

    public string CITYNAME { get; set; } = null!;

    public Guid PROVINCE_ID { get; set; }

    public virtual TB_PROVINCE PROVINCE { get; set; } = null!;
}
