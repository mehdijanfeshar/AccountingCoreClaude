namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت اعلامیه — <c>TB_ELAMHEAD.WEB_STAT</c> (معادل <c>WebStat</c> در پروژهٔ مرجع،
/// <c>Tamin.Core\Entities\Elam\WebStat.cs</c>). ستون همچنان <c>byte?</c> نگاشت می‌شود؛ این enum فقط
/// برای خوانایی در Application است. بازه‌ها نوع اعلامیه را هم معلوم می‌کنند: ۱..۴ درآمد، ۵..۸ سایر
/// صادره، ۹..۱۰ رسیده.
/// </summary>
public enum ElamWebStat : byte
{
    /// <summary>تهیهٔ اعلامیهٔ صادرهٔ درآمد.</summary>
    CreateDramad = 1,

    /// <summary>صدور سند اعلامیهٔ صادرهٔ درآمد.</summary>
    VoucherDramad = 2,

    /// <summary>تأیید اولیهٔ اعلامیهٔ صادرهٔ درآمد.</summary>
    FirstConfirmDramad = 3,

    /// <summary>تأیید نهایی و ارسال اعلامیهٔ صادرهٔ درآمد.</summary>
    FinalConfirmDramad = 4,

    /// <summary>تهیهٔ سایر اعلامیهٔ صادره.</summary>
    CreateOther = 5,

    /// <summary>صدور سند سایر اعلامیهٔ صادره.</summary>
    VoucherOther = 6,

    /// <summary>تأیید اولیهٔ سایر اعلامیهٔ صادره.</summary>
    FirstConfirmOther = 7,

    /// <summary>تأیید نهایی و ارسال سایر اعلامیهٔ صادره.</summary>
    FinalConfirmOther = 8,

    /// <summary>دریافت اعلامیهٔ رسیده.</summary>
    RcvInfo = 9,

    /// <summary>تأیید دریافت و صدور سند اعلامیهٔ رسیده.</summary>
    ConfirmRcvInfo = 10,
}

/// <summary>نوع اعلامیه، مشتق از <see cref="ElamWebStat"/> (معادل <c>ElamType</c> مرجع).</summary>
public enum ElamKind
{
    /// <summary>سایر اعلامیهٔ صادره — کد نوع رابط «1».</summary>
    Sent = 1,

    /// <summary>سایر اعلامیهٔ رسیده — کد نوع رابط «2».</summary>
    Received = 2,

    /// <summary>اعلامیهٔ صادرهٔ درآمد — کد نوع رابط «3».</summary>
    Revenue = 3,
}
