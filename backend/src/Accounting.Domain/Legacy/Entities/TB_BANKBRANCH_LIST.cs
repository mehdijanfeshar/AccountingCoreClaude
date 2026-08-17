using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_BANKBRANCH_LIST
{
    public string ID { get; set; } = null!;

    public string BRANCHCODE { get; set; } = null!;

    public string BRANCHNAME { get; set; } = null!;

    public string BANK_ID { get; set; } = null!;

    public string? TAFSILI_ID { get; set; }

    public string? CITY_ID { get; set; }

    public virtual TB_BANK_LIST BANK { get; set; } = null!;

    public virtual ICollection<TB_ACCOUNT> TB_ACCOUNTs { get; set; } = new List<TB_ACCOUNT>();
}
