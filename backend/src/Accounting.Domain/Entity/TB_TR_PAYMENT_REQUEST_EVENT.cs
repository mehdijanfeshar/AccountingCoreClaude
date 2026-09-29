using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «گردش عملیات» یک درخواست پرداخت — فقط درج، هرگز ویرایش/حذف. همان الگوی
/// <see cref="TB_PC_DOC_EVENT"/> (بخش ۴-الف، <c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// <c>ADDUSERID</c> = عامل واقعی هر گذار — از همین ستون، «تأییدکنندهٔ مرحلهٔ قبل» برای قاعدهٔ
/// «یک کاربر نمی‌تواند دو مرحلهٔ متوالی را تأیید کند» خوانده می‌شود، نه یک ستون جداگانه.
/// </summary>
public partial class TB_TR_PAYMENT_REQUEST_EVENT
{
    public Guid ID { get; set; }

    public Guid PAYMENT_REQUEST_ID { get; set; }

    public PaymentRequestEventAction ACTION { get; set; }

    public PaymentRequestState? FROM_STATE { get; set; }

    public PaymentRequestState? TO_STATE { get; set; }

    public string? NOTE { get; set; }

    public string? CLIENT_IP { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_TR_PAYMENT_REQUEST? PAYMENT_REQUEST { get; set; }
}
