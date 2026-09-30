using System.Text;

namespace Accounting.Application.FinancialStatements.Engine;

/// <summary>نوع یک جزء انتخاب‌گر حساب.</summary>
public enum SelectorTermKind
{
    /// <summary><c>1301</c> — معین با همین کد دقیق.</summary>
    Exact = 1,
    /// <summary><c>13*</c> — همهٔ معین‌هایی که کدشان با <c>13</c> شروع می‌شود.</summary>
    Prefix = 2,
    /// <summary><c>1301..1309</c> — معین‌هایی که پیشوندِ هم‌طول با دو سرِ بازه، بین آن دو است.</summary>
    Range = 3,
}

/// <summary>محدودیت مانده برای یک جزء: <c>[D]</c> فقط وقتی مانده بدهکار است، <c>[C]</c> فقط بستانکار.</summary>
public enum SelectorBalanceSide
{
    Any = 0,
    DebitOnly = 1,
    CreditOnly = 2,
}

public sealed record SelectorTerm(bool Exclude, SelectorTermKind Kind, string From, string? To, SelectorBalanceSide Side)
{
    /// <summary>آیا کد معین <paramref name="moeinCode"/> زیر این جزء می‌آید (بدون درنظرگرفتن <see cref="Side"/>).</summary>
    public bool MatchesCode(string moeinCode) => Kind switch
    {
        SelectorTermKind.Exact => string.Equals(moeinCode, From, StringComparison.Ordinal),
        SelectorTermKind.Prefix => moeinCode.StartsWith(From, StringComparison.Ordinal),
        SelectorTermKind.Range => moeinCode.Length >= From.Length
            && string.CompareOrdinal(moeinCode[..From.Length], From) >= 0
            && string.CompareOrdinal(moeinCode[..From.Length], To) <= 0,
        _ => false,
    };
}

/// <summary>
/// انتخاب‌گر حساب یک ردیف <c>Account</c> (سند منبع §۷-۲؛ <c>docs/fs-module.md</c> §۳). اجزا با فاصله یا
/// ویرگول جدا می‌شوند:
/// <list type="bullet">
/// <item><c>1301</c> دقیق، <c>13*</c> پیشوند، <c>1301..1309</c> بازه (دو سر هم‌طول)؛</item>
/// <item><c>!</c> در ابتدا = استثنا؛ <c>[D]</c>/<c>[C]</c> در انتها = فقط مانده بدهکار/بستانکار.</item>
/// </list>
/// همیشه روی کد <b>معین</b> حل می‌شود. یک معین انتخاب می‌شود اگر حداقل یک جزء غیراستثنا شاملش شود و
/// هیچ جزء استثنایی شاملش نشود. فیلتر تفصیلی (<c>{...}</c>) هنوز پشتیبانی نمی‌شود و خطای نحوی می‌دهد.
/// ارقام فارسی/عربی پیش از تجزیه به لاتین تبدیل می‌شوند.
/// </summary>
public sealed class AccountSelector
{
    private AccountSelector(IReadOnlyList<SelectorTerm> terms)
    {
        Terms = terms;
    }

    public IReadOnlyList<SelectorTerm> Terms { get; }

    public IEnumerable<SelectorTerm> Includes => Terms.Where(t => !t.Exclude);

    public IEnumerable<SelectorTerm> Excludes => Terms.Where(t => t.Exclude);

    public static bool TryParse(string? text, out AccountSelector? selector, out string? error)
    {
        try
        {
            selector = Parse(text);
            error = null;
            return true;
        }
        catch (FsExpressionException ex)
        {
            selector = null;
            error = ex.Message;
            return false;
        }
    }

    /// <exception cref="FsExpressionException">متن نامعتبر است.</exception>
    public static AccountSelector Parse(string? text)
    {
        var s = FsText.NormalizeDigits(text ?? string.Empty);
        var terms = new List<SelectorTerm>();
        var i = 0;

        while (true)
        {
            while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',' || s[i] == '،'))
            {
                i++;
            }

            if (i >= s.Length)
            {
                break;
            }

            var start = i;
            var exclude = false;

            if (s[i] == '!')
            {
                exclude = true;
                i++;
            }

            var from = ReadCode(s, ref i);
            SelectorTermKind kind;
            string? to = null;

            if (i < s.Length && s[i] == '*')
            {
                kind = SelectorTermKind.Prefix;
                i++;
            }
            else if (i + 1 < s.Length && s[i] == '.' && s[i + 1] == '.')
            {
                i += 2;
                to = ReadCode(s, ref i);

                if (to.Length != from.Length)
                {
                    throw new FsExpressionException($"دو سرِ بازهٔ «{from}..{to}» باید هم‌طول باشند.", start);
                }

                if (string.CompareOrdinal(from, to) > 0)
                {
                    throw new FsExpressionException($"ابتدای بازهٔ «{from}..{to}» از انتهایش بزرگ‌تر است.", start);
                }

                kind = SelectorTermKind.Range;
            }
            else
            {
                kind = SelectorTermKind.Exact;
            }

            var side = SelectorBalanceSide.Any;

            if (i < s.Length && s[i] == '[')
            {
                if (i + 2 < s.Length && s[i + 2] == ']' && (char.ToUpperInvariant(s[i + 1]) is 'D' or 'C'))
                {
                    side = char.ToUpperInvariant(s[i + 1]) == 'D' ? SelectorBalanceSide.DebitOnly : SelectorBalanceSide.CreditOnly;
                    i += 3;
                }
                else
                {
                    throw new FsExpressionException("پس از «[» فقط «D]» (مانده بدهکار) یا «C]» (مانده بستانکار) مجاز است.", i);
                }
            }

            if (i < s.Length && s[i] == '{')
            {
                throw new FsExpressionException("فیلتر تفصیلی ({...}) هنوز پشتیبانی نمی‌شود.", i);
            }

            if (i < s.Length && !(char.IsWhiteSpace(s[i]) || s[i] == ',' || s[i] == '،'))
            {
                throw new FsExpressionException($"نویسهٔ نامعتبر «{s[i]}» در انتخاب‌گر حساب.", i);
            }

            terms.Add(new SelectorTerm(exclude, kind, from, to, side));
        }

        if (terms.Count == 0)
        {
            throw new FsExpressionException("انتخاب‌گر حساب خالی است.", 0);
        }

        if (terms.All(t => t.Exclude))
        {
            throw new FsExpressionException("انتخاب‌گر حساب باید حداقل یک جزء غیراستثنا داشته باشد.", 0);
        }

        return new AccountSelector(terms);
    }

    /// <summary>
    /// جزء غیراستثنایی که معین <paramref name="moeinCode"/> را انتخاب می‌کند، یا <see langword="null"/>
    /// اگر انتخاب نمی‌شود. محدودیت مانده (<see cref="SelectorTerm.Side"/>) را موتور هنگام اجرا اعمال می‌کند.
    /// </summary>
    public SelectorTerm? Match(string moeinCode)
    {
        if (Excludes.Any(t => t.MatchesCode(moeinCode)))
        {
            return null;
        }

        return Includes.FirstOrDefault(t => t.MatchesCode(moeinCode));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var t in Terms)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            if (t.Exclude)
            {
                sb.Append('!');
            }

            sb.Append(t.From);
            sb.Append(t.Kind switch
            {
                SelectorTermKind.Prefix => "*",
                SelectorTermKind.Range => ".." + t.To,
                _ => string.Empty,
            });
            sb.Append(t.Side switch
            {
                SelectorBalanceSide.DebitOnly => "[D]",
                SelectorBalanceSide.CreditOnly => "[C]",
                _ => string.Empty,
            });
        }

        return sb.ToString();
    }

    private static string ReadCode(string s, ref int i)
    {
        var start = i;

        while (i < s.Length && char.IsAsciiLetterOrDigit(s[i]))
        {
            i++;
        }

        if (i == start)
        {
            throw new FsExpressionException(
                i < s.Length ? $"کد حساب انتظار می‌رفت ولی «{s[i]}» آمد." : "کد حساب انتظار می‌رفت ولی متن تمام شد.",
                start);
        }

        return s[start..i];
    }
}
