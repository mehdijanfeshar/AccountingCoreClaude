using System.Text.RegularExpressions;

namespace Accounting.Infrastructure.LegacyImport;

internal static class LegacyText
{
    /// <summary>یکسان‌سازی برای مقایسهٔ نام: ی/ک عربی، نیم‌فاصله، فاصله و علامت‌ها حذف.</summary>
    public static string Norm(string? s)
    {
        s = (s ?? "").Replace('ي', 'ی').Replace('ك', 'ک').Replace('ى', 'ی').Replace('ة', 'ه')
            .Replace('أ', 'ا').Replace('إ', 'ا').Replace('ۀ', 'ه').Replace("‌", "").Replace("-", " ");
        s = Regex.Replace(s, @"[^\p{L}\p{N} ]", " ");
        return Regex.Replace(s, @"\s+", "").ToLowerInvariant();
    }

    /// <summary>شباهت دوحرفی Dice بین ۰ و ۱.</summary>
    public static double Sim(string? a, string? b)
    {
        a = Norm(a); b = Norm(b);
        if (a.Length < 2 || b.Length < 2) return a == b ? 1 : 0;
        var ba = Enumerable.Range(0, a.Length - 1).Select(i => a.Substring(i, 2)).ToList();
        var pool = Enumerable.Range(0, b.Length - 1).Select(i => b.Substring(i, 2)).ToList();
        var total = ba.Count + pool.Count;
        var hit = 0;
        foreach (var x in ba)
        {
            var k = pool.IndexOf(x);
            if (k >= 0) { hit++; pool.RemoveAt(k); }
        }
        return 2.0 * hit / total;
    }

    public static string Clean(string? s)
        => Regex.Replace((s ?? "").Replace('ي', 'ی').Replace('ك', 'ک'), @"\s+", " ").Trim();

    public static string Cut(string s, int max) => s.Length <= max ? s : s[..max];

    /// <summary>کل قدیم XY ← کل جدید X0Y0.</summary>
    public static string NewKol(string oldKol) => oldKol.Length == 2 ? $"{oldKol[0]}0{oldKol[1]}0" : "";

    private static readonly Regex TestRx = new(@"test|تست|[a-zA-Z]", RegexOptions.IgnoreCase);

    /// <summary>ردیف آزمایشی درخت جدید (تأیید صاحب پروژه: نادیده گرفته شود).</summary>
    public static bool IsTest(string? name) => TestRx.IsMatch(name ?? "") || Norm(name).Length < 3;

    public static string Date8(string d) => d.Length == 8 ? $"{d[..4]}/{d.Substring(4, 2)}/{d.Substring(6, 2)}" : d;

    /// <summary>دو رقم سال اسکیما ← سال کامل (۵۰ به بالا = ۱۳xx).</summary>
    public static int FullYear(int yy) => yy >= 50 ? 1300 + yy : 1400 + yy;
}
