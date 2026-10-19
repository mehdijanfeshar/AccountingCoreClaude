namespace Accounting.Domain.OperationTemplates;

// یادگیری از سوابق حسابیار (فاز ۶۳، DDL 085) — الگوی حساب‌های تکرارشدهٔ سندهای سال‌های قبل.
// جدول‌های محاسبه‌شده‌اند (هر «بازسازی» کل محتوا را جایگزین می‌کند)؛ ورودی کاربر نیستند.

/// <summary>
/// یک «الگوی سابقه»: مجموعهٔ یکتای (معین، سمت) که در ≥۳ سند دائم/بررسی‌شدهٔ سال‌های اخیر تکرار شده است.
/// <see cref="Signature"/> = «moinId:side|…» مرتب‌شده (side: ۱ بدهکار، ۲ بستانکار).
/// </summary>
public class HistoryPattern
{
    public Guid Id { get; set; }
    public string Signature { get; set; } = default!;

    /// <summary>JSON ردیف‌ها: <c>[{moinId, side, share, levels:[{level, levelId, groupIds[]}]}]</c>.</summary>
    public string? LinesJson { get; set; }

    public int? VoucherCount { get; set; }
    public int? UnitCount { get; set; }

    /// <summary>برای نمایش، مثل «1401-1404».</summary>
    public string? Years { get; set; }
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }

    public long? MedianAmount { get; set; }

    /// <summary>آخرین تاریخ سند (شمسی <c>yyyyMMdd</c>).</summary>
    public string? LastDate { get; set; }

    /// <summary>JSON <c>[{"w":"دارو","c":12}]</c> — پرتکرارترین کلمه‌های نرمال‌شدهٔ شرح.</summary>
    public string? KeywordsJson { get; set; }

    /// <summary>JSON <c>["خرید دارو …"]</c> — حداکثر ۵ شرح نمونه.</summary>
    public string? SampleDescsJson { get; set; }

    /// <summary>کد نوع واحدهایی که این الگو را داشته‌اند، با «,» جدا.</summary>
    public string? VahedTypes { get; set; }

    public DateTime? RebuiltAtUtc { get; set; }
}

/// <summary>سهم یک واحد از الگو؛ برای ترجیح سابقهٔ خود واحد هنگام تطبیق جمله.</summary>
public class HistoryPatternUnit
{
    public Guid PatternId { get; set; }
    public string VahedCode { get; set; } = default!;
    public int? VoucherCount { get; set; }

    /// <summary>JSON کلمه‌های پرتکرار شرح سندهای همین واحد (مثل <see cref="HistoryPattern.KeywordsJson"/>).</summary>
    public string? KeywordsJson { get; set; }
}

/// <summary>امضای پیشنهاد حذف‌شده توسط مدیر ستاد (DDL 086)؛ بازسازی آن را دوباره نمی‌سازد.</summary>
public class HistoryDismissed
{
    public string Signature { get; set; } = default!;
    public string? AddUserId { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
}
