using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «درخواست پرداخت» خزانه‌داری (<c>PAY-xxxxxx</c>) — بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۸). جدول جانبی کاملاً جدید؛ اجرای واقعی
/// پرداخت (پر شدن Legacy <c>TB_PAYRECIVHEAD/DETAIL</c>) در بخش ۴-ب اتفاق می‌افتد —
/// <see cref="PAYRECIVHEAD_ID"/> تا آن زمان <see langword="null"/> می‌ماند.
///
/// <c>NET_PAYABLE_AMOUNT</c> همیشه سرور محاسبه می‌کند (قبل از مالیات + ارزش‌افزوده − کسور بیمه) —
/// هرگز مستقیماً از ورودی گرفته نمی‌شود. <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛
/// همیشه application-side تولید می‌شود، مثل بقیهٔ جدول‌های جانبی این ماژول.
/// </summary>
public partial class TB_TR_PAYMENT_REQUEST
{
    public Guid ID { get; set; }

    /// <summary>نمایش کامل («<c>PAY-</c>» + شمارندهٔ ۶رقمی به‌ازای (VAHEDCODE, YEAR)).</summary>
    public string CODE { get; set; } = null!;

    public string BENEFICIARY_NAME { get; set; } = null!;

    public string? BENEFICIARY_NATIONAL_ID { get; set; }

    public Guid? BENEFICIARY_TAFSILI_ID { get; set; }

    public TreasuryPaymentType PAYMENT_TYPE { get; set; }

    public string? INVOICE_REF { get; set; }

    /// <summary>تیک دستی ثبت‌کننده — ماژول فاکتور نداریم؛ تصمیم صاحب پروژه (۲۰۲۶-۰۹-۲۸).</summary>
    public bool INVOICE_APPROVED { get; set; }

    /// <summary>FK به <c>TB_ACCOUNTCODE</c> — حساب هزینه.</summary>
    public Guid EXPENSE_ACCOUNT_ID { get; set; }

    public Guid? COST_CENTER_TAFSILI_ID { get; set; }

    public decimal AMOUNT_BEFORE_TAX { get; set; }

    public decimal? VAT_PERCENT { get; set; }

    public decimal VAT_AMOUNT { get; set; }

    public decimal? INSURANCE_DEDUCTION_PERCENT { get; set; }

    public decimal INSURANCE_DEDUCTION_AMOUNT { get; set; }

    /// <summary>سرور محاسبه می‌کند = <see cref="AMOUNT_BEFORE_TAX"/> + <see cref="VAT_AMOUNT"/> −
    /// <see cref="INSURANCE_DEDUCTION_AMOUNT"/>. هرگز ورودی مستقیم کاربر نیست.</summary>
    public decimal NET_PAYABLE_AMOUNT { get; set; }

    /// <summary>شمسی <c>YYYYMMDD</c>.</summary>
    public string DUE_DATE { get; set; } = null!;

    /// <summary>FK به <c>TB_ACCOUNT</c> — حساب بانکی مبدأ.</summary>
    public Guid PAYMENT_ACCOUNT_ID { get; set; }

    public TreasuryPaymentMethod? PAYMENT_METHOD { get; set; }

    public string? DESCRIPTION { get; set; }

    public PaymentRequestState REQUEST_STATE { get; set; }

    public DateTime? SUBMITTED_DATE { get; set; }

    /// <summary>بخش ۴-ب پر می‌کند — سرسند Legacy پرداخت اجراشده.</summary>
    public Guid? PAYRECIVHEAD_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }
}
