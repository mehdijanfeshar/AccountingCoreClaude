namespace Accounting.Domain.ValueObjects;

/// <summary>
/// How a ترمیم/شارژ (<c>TB_PC_REPLENISHMENT.PAYMENT_METHOD</c>) is to be paid to the تنخواه‌دار —
/// بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>، صفحهٔ ۹ پاورپوینت، ۲۰۲۶-۰۹-۲۸). Informational
/// only in this batch: the actual money movement (خزانه، بخش ۴+) is out of scope —
/// <c>RecordPettyCashReplenishmentPaymentCommand</c> only marks the ترمیم <c>Paid</c>, it never
/// issues a GL voucher or touches a bank account balance.
/// </summary>
public enum PettyCashPaymentMethod
{
    /// <summary>پایا به حساب تنخواه‌دار.</summary>
    BankTransferToCustodian = 1,

    /// <summary>چک.</summary>
    Check = 2,

    /// <summary>نقد.</summary>
    Cash = 3,

    /// <summary>سایر.</summary>
    Other = 4,
}
