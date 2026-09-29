using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «انتقال وجه» بین دو حساب بانکی خزانه‌داری (<c>TRF-xxxxxx</c>) — بخش ۴-ج
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹). تک‌مرحله‌ای تأیید (فقط خزانه‌دار،
/// برخلاف زنجیرهٔ سه‌مرحله‌ای <see cref="TB_TR_PAYMENT_REQUEST"/>). <c>approve</c> دو کنترل
/// مسدودکننده دارد (موجودی مبدأ، سقف روزانهٔ انتقال) پیش از صدور سند GL موقت.
/// </summary>
public partial class TB_TR_TRANSFER
{
    public Guid ID { get; set; }

    /// <summary>نمایش کامل («<c>TRF-</c>» + شمارندهٔ ۶رقمی به‌ازای (VAHEDCODE, YEAR)).</summary>
    public string CODE { get; set; } = null!;

    /// <summary>FK به <c>TB_ACCOUNT</c> — حساب بانکی مبدأ.</summary>
    public Guid SOURCE_BANK_ACCOUNT_ID { get; set; }

    /// <summary>FK به <c>TB_ACCOUNT</c> — حساب بانکی مقصد. باید با
    /// <see cref="SOURCE_BANK_ACCOUNT_ID"/> متفاوت باشد.</summary>
    public Guid DEST_BANK_ACCOUNT_ID { get; set; }

    public decimal AMOUNT { get; set; }

    /// <summary>شمسی <c>YYYYMMDD</c>.</summary>
    public string TRANSFER_DATE { get; set; } = null!;

    public TreasuryPaymentMethod TRANSFER_METHOD { get; set; }

    public string REASON { get; set; } = null!;

    public TransferState STATE { get; set; }

    /// <summary>شمارهٔ پیگیری/مرجع بانکی — فقط در لحظهٔ <c>approve</c> ست می‌شود، از آن پس الزامی.</summary>
    public string? BANK_REFERENCE { get; set; }

    /// <summary>سند GL موقت «انتقال» — فقط پس از <c>approve</c>.</summary>
    public Guid? VOUCHER_ID { get; set; }

    public string? APPROVED_BY { get; set; }

    public DateTime? APPROVED_DATE { get; set; }

    /// <summary>آخرین دلیل بازگشت (<c>return</c>) — با تأیید دوباره پاک نمی‌شود؛ تاریخچهٔ کامل در
    /// «گردش عملیات» (<see cref="TB_TR_TRANSFER_EVENT"/>).</summary>
    public string? RETURN_REASON { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }
}
