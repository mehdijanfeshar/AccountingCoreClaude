using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// یک ردیف صورت‌حساب بانکی (برداشت یا واریز) — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹). دقیقاً یکی از
/// <see cref="WITHDRAWAL"/>/<see cref="DEPOSIT"/> باید بزرگ‌تر از صفر باشد (کنترل سمت Application،
/// نه <c>CHECK</c> دیتابیسی). برخلاف <c>TB_TR_PAYMENT_REQUEST_LINK_TAFSILI</c> جدول
/// permanently-embedded <b>نیست</b> — یک aggregate root کامل با CRUD مستقل خودش
/// (<c>ITreasuryBankStatementLineRepository</c>)، فقط زیر مسیر REST والدش نشانده شده، دقیقاً مثل
/// <c>TB_VOUCHERSDETAIL</c> زیر <c>TB_VOUCHERSHEAD</c>.
/// </summary>
public partial class TB_TR_BANK_STATEMENT_LINE
{
    public Guid ID { get; set; }

    public Guid STATEMENT_ID { get; set; }

    /// <summary>شمسی <c>YYYYMMDD</c>.</summary>
    public string LINE_DATE { get; set; } = null!;

    /// <summary>شمارهٔ پیگیری/مرجع بانکی — اختیاری، برای تطبیق اولویت (۱) auto-match استفاده می‌شود.</summary>
    public string? BANK_REFERENCE { get; set; }

    public string? DESCRIPTION { get; set; }

    public decimal WITHDRAWAL { get; set; }

    public decimal DEPOSIT { get; set; }

    /// <summary>مانده پس از این ردیف طبق صورت‌حساب بانک — اختیاری، فقط نمایشی.</summary>
    public decimal? BALANCE { get; set; }

    public BankStatementLineMatchState MATCH_STATE { get; set; }

    /// <summary>FK به <c>TB_VOUCHERSDETAIL</c> — ردیف دفتری تطبیق‌یافته (خودکار یا دستی). بدون FK
    /// واقعی در EF.</summary>
    public Guid? MATCHED_VOUCHERDETAIL_ID { get; set; }

    public BankStatementLineResolutionType? RESOLUTION_TYPE { get; set; }

    /// <summary>سند GL موقت «کارمزد بانکی» — فقط وقتی <see cref="RESOLUTION_TYPE"/> =
    /// <see cref="BankStatementLineResolutionType.BankFeeVoucher"/>.</summary>
    public Guid? RESOLUTION_VOUCHER_ID { get; set; }

    /// <summary>FK به <c>TB_TR_RECEIPT</c> — فقط وقتی <see cref="RESOLUTION_TYPE"/> =
    /// <see cref="BankStatementLineResolutionType.LinkedReceipt"/>.</summary>
    public Guid? RESOLUTION_RECEIPT_ID { get; set; }

    public string? RESOLUTION_NOTE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_TR_BANK_STATEMENT? STATEMENT { get; set; }
}
