using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_EXPENCE_LINK_TAFSILI
{
    public Guid ID { get; set; }

    public Guid EXPENSE_ID { get; set; }

    public Guid TAFSILI_ID { get; set; }

    public Guid LEVEL_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public string? VAHEDCODE { get; set; }

    public virtual TB_EXPENCE EXPENSE { get; set; } = null!;

    public virtual TB_LEVEL_TAFSIL LEVEL { get; set; } = null!;

    public virtual TB_TAFSILI TAFSILI { get; set; } = null!;
}
