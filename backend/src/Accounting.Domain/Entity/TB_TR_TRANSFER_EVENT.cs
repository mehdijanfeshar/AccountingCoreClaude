using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «گردش عملیات» یک انتقال وجه — فقط درج، هرگز ویرایش/حذف. همان الگوی
/// <see cref="TB_TR_PAYMENT_REQUEST_EVENT"/> (بخش ۴-ج، <c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
public partial class TB_TR_TRANSFER_EVENT
{
    public Guid ID { get; set; }

    public Guid TRANSFER_ID { get; set; }

    public TransferEventAction ACTION { get; set; }

    public TransferState? FROM_STATE { get; set; }

    public TransferState? TO_STATE { get; set; }

    public string? NOTE { get; set; }

    public string? CLIENT_IP { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_TR_TRANSFER? TRANSFER { get; set; }
}
