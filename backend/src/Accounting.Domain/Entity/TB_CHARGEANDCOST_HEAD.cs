using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

public partial class TB_CHARGEANDCOST_HEAD
{
    public Guid ID { get; set; }

    /// <summary>
    /// <c>NUMBER(1)</c>, previously (incorrectly) scaffolded as <c>bool</c> — see
    /// <see cref="ChargeAndCostType"/> XML doc and CLAUDE.md open risk #2. Fixed 2026-09-27 as
    /// the petty-cash module's prerequisite; mapped with an explicit
    /// <c>.HasConversion&lt;int&gt;()</c> in <c>LegacyDbContext</c> per the phase 25 convention
    /// (<c>LegacyEnumMappingConventionTests</c> enforces this automatically).
    /// </summary>
    public ChargeAndCostType CHARGEANDCOST_TYPE { get; set; }

    public string CHARGEANDCOST_CODE { get; set; } = null!;

    public string CHARGEANDCOST_DATE { get; set; } = null!;

    public string? DESCRIPTION { get; set; }

    /// <summary>
    /// <c>NUMBER(1)</c>, previously (incorrectly) scaffolded as <c>bool</c> — see
    /// <see cref="ChargeAndCostStatus"/> XML doc (note it starts at 0, not 1) and CLAUDE.md open
    /// risk #2. Fixed 2026-09-27 as the petty-cash module's prerequisite.
    /// </summary>
    public ChargeAndCostStatus STATUS { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public Guid? ACCOUNT_ID { get; set; }

    public virtual TB_ACCOUNT? ACCOUNT { get; set; }

    public virtual ICollection<TB_CHARGEANDCOST_DETAIL> TB_CHARGEANDCOST_DETAILs { get; set; } = new List<TB_CHARGEANDCOST_DETAIL>();
}
