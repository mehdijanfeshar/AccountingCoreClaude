namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Which role may record an استرداد وجه (<c>TB_PC_REFUND</c>) for one specific تنخواه —
/// <c>TB_PC_FUND.REFUND_RECORDER</c>, بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>، صفحهٔ ۱۱
/// پاورپوینت، ۲۰۲۶-۰۹-۲۸). Owner decision: the recorder is not hard-coded in code — the مدیر مالی
/// configures it per fund when defining/editing the تنخواه (<c>CreatePettyCashFundCommand</c>/
/// <c>UpdatePettyCashFundCommand</c>'s <c>RefundRecorder</c> field). Default (when unset by the
/// caller) is <see cref="Treasurer"/>.
/// </summary>
public enum PettyCashRefundRecorder
{
    /// <summary>تنخواه‌دار — یعنی <c>TB_PC_FUND.CUSTODIAN_USERID</c>، نه یک <c>TB_PC_REVIEWER</c>
    /// جداگانه.</summary>
    Custodian = 1,

    /// <summary>خزانه‌دار — <see cref="PettyCashRole.Treasurer"/> فعال روی همین تنخواه.</summary>
    Treasurer = 2,

    /// <summary>حسابدار ارشد — <see cref="PettyCashRole.SeniorAccountant"/> فعال روی همین
    /// تنخواه.</summary>
    SeniorAccountant = 3,

    /// <summary>مدیر مالی — <see cref="PettyCashRole.FinanceManager"/> فعال روی همین تنخواه.</summary>
    FinanceManager = 4,
}
