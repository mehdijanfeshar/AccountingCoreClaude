using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_CITY
{
    public string ID { get; set; } = null!;

    public string CITYCODE { get; set; } = null!;

    public string CITYNAME { get; set; } = null!;

    public string PROVINCE_ID { get; set; } = null!;

    public virtual TB_PROVINCE PROVINCE { get; set; } = null!;
}
