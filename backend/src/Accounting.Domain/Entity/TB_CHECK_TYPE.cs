using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_CHECK_TYPE
{
    public Guid ID { get; set; }

    public string? CHEQUE_TYPE_TITLE { get; set; }

    public byte? CHEQUE_WIDTH { get; set; }

    public byte? CHEQUE_HEIGHT { get; set; }

    public byte[]? CHEQUE_IMAGE { get; set; }

    public string? CHEQUE_ADATE_FONT { get; set; }

    public byte? CHEQUE_ADATE_LEFT { get; set; }

    public byte? CHEQUE_ADATE_TOP { get; set; }

    public byte? CHEQUE_ADATE_WIDTH { get; set; }

    public string? CHEQUE_NDATE_FONT { get; set; }

    public byte? CHEQUE_NDATE_LEFT { get; set; }

    public byte? CHEQUE_NDATE_TOP { get; set; }

    public byte? CHEQUE_NDATE_WIDTH { get; set; }

    public string? CHEQUE_AAMOUNT_FONT { get; set; }

    public byte? CHEQUE_AAMOUNT_LEFT { get; set; }

    public byte? CHEQUE_AAMOUNT_TOP { get; set; }

    public byte? CHEQUE_AAMOUNT_WIDTH { get; set; }

    public string? CHEQUE_LAMOUNT_FONT { get; set; }

    public byte? CHEQUE_LAMOUNT_LEFT { get; set; }

    public byte? CHEQUE_LAMOUNT_TOP { get; set; }

    public byte? CHEQUE_LAMOUNT_WIDTH { get; set; }

    public string? CHEQUE_NAMOUNT_FONT { get; set; }

    public byte? CHEQUE_NAMOUNT_LEFT { get; set; }

    public byte? CHEQUE_NAMOUNT_TOP { get; set; }

    public byte? CHEQUE_NAMOUNT_WIDTH { get; set; }

    public string? CHEQUE_DESCRIBE1_FONT { get; set; }

    public byte? CHEQUE_DESCRIBE1_LEFT { get; set; }

    public byte? CHEQUE_DESCRIBE1_TOP { get; set; }

    public byte? CHEQUE_DESCRIBE1_WIDTH { get; set; }

    public string? CHEQUE_DESCRIBE2_FONT { get; set; }

    public byte? CHEQUE_DESCRIBE2_LEFT { get; set; }

    public byte? CHEQUE_DESCRIBE2_TOP { get; set; }

    public byte? CHEQUE_DESCRIBE2_WIDTH { get; set; }

    public string? CHEQUE_BREAKLINE_FONT { get; set; }

    public byte? CHEQUE_BREAKLINE_LEFT { get; set; }

    public byte? CHEQUE_BREAKLINE_TOP { get; set; }

    public byte? CHEQUE_BREAKLINE_WIDTH { get; set; }

    public byte? PRINTER_MARGINE_TOP { get; set; }

    public byte? PRINTER_MARGINE_LEFT { get; set; }

    public string? PRINTER_TYPE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string YEAR { get; set; } = null!;

    public string VAHEDCODE { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public virtual ICollection<TB_CHECKBOOK> TB_CHECKBOOKs { get; set; } = new List<TB_CHECKBOOK>();
}
