using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_BANK_LIST
{
    public Guid ID { get; set; }

    public string BANKCODE { get; set; } = null!;

    public string BANKNAME { get; set; } = null!;

    public virtual ICollection<TB_ACCOUNT> TB_ACCOUNTs { get; set; } = new List<TB_ACCOUNT>();

    public virtual ICollection<TB_BANKBRANCH_LIST> TB_BANKBRANCH_LISTs { get; set; } = new List<TB_BANKBRANCH_LIST>();
}
