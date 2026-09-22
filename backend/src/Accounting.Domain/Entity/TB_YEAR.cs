using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_YEAR
{
    /// <summary>
    /// سال مالی — Jalali year, e.g. 1405. Physical column is <c>NUMBER(4,0)</c>.
    ///
    /// ⚠️ <b>Was scaffolded as <c>byte</c>, which is wrong and threw at runtime.</b> A byte tops
    /// out at 255, so <c>OracleDataReader.GetByte</c> raised
    /// <c>InvalidCastException: Specified cast is not valid</c> for every real row (verified on
    /// live Oracle 2026-09-22: rows 1405 and 1404). The bug survived unnoticed from the phase-2
    /// scaffold until phase 37 simply because nothing had ever queried this table. Corrected to
    /// <c>short</c>, which covers the full NUMBER(4) domain.
    /// </summary>
    public short WORKING_YEAR { get; set; }

    public bool? ISCURRENT { get; set; }

    public decimal? LAST_NUMBER { get; set; }
}
