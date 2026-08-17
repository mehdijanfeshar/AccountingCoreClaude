using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_AUDITLOG
{
    public string ID { get; set; } = null!;

    public string TABLENAME { get; set; } = null!;

    public string RECORDID { get; set; } = null!;

    public string? COLUMNNAME { get; set; }

    public string? OLDVALUE { get; set; }

    public string? NEWVALUE { get; set; }

    public string ACTIONTYPE { get; set; } = null!;

    public string USERID { get; set; } = null!;

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public DateTime? CHANGEDATE { get; set; }
}
