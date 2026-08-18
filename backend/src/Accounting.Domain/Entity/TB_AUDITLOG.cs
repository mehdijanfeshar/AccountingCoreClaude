using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_AUDITLOG
{
    public Guid ID { get; set; }

    public string TABLENAME { get; set; } = null!;

    public Guid RECORDID { get; set; }

    public string? COLUMNNAME { get; set; }

    public string? OLDVALUE { get; set; }

    public string? NEWVALUE { get; set; }

    public string ACTIONTYPE { get; set; } = null!;

    public string USERID { get; set; } = null!;

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public DateTime? CHANGEDATE { get; set; }
}
