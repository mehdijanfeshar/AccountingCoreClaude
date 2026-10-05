namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت کارتابل تأیید چک (دفتر چک) — جایگزین «دستور پرداخت» و «تاییدیه چک» کاغذی سیستم قدیم.
/// گردش: صدور دستور پرداخت ⇒ تأیید رئیس حسابداری ⇒ تأیید مدیر واحد (= تاییدیه چک) ⇒ قابل چاپ.
/// </summary>
public enum ChequeApprovalState
{
    /// <summary>دستور پرداخت صادر شد؛ در انتظار تأیید رئیس حسابداری.</summary>
    PendingAccounting = 1,

    /// <summary>تأیید رئیس حسابداری؛ در انتظار تأیید مدیر واحد.</summary>
    PendingManager = 2,

    /// <summary>تأیید مدیر واحد — تاییدیه چک؛ چک قابل چاپ است.</summary>
    Confirmed = 3,

    /// <summary>برگشت داده شد؛ می‌تواند دوباره صادر شود.</summary>
    Returned = 4,
}

/// <summary>اقدام روی کارتابل تأیید چک.</summary>
public enum ChequeApprovalAction
{
    IssuePaymentOrder = 1,
    ApproveAccounting = 2,
    ApproveManager = 3,
    Return = 4,
}
