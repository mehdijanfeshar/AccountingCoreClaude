using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_BANKBRANCH_LIST
{
    public Guid ID { get; set; }

    public string BRANCHCODE { get; set; } = null!;

    public string BRANCHNAME { get; set; } = null!;

    public Guid BANK_ID { get; set; }

    public Guid? TAFSILI_ID { get; set; }

    public Guid? CITY_ID { get; set; }

    public virtual TB_BANK_LIST BANK { get; set; } = null!;

    public virtual ICollection<TB_ACCOUNT> TB_ACCOUNTs { get; set; } = new List<TB_ACCOUNT>();
}
