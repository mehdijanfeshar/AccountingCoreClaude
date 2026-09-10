using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_TAFSIL_LINK_TAFSILGROUP
{
    public Guid ID { get; set; }

    public Guid TAFSIL_ID { get; set; }

    public Guid TAFSILGROUP_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public bool ISDELETED { get; set; }

    /// <summary>
    /// Visibility scope for this تفصیلی-گروه link, consumed by Rule B in
    /// <c>GetTafsiliLevelItemsQuery</c>/<c>TafsiliLookupReadRepository</c>. Declared <c>short?</c>
    /// (Oracle <c>NUMBER(1)</c>, mapping unchanged), NOT <c>bool</c> — changed 2026-09-10 (phase
    /// 20-b) from an incorrect <c>bool?</c>. Live Oracle data holds <c>{1, 3, NULL}</c>
    /// (<c>docs/centralaccount-business-reference.md</c> §23-2), which a <see cref="bool"/>?
    /// cannot represent; the values correspond to <see cref="VahedCategory"/> (<c>1</c> =
    /// <see cref="VahedCategory.Insurance"/>, <c>3</c> = <see cref="VahedCategory.All"/>) with
    /// <see langword="null"/> meaning "no wildcard category — only an exact <c>VAHEDCODE</c>
    /// match makes this row visible". This is a deliberate, narrow partial payment on CLAUDE.md
    /// open risk #2 (the wider <c>bool?</c>-on-multi-valued-<c>NUMBER(1)</c> bug family) —
    /// confined to this one column; no other <c>bool?</c> column was touched.
    /// </summary>
    public short? VAHEDTYPE { get; set; }

    public virtual TB_TAFSIL_GROUP TAFSILGROUP { get; set; } = null!;
}
