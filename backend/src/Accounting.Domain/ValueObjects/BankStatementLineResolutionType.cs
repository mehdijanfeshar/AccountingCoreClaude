namespace Accounting.Domain.ValueObjects;

/// <summary>
/// How an unmatched <c>TB_TR_BANK_STATEMENT_LINE</c> row was resolved — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹).
/// </summary>
public enum BankStatementLineResolutionType
{
    /// <summary>برداشت‌های تنها — صدور سند GL موقت «کارمزد بانکی» (بدهکار حساب کارمزد، بستانکار
    /// بانک). فقط روی ردیف برداشتی مجاز است.</summary>
    BankFeeVoucher = 1,

    /// <summary>واریزهای تنها — لینک به یک دریافت وجه <c>Registered</c> همان حساب بانکی و همان
    /// مبلغ. فقط روی ردیف واریزی مجاز است.</summary>
    LinkedReceipt = 2,

    /// <summary>نادیده‌گرفته‌شده — یادداشت اجباری، بدون اثر حسابداری.</summary>
    Ignored = 3,
}
