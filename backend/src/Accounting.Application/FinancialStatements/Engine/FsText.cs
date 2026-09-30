using System.Text.RegularExpressions;

namespace Accounting.Application.FinancialStatements.Engine;

internal static partial class FsText
{
    /// <summary>ارقام فارسی (۰-۹) و عربی (٠-٩) را به لاتین تبدیل می‌کند.</summary>
    public static string NormalizeDigits(string s)
    {
        var chars = s.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];

            if (c is >= '۰' and <= '۹')
            {
                chars[i] = (char)('0' + (c - '۰'));
            }
            else if (c is >= '٠' and <= '٩')
            {
                chars[i] = (char)('0' + (c - '٠'));
            }
        }

        return new string(chars);
    }

    /// <summary>کد ردیف: حرف لاتین + تا ۱۹ حرف/رقم/زیرخط (مثل <c>A01</c>، <c>L99</c>).</summary>
    public static bool IsValidRowCode(string? code)
        => code is not null && RowCodeRegex().IsMatch(code) && !FsFormula.ReservedNames.Contains(code.ToUpperInvariant());

    /// <summary>کد قالب: بخش‌های حرف/رقم/زیرخط جداشده با نقطه (مثل <c>PENSION.NET_ASSETS</c>).</summary>
    public static bool IsValidTemplateCode(string? code)
        => code is not null && TemplateCodeRegex().IsMatch(code);

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,19}$")]
    private static partial Regex RowCodeRegex();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*(\\.[A-Za-z][A-Za-z0-9_]*)*$")]
    private static partial Regex TemplateCodeRegex();
}
