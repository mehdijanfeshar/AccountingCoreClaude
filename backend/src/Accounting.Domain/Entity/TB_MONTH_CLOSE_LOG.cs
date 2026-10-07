using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// نتیجهٔ صورتحساب ماه برای یک واحد در یک اجرا (DDL 074، ۲۰۲۶-۱۰-۰۷). <see cref="RESULT"/>: ۱ = صورتحساب شد
/// (اسناد بررسی‌شده تأیید دائم شدند)، ۲ = رد شد (سند یادداشت/موقت یا اعلامیهٔ ارسال‌نشده — <see cref="REASON"/>).
/// </summary>
public partial class TB_MONTH_CLOSE_LOG
{
    public Guid ID { get; set; }

    public Guid BATCH_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public int MONTH { get; set; }

    public int RESULT { get; set; }

    public int ACCEPTED_COUNT { get; set; }

    public int PENDING_VOUCHERS { get; set; }

    public int PENDING_ELAMS { get; set; }

    public string? REASON { get; set; }

    public string USERID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }
}
