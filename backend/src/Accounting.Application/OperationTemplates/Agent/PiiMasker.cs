using System.Text.RegularExpressions;

namespace Accounting.Application.OperationTemplates.Agent;

/// <summary>
/// پوشاندن دادهٔ شخصی جمله پیش از ارسال به مدل بیرونی (G5): شبا، شمارهٔ کارت، تلفن و کد ملی با نشانهٔ
/// <c>[[NID_1]]</c>، <c>[[IBAN_1]]</c>، <c>[[CARD_1]]</c>، <c>[[TEL_1]]</c> عوض می‌شوند و پس از پاسخ، در سرور
/// برگردانده می‌شوند (<see cref="PiiMaskResult.Restore"/>). ارقام فارسی، عربی و لاتین هر سه پشتیبانی می‌شوند.
///
/// <para>ترتیب مهم است (از اختصاصی به عمومی): شبا ← کارت ← تلفن ← کد ملی. کد ملی فقط اگر رقم کنترل معتبر داشته
/// باشد پوشانده می‌شود، وگرنه مبلغ ۱۰ رقمی (مثلاً ۱٬۰۰۰٬۰۰۰٬۰۰۰ ریال) را اشتباهاً می‌پوشاند و مدل مبلغ را از
/// دست می‌دهد. ⚠️ پیامد: کد ملی با رقم کنترل نادرست (غلط تایپی) پوشانده نمی‌شود.</para>
/// </summary>
public static class PiiMasker
{
    private const string D = "[0-9۰-۹٠-٩]";
    private const string Sep = "[\\s\\-]?";

    private static readonly Regex Iban = new($"(?<![A-Za-z0-9]|{D})IR{Sep}(?:{D}{Sep}){{23}}{D}(?!{D})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Card = new($"(?<!{D})(?:{D}{{4}}[\\s\\-]?){{3}}{D}{{4}}(?!{D})", RegexOptions.Compiled);
    private static readonly Regex Phone = new(
        $"(?<!{D})(?:(?:\\+98|0098)[\\s\\-]?9{D}{{9}}|09{D}{{9}}|0{D}{{2}}[\\s\\-]?{D}{{8}})(?!{D})", RegexOptions.Compiled);
    private static readonly Regex Nid = new($"(?<!{D})(?:{D}{{10}})(?!{D})", RegexOptions.Compiled);

    public static PiiMaskResult Mask(string? text)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(text)) return new(text ?? "", map);

        var counters = new Dictionary<string, int>();
        string Replace(string input, Regex rx, string kind, Func<string, bool>? accept = null) =>
            rx.Replace(input, m =>
            {
                if (accept is not null && !accept(m.Value)) return m.Value;
                counters[kind] = counters.GetValueOrDefault(kind) + 1;
                var token = $"[[{kind}_{counters[kind]}]]";
                map[token] = m.Value;
                return token;
            });

        var s = Replace(text, Iban, "IBAN");
        s = Replace(s, Card, "CARD");
        s = Replace(s, Phone, "TEL");
        s = Replace(s, Nid, "NID", IsValidNationalCode);
        return new(s, map);
    }

    /// <summary>رقم کنترل کد ملی ایران (و ردّ «همهٔ ارقام یکسان»).</summary>
    internal static bool IsValidNationalCode(string raw)
    {
        var digits = new int[raw.Length];
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            digits[i] = c switch
            {
                >= '۰' and <= '۹' => c - '۰',
                >= '٠' and <= '٩' => c - '٠',
                _ => c - '0',
            };
        }
        if (digits.Length != 10 || digits.Distinct().Count() == 1) return false;
        var sum = 0;
        for (var i = 0; i < 9; i++) sum += digits[i] * (10 - i);
        var r = sum % 11;
        return digits[9] == (r < 2 ? r : 11 - r);
    }
}

/// <param name="Masked">متن پوشانده‌شده — تنها چیزی که به مدل می‌رود یا ذخیره می‌شود.</param>
/// <param name="Map">نشانه ⇒ مقدار اصلی؛ فقط در حافظهٔ همین درخواست.</param>
public sealed record PiiMaskResult(string Masked, IReadOnlyDictionary<string, string> Map)
{
    public bool HasMasked => Map.Count > 0;

    /// <summary>نشانه‌های داخل پاسخ مدل (JSON خام) را به مقدار اصلی برمی‌گرداند. مقدارها فقط رقم/IR/جداکننده‌اند، پس JSON نمی‌شکند.</summary>
    public string Restore(string text)
    {
        if (Map.Count == 0 || string.IsNullOrEmpty(text)) return text;
        foreach (var (token, original) in Map)
            text = text.Replace(token, original, StringComparison.Ordinal);
        return text;
    }
}

/// <summary>
/// پوشاندن چند متن در یک درخواست با یک نقشهٔ مشترک (گفتگو با دستیار، فاز ۶۵): مقدار یکسان همیشه همان نشانه را
/// می‌گیرد و نشانهٔ دو پیام با هم برخورد نمی‌کند (<see cref="PiiMasker.Mask"/> شمارنده را برای هر متن از ۱ شروع می‌کند).
/// </summary>
public sealed class PiiMaskSession
{
    private readonly Dictionary<string, string> _tokenToValue = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _valueToToken = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _counters = new(StringComparer.Ordinal);

    public bool HasMasked => _tokenToValue.Count > 0;

    public string Mask(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        var single = PiiMasker.Mask(text);
        if (!single.HasMasked) return single.Masked;

        // نشانه‌های محلی ⇒ نشانه‌های سراسری همین جلسه؛ از آخر به اول تا [[NID_1]] جای [[NID_12]] را نگیرد.
        var masked = single.Masked;
        foreach (var (local, value) in single.Map.OrderByDescending(kv => kv.Key.Length).ThenByDescending(kv => kv.Key, StringComparer.Ordinal))
        {
            if (!_valueToToken.TryGetValue(value, out var global))
            {
                var kind = local[2..local.IndexOf('_')];
                _counters[kind] = _counters.GetValueOrDefault(kind) + 1;
                global = $"[[{kind}_{_counters[kind]}]]";
                _valueToToken[value] = global;
                _tokenToValue[global] = value;
            }
            masked = masked.Replace(local, "\u0001" + global[2..], StringComparison.Ordinal);
        }
        return masked.Replace("\u0001", "[[", StringComparison.Ordinal);
    }

    public string Restore(string? text)
    {
        if (string.IsNullOrEmpty(text) || _tokenToValue.Count == 0) return text ?? "";
        foreach (var (token, value) in _tokenToValue.OrderByDescending(kv => kv.Key.Length))
            text = text.Replace(token, value, StringComparison.Ordinal);
        return text;
    }
}
