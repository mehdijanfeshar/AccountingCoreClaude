using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// گزارش ماتریسی/تلفیقی — projection of the Oracle view <c>VW_CONSOLIDATE_REPORT</c>.
///
/// <para>
/// <b>The first view-backed type in this project</b>, and the first report that actually follows
/// team working-rule #2 ("the read side reads from a View, not the write model"). The trial
/// balance reports are a recorded exception to that rule precisely because no equivalent view was
/// known to exist; confirmed on live Oracle (2026-09-22) that this one does, along with 27 others.
/// </para>
///
/// <para>
/// One row per voucher detail line, already denormalised by the view: the account's گروه/کل/معین
/// codes and names are flattened onto the line, and its تفصیلی assignments are spread across seven
/// <c>TAFSILICODEn</c>/<c>TAFSILINAMEn</c> column pairs. That shape is what makes a single report
/// able to aggregate by any level without a different join per level.
/// </para>
///
/// <para>
/// Keyless and read-only — mapped with <c>HasNoKey().ToView(...)</c>. Columns the report does not
/// use (<c>ID</c>, <c>MOINID</c>, <c>KOLID</c>, <c>GROUPID</c>, <c>HEAD_DESC</c>, <c>ATFNUMBER</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c>) are deliberately not mapped: an unmapped view column
/// costs nothing, while a mapped one is a promise to keep in sync.
/// </para>
/// </summary>
public partial class VW_CONSOLIDATE_REPORT
{
    public string? GROUPCODE { get; set; }

    public string? GROUPNAME { get; set; }

    public string? KOLCODE { get; set; }

    public string? KOLNAME { get; set; }

    public string? MOINCODE { get; set; }

    public string? MOINNAME { get; set; }

    public decimal? DEBTOR { get; set; }

    public decimal? CREDITOR { get; set; }

    /// <summary>شماره سند — <c>VOUCHERNUMBER</c>.</summary>
    public string? VOUCHERNUMBER { get; set; }

    /// <summary>تاریخ سند, Jalali <c>YYYYMMDD</c> — <c>VOUCHERDATE</c>.</summary>
    public string? VOUCHERDATE { get; set; }

    /// <summary>
    /// وضعیت سند as a raw number.
    ///
    /// ⚠️ Deliberately <see cref="int"/>, not the <c>DocLife</c> enum. This is a view whose column
    /// is a plain <c>NUMBER</c>, and the project has been bitten twice by Oracle's CLR-type
    /// conventions on numeric columns (phase 25's <c>NUMBER(1)</c>⇒<c>bool</c>, phase 37's
    /// <c>GetByte</c> overflow). A report that only ever filters and displays this value has
    /// nothing to gain from the enum and everything to lose from another conversion surprise.
    /// </summary>
    public int? DOCLIFE { get; set; }

    /// <summary>نوع سند — <c>TB_SYSTYPE.ID</c>.</summary>
    public Guid? SYSID { get; set; }

    public string? YEAR { get; set; }

    public string? VAHEDCODE { get; set; }

    /// <summary>
    /// حذف منطقی as a raw number (0/1).
    ///
    /// ⚠️ Deliberately <see cref="int"/>, not <c>bool?</c> — for the same reason as
    /// <see cref="DOCLIFE"/> above, and this one was learned the hard way twice. A <c>bool?</c>
    /// here makes EF render the predicate as <c>ISDELETED &lt;&gt; True</c>, and Oracle has no
    /// boolean literal: <c>ORA-00904: "TRUE": invalid identifier</c>. Phase 37 hit the identical
    /// failure with a projected comparison; this is its predicate twin.
    ///
    /// The rule for this database, now covering both halves: <b>never let a boolean reach the
    /// generated SQL</b> — neither as a projected expression nor as a comparison operand. Read the
    /// number and compare against a number.
    /// </summary>
    public int? ISDELETED { get; set; }

    public string? TAFSILICODE1 { get; set; }

    public string? TAFSILINAME1 { get; set; }

    public string? TAFSILICODE2 { get; set; }

    public string? TAFSILINAME2 { get; set; }

    public string? TAFSILICODE3 { get; set; }

    public string? TAFSILINAME3 { get; set; }

    public string? TAFSILICODE4 { get; set; }

    public string? TAFSILINAME4 { get; set; }

    public string? TAFSILICODE5 { get; set; }

    public string? TAFSILINAME5 { get; set; }

    public string? TAFSILICODE6 { get; set; }

    public string? TAFSILINAME6 { get; set; }

    public string? TAFSILICODE7 { get; set; }

    public string? TAFSILINAME7 { get; set; }
}
