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

    /// <summary>
    /// اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹) — گروه تفصیلی‌ای که <c>TB_TR_PAYMENT_REQUEST.BENEFICIARY_TAFSILI_ID</c>
    /// باید عضوش باشد (از طریق <c>TB_TAFSIL_LINK_TAFSILGROUP</c>). بدون FK واقعی روی
    /// <c>TB_TAFSIL_GROUP</c> (همان الگوی ریسک #۹/#۱۴) — وجودش سمت Application کنترل می‌شود.
    /// <see langword="null"/> یعنی واحد هنوز گروهی تعریف نکرده — ثبت ذی‌نفع تفصیلی‌دار رد می‌شود.
    /// </summary>
    public Guid? BENEFICIARY_TAFSIL_GROUP_ID { get; set; }

    /// <summary>
    /// بخش ۴-ب (۲۰۲۶-۰۹-۲۹) — حساب معین «بستانکاران» که ردیف بستانکار سند «شناسایی بدهی» و ردیف
    /// بدهکار سند «پرداخت» روی آن نوشته می‌شود. FK به <c>TB_ACCOUNTCODE</c>، بدون FK واقعی در EF
    /// (همان الگوی ریسک #۹/#۱۴) — وجودش سمت Application کنترل می‌شود.
    /// <see langword="null"/> یعنی هنوز تعریف نشده — صدور سند شناسایی بدهی با ۴۰۹ رد می‌شود.
    /// </summary>
    public Guid? PAYABLES_ACCOUNT_ID { get; set; }

    /// <summary>
    /// بخش ۴-ب (۲۰۲۶-۰۹-۲۹) — حساب معین «اعتبار مالیات بر ارزش‌افزوده» — ردیف بدهکار سند شناسایی
    /// بدهی، فقط وقتی <c>VAT_AMOUNT &gt; 0</c>. همان الگوی بدون-FK‌بودن بالا.
    /// </summary>
    public Guid? VAT_CREDIT_ACCOUNT_ID { get; set; }

    /// <summary>
    /// بخش ۴-ب (۲۰۲۶-۰۹-۲۹) — حساب معین «بستانکاران بیمه» — ردیف بستانکار سند شناسایی بدهی، فقط
    /// وقتی <c>INSURANCE_DEDUCTION_AMOUNT &gt; 0</c>. همان الگوی بدون-FK‌بودن بالا.
    /// </summary>
    public Guid? INSURANCE_PAYABLE_ACCOUNT_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }
}
