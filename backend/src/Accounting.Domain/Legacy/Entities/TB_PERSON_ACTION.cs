using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_PERSON_ACTION
{
    public string ID { get; set; } = null!;

    public string? USERNAME { get; set; }

    public string USERID { get; set; } = null!;

    public string? FROMDATE { get; set; }

    public string? TODATE { get; set; }

    public bool? STATUS { get; set; }

    public bool OPERATORROLE { get; set; }

    public string? VAHEDCODE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public DateTime CREATEDDATE { get; set; }
}
