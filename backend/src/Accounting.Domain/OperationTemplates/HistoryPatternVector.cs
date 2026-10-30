namespace Accounting.Domain.OperationTemplates;

/// <summary>
/// بردار معنایی (embedding) یک الگوی سابقه — DDL 089. جدول محاسبه‌شده؛ کلید همان شناسهٔ پایدار الگوست (MD5 امضا)
/// پس با «بازسازی» از بین نمی‌رود و فقط الگوهایی که متنشان عوض شده دوباره بردار می‌گیرند.
/// </summary>
public class HistoryPatternVector
{
    public Guid PatternId { get; set; }

    /// <summary>نام مدل embedding (مثل «bge-m3»)؛ با عوض شدن مدل همه دوباره ساخته می‌شوند.</summary>
    public string Model { get; set; } = default!;

    /// <summary>SHA-256 متن ورودی + مدل؛ برابر بودن ⇒ نیاز به ساخت دوباره نیست.</summary>
    public string? TextHash { get; set; }

    public int? Dim { get; set; }

    /// <summary>float32 little-endian، نرمال‌شده (طول ۱).</summary>
    public byte[]? Vector { get; set; }

    public DateTime? CreatedAtUtc { get; set; }
}

/// <summary>
/// یک «معنا» از شرح‌های یک الگوی سابقه — DDL 089. حساب‌ها یکی‌اند ولی شرح‌ها چند کار متفاوت را نشان می‌دهند
/// (مثل «هزینهٔ متفرقه / بانک» ⇒ پذیرایی، ایاب‌وذهاب). محاسبه‌شده با گروه‌بندی معنایی نمونه‌شرح‌ها؛ بدون FK.
/// الگوی یک‌معنایی یک ردیف (IDX = 0) می‌گیرد تا معلوم باشد پردازش شده است.
/// </summary>
public class HistoryPatternMeaning
{
    public Guid PatternId { get; set; }
    public int Index { get; set; }

    /// <summary>نماینده‌ترین شرح (بدون نام طرف حساب و رقم).</summary>
    public string Title { get; set; } = default!;

    /// <summary>سهم از نمونه‌شرح‌های الگو، به درصد.</summary>
    public decimal? Share { get; set; }

    public int? SampleCount { get; set; }

    /// <summary>JSON <c>["…"]</c> — حداکثر ۳ شرح نمونه.</summary>
    public string? SamplesJson { get; set; }

    /// <summary>JSON <c>[{"w":"…","c":n}]</c>.</summary>
    public string? KeywordsJson { get; set; }

    public DateTime? CreatedAtUtc { get; set; }
}
