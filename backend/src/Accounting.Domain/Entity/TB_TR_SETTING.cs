using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// تنظیمات خزانه‌داری یک واحد — یک ردیف به‌ازای <c>VAHEDCODE</c> (<c>UNIQUE</c>). هیچ مقدار
/// پیش‌فرض در کد یا DDL نیست — تا وقتی مدیر مالی تعریف نکند، این ردیف اصلاً وجود ندارد (صاحب
/// پروژه، ۲۰۲۶-۰۹-۲۸؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
public partial class TB_TR_SETTING
{
    public Guid ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    /// <summary>مبلغی که از آن بیشتر، درخواست پرداخت باید تا مدیرعامل هم بالا برود.</summary>
    public decimal CEO_APPROVAL_THRESHOLD { get; set; }

    /// <summary>سقف مبلغ برای واجد شرایط بودن یک درخواست در تأیید گروهی.</summary>
    public decimal BULK_APPROVE_LIMIT { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }
}
