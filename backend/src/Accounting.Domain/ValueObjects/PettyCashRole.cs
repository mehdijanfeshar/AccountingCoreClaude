namespace Accounting.Domain.ValueObjects;

/// <summary>
/// A بررسی‌کنندهٔ تنخواه's role within one <c>TB_PC_FUND</c> — <c>TB_PC_REVIEWER.ROLE</c>. Added
/// در تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، <c>docs/tankhah-khazaneh-module.md</c>، صفحهٔ ۱۳ پاورپوینت).
/// Custodian (تنخواه‌دار) is <b>not</b> a role here — it is <c>TB_PC_FUND.CUSTODIAN_USERID</c>,
/// checked separately (<c>PettyCashNotCustodianException</c>).
///
/// A single (<c>FUND_ID</c>, <c>REVIEWER_USERID</c>) pair may hold more than one role — the unique
/// key widened from <c>(FUND_ID, REVIEWER_USERID)</c> to <c>(FUND_ID, REVIEWER_USERID, ROLE)</c>
/// for exactly this reason (<c>backend/db/048_petty_cash_roles.sql</c>).
/// </summary>
public enum PettyCashRole
{
    /// <summary>بازرس مالی — کنترل سند (<c>StartReview</c>, <c>Verify</c>) و مجاز به
    /// <c>Return</c>/<c>Reject</c>.</summary>
    Inspector = 1,

    /// <summary>مدیر مالی — تأیید نهایی تا سقف <c>TB_PC_FUND.FINANCE_MANAGER_APPROVAL_LIMIT</c>،
    /// و مجاز به <c>Return</c>/<c>Reject</c>.</summary>
    FinanceManager = 2,

    /// <summary>مدیرعامل — تأیید نهایی بدون سقف مبلغ، و مجاز به <c>Return</c>/<c>Reject</c>.</summary>
    ChiefExecutive = 3,

    /// <summary>حسابدار ارشد — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>، ۲۰۲۶-۰۹-۲۸).
    /// فعلاً هیچ اکشن ترمیم/استردادی به این نقش اختصاص داده نشده (تصمیم محافظه‌کارانهٔ همان تاریخ:
    /// ایجاد/ارسال/حذف پیش‌نویس ترمیم فقط <see cref="FinanceManager"/> یا <see cref="Treasurer"/>)؛
    /// مقدار رزرو شده تا endpoint بررسی‌کنندگان خودکار (<c>funds/{fundId}/reviewers</c>) بتواند
    /// این نقش را هم بپذیرد.</summary>
    SeniorAccountant = 4,

    /// <summary>خزانه‌دار — بخش ۳-الف: اجرای پرداخت ترمیم (<c>record-payment</c>) و، طبق
    /// <c>TB_PC_FUND.REFUND_RECORDER</c>، احتمالاً ثبت استرداد وجه.</summary>
    Treasurer = 5,
}
