using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>تاریخچهٔ کارتابل تأیید چک — DDL 064.</summary>
public partial class TB_CHECK_APPROVAL_EVENT
{
    public Guid ID { get; set; }

    public Guid APPROVAL_ID { get; set; }

    public ChequeApprovalAction ACTION { get; set; }

    public ChequeApprovalState? FROM_STATE { get; set; }

    public ChequeApprovalState TO_STATE { get; set; }

    public string USERID { get; set; } = null!;

    public string? NOTE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public virtual TB_CHECK_APPROVAL APPROVAL { get; set; } = null!;
}
