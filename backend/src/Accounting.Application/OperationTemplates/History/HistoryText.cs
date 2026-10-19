using System.Text;

namespace Accounting.Application.OperationTemplates.History;

/// <summary>
/// نرمال‌سازی متن فارسی و استخراج کلمهٔ کلیدی برای یادگیری از سوابق (فاز ۶۳). قطعی و بدون هوش مصنوعی:
/// ي→ی، ك→ک، حذف نیم‌فاصله/کشیده/اعراب، رقم (فارسی/عربی/لاتین) حذف می‌شود (جداکننده)، کلمهٔ توقف و کلمهٔ یک‌حرفی کنار
/// گذاشته می‌شود و پسوند جمع «ها/های» از کلمه‌های بلند برداشته می‌شود تا «داروها» و «دارو» یکی شوند.
/// </summary>
public static class HistoryText
{
    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "از", "به", "با", "در", "برای", "بابت", "جهت", "تا", "را", "که", "و", "این", "آن", "یا", "هم", "ها", "های",
        "است", "شد", "شده", "بود", "می", "نمی", "هر", "بر", "بین", "طی", "طبق", "توسط", "مورخ", "تاریخ", "مربوط", "مربوطه",
        "سند", "شماره", "شرح", "مبلغ", "ریال", "تومان", "آقای", "خانم", "جناب", "اصلاحی", "کل", "یک", "دو", "بنا", "علی",
        "حساب", "بدهکار", "بستانکار", "ردیف", "فاکتور", "وجه", "مبالغ", "خود", "بنده", "ما", "من", "شما",
    };

    /// <summary>متن نرمال‌شده (ی/ک فارسی، بدون نیم‌فاصله/اعراب/رقم)؛ حروف غیرحرفی به فاصله تبدیل می‌شوند.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var sb = new StringBuilder(text.Length);
        foreach (var raw in text)
        {
            var ch = raw switch { 'ي' => 'ی', 'ى' => 'ی', 'ك' => 'ک', _ => raw };
            if (ch is '‌' or '‍' or 'ـ') continue;                 // نیم‌فاصله، اتصال، کشیده
            if (ch is >= 'ً' and <= 'ٟ' or 'ٰ') continue;       // اعراب
            if (char.IsLetter(ch)) sb.Append(char.ToLowerInvariant(ch));
            else sb.Append(' ');                                               // رقم و علامت ⇒ جداکننده
        }
        return sb.ToString();
    }

    /// <summary>کلمه‌های معنادار یک متن (بدون تکرار ترتیب‌دار نمی‌شود؛ تکرار حفظ می‌شود).</summary>
    public static IReadOnlyList<string> Tokens(string? text)
    {
        var result = new List<string>();
        foreach (var w in Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = Stem(w);
            if (t.Length < 2 || Stopwords.Contains(t) || Stopwords.Contains(w)) continue;
            result.Add(t);
        }
        return result;
    }

    /// <summary>کلمه‌های یکتای یک متن؛ مبنای شمارش «در چند سند آمده است».</summary>
    public static IReadOnlySet<string> DistinctTokens(string? text) => Tokens(text).ToHashSet(StringComparer.Ordinal);

    private static string Stem(string w)
    {
        if (w.Length > 5 && w.EndsWith("های", StringComparison.Ordinal)) return w[..^3];
        if (w.Length > 4 && w.EndsWith("ها", StringComparison.Ordinal)) return w[..^2];
        return w;
    }

    /// <summary>
    /// شرح نمونه برای کاربر عادی: رقم‌ها حذف، و از اولین نشانهٔ نام طرف حساب («از/به/آقای/شرکت/…») به بعد بریده می‌شود
    /// تا نام تفصیلی واحدهای دیگر درز نکند؛ حداکثر <paramref name="max"/> نویسه.
    /// </summary>
    public static string MaskForOtherUnits(string? description, int max = 40)
    {
        if (string.IsNullOrWhiteSpace(description)) return string.Empty;
        var words = new List<string>();
        foreach (var w in Normalize(description).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (NameMarkers.Contains(w) && words.Count > 0) break;
            words.Add(w);
        }
        var s = string.Join(' ', words).Trim();
        return s.Length <= max ? s : s[..max].TrimEnd();
    }

    private static readonly HashSet<string> NameMarkers = new(StringComparer.Ordinal)
    {
        "از", "به", "آقای", "خانم", "جناب", "شرکت", "موسسه", "مؤسسه", "فروشگاه", "دکتر", "مهندس", "توسط", "نزد",
    };

    /// <summary>پرتکرارترین کلمه‌ها از شمارش‌ها، نزولی با شکستن تساوی الفبایی (قطعی).</summary>
    public static List<KeywordCount> Top(IReadOnlyDictionary<string, int> counts, int take) =>
        counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(take).Select(kv => new KeywordCount(kv.Key, kv.Value)).ToList();

}
