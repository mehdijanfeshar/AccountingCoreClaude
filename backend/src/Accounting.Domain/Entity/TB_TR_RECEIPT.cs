using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «دریافت وجه» خزانه‌داری (<c>RCV-xxxxxx</c>) — بخش ۴-ج
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹). جدول جانبی کاملاً جدید، هم‌شکل کلی
/// <see cref="TB_TR_PAYMENT_REQUEST"/> اما بدون زنجیرهٔ تأیید — فقط <c>register</c> (خزانه‌دار) سند
/// GL موقت + Legacy <c>TB_PAYRECIVHEAD/DETAIL</c> (نوع دریافت) صادر می‌کند.
///
/// بدون تخصیص فاکتور فروش (owner decision ۲۰۲۶-۰۹-۲۹ — ماژول فاکتور فروش نداریم) — فقط
/// <see cref="INVOICE_REF"/> آزاد اختیاری. هیچ ستون شناسه‌ای اینجا FK واقعی در EF ندارد (همان
/// الگوی ریسک باز #۹/#۱۴).
/// </summary>
public partial class TB_TR_RECEIPT
{
    public Guid ID { get; set; }

    /// <summary>نمایش کامل («<c>RCV-</c>» + شمارندهٔ ۶رقمی به‌ازای (VAHEDCODE, YEAR)).</summary>
    public string CODE { get; set; } = null!;

    /// <summary>FK به <c>TB_TAFSILI</c> — پرداخت‌کننده، باید عضو گروه تفصیلی مشتریان
    /// (<c>TB_TR_SETTING.CUSTOMER_TAFSIL_GROUP_ID</c>) باشد.</summary>
    public Guid PAYER_TAFSILI_ID { get; set; }

    /// <summary>عکس‌فوری نام تفصیلی پرداخت‌کننده در لحظهٔ ثبت.</summary>
    public string PAYER_NAME { get; set; } = null!;

    public decimal AMOUNT { get; set; }

    /// <summary>FK به <c>TB_ACCOUNT</c> — حساب بانکی مقصد (واحد).</summary>
    public Guid BANK_ACCOUNT_ID { get; set; }

    public TreasuryPaymentMethod RECEIPT_METHOD { get; set; }

    /// <summary>شمسی <c>YYYYMMDD</c>.</summary>
    public string RECEIPT_DATE { get; set; } = null!;

    /// <summary>شمارهٔ پیگیری/مرجع بانکی — الزامی، یکتا در میان دریافت‌های زندهٔ غیرلغوشدهٔ همان
    /// حساب بانکی (کنترل سمت Application، بدون UNIQUE واقعی — همان الگوی این پروژه برای یکتایی
    /// وابسته‌به‌وضعیت).</summary>
    public string BANK_REFERENCE { get; set; } = null!;

    /// <summary>مرجع آزاد فاکتور — بدون تخصیص واقعی (ماژول فاکتور فروش نداریم).</summary>
    public string? INVOICE_REF { get; set; }

    public string? DESCRIPTION { get; set; }

    public ReceiptState STATE { get; set; }

    /// <summary>سند GL موقت «دریافت» — فقط پس از <c>register</c>.</summary>
    public Guid? VOUCHER_ID { get; set; }

    /// <summary>سرسند Legacy <c>TB_PAYRECIVHEAD</c> (نوع دریافت) — فقط پس از <c>register</c>.</summary>
    public Guid? PAYRECIVHEAD_ID { get; set; }

    public string? REGISTERED_BY { get; set; }

    public DateTime? REGISTERED_DATE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }
}
