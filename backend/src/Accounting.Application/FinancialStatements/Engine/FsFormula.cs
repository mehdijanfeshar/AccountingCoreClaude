using System.Globalization;

namespace Accounting.Application.FinancialStatements.Engine;

/// <summary>گره درخت نحوی فرمول ردیف قالب.</summary>
public abstract record FsExpr;

public sealed record FsNumberExpr(decimal Value) : FsExpr;

/// <summary><c>A01</c> یا <c>A01.PRV</c> — ردیفی از همین صورت، اختیاری با کلید ستون.</summary>
public sealed record FsRowRefExpr(string RowCode, string? ColumnKey) : FsExpr;

/// <summary><c>STMT(PENSION.CHANGES, X99)</c> — ردیفی از صورت دیگر همان اجرا (ستون جاری).</summary>
public sealed record FsStatementRefExpr(string TemplateCode, string RowCode) : FsExpr;

/// <summary><c>SUM(A01:A08)</c> — جمع ردیف‌های مقداری بین دو ردیف، به ترتیب ارائه (هر دو سر شامل).</summary>
public sealed record FsSumRangeExpr(string FromRowCode, string ToRowCode) : FsExpr;

/// <summary><c>PRIOR(A01)</c> — همان ردیف در دورهٔ مقایسه‌ای قبل.</summary>
public sealed record FsPriorExpr(string RowCode) : FsExpr;

public sealed record FsUnaryExpr(char Op, FsExpr Operand) : FsExpr;

public sealed record FsBinaryExpr(char Op, FsExpr Left, FsExpr Right) : FsExpr;

/// <summary><c>ABS(x)</c> یا <c>ROUND(x, n)</c>.</summary>
public sealed record FsFunctionExpr(string Name, IReadOnlyList<FsExpr> Args) : FsExpr;

/// <summary><c>IF(a &gt; b, x, y)</c>. عملگرها: <c>= &lt;&gt; &lt; &lt;= &gt; &gt;=</c>.</summary>
public sealed record FsIfExpr(string CompareOp, FsExpr CompareLeft, FsExpr CompareRight, FsExpr Then, FsExpr Else) : FsExpr;

/// <summary>
/// فرمول ردیف <c>Formula</c> قالب صورت مالی (سند منبع §۷-۳؛ <c>docs/fs-module.md</c> §۳):
/// <code>
/// expr   := term (("+"|"-") term)*
/// term   := unary (("*"|"/") unary)*
/// unary  := "-" unary | factor
/// factor := NUMBER | ROWCODE | ROWCODE "." COLKEY | func | "(" expr ")"
/// func   := SUM(ROWCODE ":" ROWCODE) | ABS(expr) | ROUND(expr, INT) | IF(expr CMP expr, expr, expr)
///         | PRIOR(ROWCODE) | STMT(TEMPLATECODE, ROWCODE)
/// </code>
/// نام تابع‌ها به بزرگی/کوچکی حرف حساس نیست؛ کد ردیف‌ها هست. ارقام فارسی پیش از تجزیه لاتین می‌شوند.
/// <c>NOTE(...)</c> سند منبع با ماژول یادداشت‌ها (بخش ۴۵-و) می‌آید و فعلاً خطای نحوی است.
/// </summary>
public sealed class FsFormula
{
    internal static readonly HashSet<string> ReservedNames = new(StringComparer.Ordinal)
    {
        "SUM", "ABS", "ROUND", "IF", "PRIOR", "STMT", "NOTE",
    };

    private FsFormula(FsExpr root, string text)
    {
        Root = root;
        Text = text;
    }

    public FsExpr Root { get; }

    public string Text { get; }

    public static bool TryParse(string? text, out FsFormula? formula, out string? error)
    {
        try
        {
            formula = Parse(text);
            error = null;
            return true;
        }
        catch (FsExpressionException ex)
        {
            formula = null;
            error = ex.Message;
            return false;
        }
    }

    /// <exception cref="FsExpressionException">متن نامعتبر است.</exception>
    public static FsFormula Parse(string? text)
    {
        var parser = new Parser(FsText.NormalizeDigits(text ?? string.Empty));
        var root = parser.ParseAll();
        return new FsFormula(root, text ?? string.Empty);
    }

    /// <summary>همهٔ ارجاع‌های این فرمول به ردیف‌های <b>همین</b> صورت (بدون <c>STMT</c>).</summary>
    public IEnumerable<string> DirectRowRefs() => Walk(Root).SelectMany(e => e switch
    {
        FsRowRefExpr r => new[] { r.RowCode },
        FsPriorExpr p => new[] { p.RowCode },
        FsSumRangeExpr s => new[] { s.FromRowCode, s.ToRowCode },
        _ => Array.Empty<string>(),
    });

    public IEnumerable<FsSumRangeExpr> SumRanges() => Walk(Root).OfType<FsSumRangeExpr>();

    public IEnumerable<FsStatementRefExpr> StatementRefs() => Walk(Root).OfType<FsStatementRefExpr>();

    /// <summary>ارجاع‌هایی که در ترتیب محاسبهٔ دورهٔ جاری اثر دارند (<c>PRIOR</c> و ارجاع با کلید ستون نه).</summary>
    public IEnumerable<string> CurrentPeriodRowRefs() => Walk(Root).SelectMany(e => e switch
    {
        FsRowRefExpr { ColumnKey: null } r => new[] { r.RowCode },
        _ => Array.Empty<string>(),
    });

    public static IEnumerable<FsExpr> Walk(FsExpr e)
    {
        yield return e;

        IEnumerable<FsExpr> children = e switch
        {
            FsUnaryExpr u => new[] { u.Operand },
            FsBinaryExpr b => new[] { b.Left, b.Right },
            FsFunctionExpr f => f.Args,
            FsIfExpr i => new[] { i.CompareLeft, i.CompareRight, i.Then, i.Else },
            _ => Array.Empty<FsExpr>(),
        };

        foreach (var c in children)
        {
            foreach (var d in Walk(c))
            {
                yield return d;
            }
        }
    }

    private enum TokKind { Number, Ident, Op, LParen, RParen, Comma, Colon, Dot, End }

    private readonly record struct Token(TokKind Kind, string Text, int Pos);

    private sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _p;

        public Parser(string s)
        {
            _tokens = Tokenize(s);
        }

        public FsExpr ParseAll()
        {
            if (Peek.Kind == TokKind.End)
            {
                throw new FsExpressionException("فرمول خالی است.", 0);
            }

            var e = ParseExpr();

            if (Peek.Kind != TokKind.End)
            {
                throw new FsExpressionException($"«{Peek.Text}» در این جای فرمول انتظار نمی‌رفت.", Peek.Pos);
            }

            return e;
        }

        private Token Peek => _tokens[_p];

        private Token Next() => _tokens[_p++];

        private Token Expect(TokKind kind, string what)
        {
            var t = Peek;

            if (t.Kind != kind)
            {
                throw new FsExpressionException(
                    t.Kind == TokKind.End ? $"{what} انتظار می‌رفت ولی فرمول تمام شد." : $"{what} انتظار می‌رفت ولی «{t.Text}» آمد.",
                    t.Pos);
            }

            _p++;
            return t;
        }

        private FsExpr ParseExpr()
        {
            var left = ParseTerm();

            while (Peek.Kind == TokKind.Op && Peek.Text is "+" or "-")
            {
                var op = Next().Text[0];
                left = new FsBinaryExpr(op, left, ParseTerm());
            }

            return left;
        }

        private FsExpr ParseTerm()
        {
            var left = ParseUnary();

            while (Peek.Kind == TokKind.Op && Peek.Text is "*" or "/")
            {
                var op = Next().Text[0];
                left = new FsBinaryExpr(op, left, ParseUnary());
            }

            return left;
        }

        private FsExpr ParseUnary()
        {
            if (Peek.Kind == TokKind.Op && Peek.Text == "-")
            {
                Next();
                return new FsUnaryExpr('-', ParseUnary());
            }

            if (Peek.Kind == TokKind.Op && Peek.Text == "+")
            {
                Next();
                return ParseUnary();
            }

            return ParseFactor();
        }

        private FsExpr ParseFactor()
        {
            var t = Peek;

            switch (t.Kind)
            {
                case TokKind.Number:
                    Next();
                    return new FsNumberExpr(decimal.Parse(t.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));

                case TokKind.LParen:
                    Next();
                    var inner = ParseExpr();
                    Expect(TokKind.RParen, "«)»");
                    return inner;

                case TokKind.Ident:
                    Next();

                    if (Peek.Kind == TokKind.LParen)
                    {
                        return ParseFunction(t);
                    }

                    if (ReservedNames.Contains(t.Text.ToUpperInvariant()))
                    {
                        throw new FsExpressionException($"«{t.Text}» نام تابع است و باید با «(» بیاید.", t.Pos);
                    }

                    string? col = null;

                    if (Peek.Kind == TokKind.Dot)
                    {
                        Next();
                        col = Expect(TokKind.Ident, "کلید ستون").Text;
                    }

                    return new FsRowRefExpr(t.Text, col);

                default:
                    throw new FsExpressionException(
                        t.Kind == TokKind.End ? "فرمول ناقص تمام شد." : $"«{t.Text}» در این جای فرمول انتظار نمی‌رفت.",
                        t.Pos);
            }
        }

        private FsExpr ParseFunction(Token nameTok)
        {
            var name = nameTok.Text.ToUpperInvariant();
            Expect(TokKind.LParen, "«(»");
            FsExpr result;

            switch (name)
            {
                case "SUM":
                    var from = Expect(TokKind.Ident, "کد ردیف").Text;
                    Expect(TokKind.Colon, "«:»");
                    var to = Expect(TokKind.Ident, "کد ردیف").Text;
                    result = new FsSumRangeExpr(from, to);
                    break;

                case "ABS":
                    result = new FsFunctionExpr("ABS", new[] { ParseExpr() });
                    break;

                case "ROUND":
                    var arg = ParseExpr();
                    Expect(TokKind.Comma, "«,»");
                    var digitsTok = Expect(TokKind.Number, "تعداد رقم (عدد صحیح)");

                    if (!int.TryParse(digitsTok.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var digits) || digits > 12)
                    {
                        throw new FsExpressionException("آرگومان دوم ROUND باید عدد صحیح ۰ تا ۱۲ باشد.", digitsTok.Pos);
                    }

                    result = new FsFunctionExpr("ROUND", new[] { arg, new FsNumberExpr(digits) });
                    break;

                case "IF":
                    var cl = ParseExpr();

                    if (Peek.Kind != TokKind.Op || Peek.Text is not ("=" or "<>" or "<" or "<=" or ">" or ">="))
                    {
                        throw new FsExpressionException("شرط IF به عملگر مقایسه (= <> < <= > >=) نیاز دارد.", Peek.Pos);
                    }

                    var cmp = Next().Text;
                    var cr = ParseExpr();
                    Expect(TokKind.Comma, "«,»");
                    var th = ParseExpr();
                    Expect(TokKind.Comma, "«,»");
                    var el = ParseExpr();
                    result = new FsIfExpr(cmp, cl, cr, th, el);
                    break;

                case "PRIOR":
                    result = new FsPriorExpr(Expect(TokKind.Ident, "کد ردیف").Text);
                    break;

                case "STMT":
                    var code = Expect(TokKind.Ident, "کد قالب").Text;

                    while (Peek.Kind == TokKind.Dot)
                    {
                        Next();
                        code += "." + Expect(TokKind.Ident, "ادامهٔ کد قالب").Text;
                    }

                    Expect(TokKind.Comma, "«,»");
                    result = new FsStatementRefExpr(code, Expect(TokKind.Ident, "کد ردیف").Text);
                    break;

                case "NOTE":
                    throw new FsExpressionException("تابع NOTE با ماژول یادداشت‌ها می‌آید و هنوز پشتیبانی نمی‌شود.", nameTok.Pos);

                default:
                    throw new FsExpressionException($"تابع ناشناخته «{nameTok.Text}».", nameTok.Pos);
            }

            Expect(TokKind.RParen, "«)»");
            return result;
        }

        private static List<Token> Tokenize(string s)
        {
            var list = new List<Token>();
            var i = 0;

            while (i < s.Length)
            {
                var c = s[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                var start = i;

                if (char.IsAsciiDigit(c))
                {
                    while (i < s.Length && char.IsAsciiDigit(s[i]))
                    {
                        i++;
                    }

                    if (i + 1 < s.Length && s[i] == '.' && char.IsAsciiDigit(s[i + 1]))
                    {
                        i++;

                        while (i < s.Length && char.IsAsciiDigit(s[i]))
                        {
                            i++;
                        }
                    }

                    list.Add(new Token(TokKind.Number, s[start..i], start));
                    continue;
                }

                if (char.IsAsciiLetter(c) || c == '_')
                {
                    while (i < s.Length && (char.IsAsciiLetterOrDigit(s[i]) || s[i] == '_'))
                    {
                        i++;
                    }

                    list.Add(new Token(TokKind.Ident, s[start..i], start));
                    continue;
                }

                if ((c == '<' || c == '>') && i + 1 < s.Length && (s[i + 1] == '=' || (c == '<' && s[i + 1] == '>')))
                {
                    list.Add(new Token(TokKind.Op, s.Substring(i, 2), start));
                    i += 2;
                    continue;
                }

                var kind = c switch
                {
                    '+' or '-' or '*' or '/' or '=' or '<' or '>' => TokKind.Op,
                    '(' => TokKind.LParen,
                    ')' => TokKind.RParen,
                    ',' or '،' => TokKind.Comma,
                    ':' => TokKind.Colon,
                    '.' => TokKind.Dot,
                    _ => throw new FsExpressionException($"نویسهٔ نامعتبر «{c}» در فرمول.", i),
                };

                list.Add(new Token(kind, c.ToString(), start));
                i++;
            }

            list.Add(new Token(TokKind.End, string.Empty, s.Length));
            return list;
        }
    }
}
