using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «گردش عملیات» (Audit Trail) یک صورت‌هزینهٔ تنخواه — فقط درج، هرگز ویرایش/حذف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۳). هیچ معادل Legacy ای ندارد؛ کاملاً جدید.
///
/// هر تغییر روی <see cref="TB_PC_EXPENSE_DOC"/> (ساخت/ویرایش/ارسال/حذف در بخش ۱؛ بررسی/برگشت/رد/
/// تأیید در بخش ۲) دقیقاً یک ردیف اینجا در همان تراکنش می‌گیرد — رجوع به قاعدهٔ §۴.
/// <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱).
/// </summary>
public partial class TB_PC_DOC_EVENT
{
    public Guid ID { get; set; }

    public Guid EXPENSE_DOC_ID { get; set; }

    public PettyCashDocAction ACTION { get; set; }

    public PettyCashDocState? FROM_STATE { get; set; }

    public PettyCashDocState? TO_STATE { get; set; }

    public string? NOTE { get; set; }

    /// <summary>کدهای دلیل برگشت با <c>,</c> جدا شده‌اند — بخش ۲ (برگشت چندعلتی). بخش ۱ هیچ‌وقت
    /// این ستون را پر نمی‌کند.</summary>
    public string? RETURN_REASONS { get; set; }

    public string? CLIENT_IP { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_PC_EXPENSE_DOC? EXPENSE_DOC { get; set; }
}
