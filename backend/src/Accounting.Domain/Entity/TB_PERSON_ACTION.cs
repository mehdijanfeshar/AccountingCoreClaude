using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

public partial class TB_PERSON_ACTION
{
    public Guid ID { get; set; }

    public string? USERNAME { get; set; }

    public string USERID { get; set; } = null!;

    public string? FROMDATE { get; set; }

    public string? TODATE { get; set; }

    public bool? STATUS { get; set; }

    public OperatorRole OPERATORROLE { get; set; }

    public string? VAHEDCODE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public DateTime CREATEDDATE { get; set; }
}
