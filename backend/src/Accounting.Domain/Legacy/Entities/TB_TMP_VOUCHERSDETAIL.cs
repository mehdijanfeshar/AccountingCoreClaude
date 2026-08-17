using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_TMP_VOUCHERSDETAIL
{
    public string ID { get; set; } = null!;

    public string? TMPVOUCHERHEAD_ID { get; set; }

    public string? MOINCODE { get; set; }

    public string? TAFSILI_CODE1 { get; set; }

    public string? TAFSILI_CODE2 { get; set; }

    public string? TAFSILI_CODE3 { get; set; }

    public string? TAFSILI_CODE4 { get; set; }

    public string? TAFSILI_CODE5 { get; set; }

    public string? TAFSILI_CODE6 { get; set; }

    public string? TAFSILI_CODE7 { get; set; }

    public int? RADIF { get; set; }

    public string? DETAIL_DESC { get; set; }

    public decimal? DEBTOR { get; set; }

    public decimal? CREDITOR { get; set; }

    public string? CHECK_DATE { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool? ISDELETED { get; set; }

    public string? VALUE { get; set; }

    public virtual TB_TMP_VOUCHERHEAD? TMPVOUCHERHEAD { get; set; }
}
