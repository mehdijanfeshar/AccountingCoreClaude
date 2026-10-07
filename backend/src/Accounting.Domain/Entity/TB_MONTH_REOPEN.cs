using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// رمز برگشت صورتحساب ماه (DDL 073، ۲۰۲۶-۱۰-۰۷). ستاد صادر می‌کند؛ واحد یک‌بار مصرف می‌کند و اسناد
/// «تأیید دائم» آن ماه به «بررسی‌شده» برمی‌گردند. خودِ رمز ذخیره نمی‌شود — از روی
/// (واحد، سال، ماه، <see cref="SEQ"/>) با کلید محرمانه دوباره ساخته می‌شود.
/// </summary>
public partial class TB_MONTH_REOPEN
{
    public Guid ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public int MONTH { get; set; }

    public int SEQ { get; set; }

    public string? REASON { get; set; }

    public string ISSUEDBY { get; set; } = null!;

    public DateTime ISSUEDDATE { get; set; }

    public int FAILED_ATTEMPTS { get; set; }

    public string? USEDBY { get; set; }

    public DateTime? USEDDATE { get; set; }

    public int? REVERTED_COUNT { get; set; }
}
