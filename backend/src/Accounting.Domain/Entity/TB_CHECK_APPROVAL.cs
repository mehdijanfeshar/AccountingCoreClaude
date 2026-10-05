using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>کارتابل تأیید یک چک (دستور پرداخت ⇐ تاییدیه). یک ردیف به‌ازای هر چک — DDL 064.</summary>
public partial class TB_CHECK_APPROVAL
{
    public Guid ID { get; set; }

    public Guid CHECK_ID { get; set; }

    public ChequeApprovalState STATE { get; set; }

    public string PREPARED_BY { get; set; } = null!;

    public DateTime PREPARED_DATE { get; set; }

    public string? ACCOUNTING_BY { get; set; }

    public DateTime? ACCOUNTING_DATE { get; set; }

    public string? MANAGER_BY { get; set; }

    public DateTime? MANAGER_DATE { get; set; }

    public string? NOTE { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string? YEAR { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual ICollection<TB_CHECK_APPROVAL_EVENT> TB_CHECK_APPROVAL_EVENTs { get; set; } = new List<TB_CHECK_APPROVAL_EVENT>();
}
