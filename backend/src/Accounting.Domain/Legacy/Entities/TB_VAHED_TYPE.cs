using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_VAHED_TYPE
{
    public string ID { get; set; } = null!;

    public string? TYPECODE { get; set; }

    public string? TYPENAME { get; set; }

    public string? PARENTTYPECODE { get; set; }

    public virtual ICollection<TB_ACCOUNTEXCEPTION> TB_ACCOUNTEXCEPTIONs { get; set; } = new List<TB_ACCOUNTEXCEPTION>();

    public virtual ICollection<TB_RABET_CLOSING> TB_RABET_CLOSINGs { get; set; } = new List<TB_RABET_CLOSING>();

    public virtual ICollection<TB_VAHED_INFO> TB_VAHED_INFOs { get; set; } = new List<TB_VAHED_INFO>();

    public virtual ICollection<TB_WHITEANDBLACKLIST> TB_WHITEANDBLACKLISTs { get; set; } = new List<TB_WHITEANDBLACKLIST>();

    public virtual ICollection<TB_WHITELIST> TB_WHITELISTs { get; set; } = new List<TB_WHITELIST>();
}
