using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_VAHED_INFO
{
    public string ID { get; set; } = null!;

    public string VAHEDCODE { get; set; } = null!;

    public string VAHEDNAME { get; set; } = null!;

    public string CITY_ID { get; set; } = null!;

    public string VAHEDTYPE_ID { get; set; } = null!;

    public string? PARENT_ID { get; set; }

    public virtual ICollection<TB_TAFSILI> TB_TAFSILIs { get; set; } = new List<TB_TAFSILI>();

    public virtual ICollection<TB_WHITELIST> TB_WHITELISTs { get; set; } = new List<TB_WHITELIST>();

    public virtual ICollection<TB_WORKSHOP> TB_WORKSHOPs { get; set; } = new List<TB_WORKSHOP>();

    public virtual TB_VAHED_TYPE VAHEDTYPE { get; set; } = null!;
}
