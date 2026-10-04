using System.Globalization;
using System.Text.RegularExpressions;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Narratives;

/// <summary>
/// متغیرهای متن یادداشت توضیحی (ح-۶، سند منبع §۹): <c>{{[TPL/]ROW[.cur|.prior|.change|.change%]}}</c> — مبلغ
/// ردیف <c>ROW</c> از Snapshot اجرا. بدون <c>TPL</c> اول یادداشت عددی پیوسته، سپس صورت‌های اصلی، سپس بقیه
/// جستجو می‌شوند. مبلغ به علامت نمایشی (ماهیت بستانکار قرینه)، تقسیم بر واحد مبلغ، ارقام فارسی، منفی در پرانتز.
/// متغیر ناشناخته «[؟…]» می‌شود. همان قواعد در فرانت (<c>narrativeVariables.ts</c>) — هر دو را با هم عوض کنید.
/// </summary>
public static partial class FsNarrativeVariables
{
    [GeneratedRegex(@"\{\{\s*([^{}]+?)\s*\}\}")]
    private static partial Regex TokenRegex();

    private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

    public static string Replace(string text, IReadOnlyList<FsRunStatementDto> statements, string? linkedTemplateCode, decimal divisor)
        => TokenRegex().Replace(text, m => Resolve(m.Groups[1].Value, statements, linkedTemplateCode, divisor));

    public static string Resolve(string token, IReadOnlyList<FsRunStatementDto> statements, string? linkedTemplateCode, decimal divisor)
    {
        var t = token.Trim();
        var slash = t.IndexOf('/');
        var tpl = slash > 0 ? t[..slash] : null;
        var rest = slash > 0 ? t[(slash + 1)..] : t;
        var dot = rest.IndexOf('.');
        var rowCode = dot > 0 ? rest[..dot] : rest;
        var part = dot > 0 ? rest[(dot + 1)..].ToLowerInvariant() : "cur";

        var row = Find(statements, tpl, rowCode, linkedTemplateCode);

        if (row is null)
        {
            return $"[؟{t}]";
        }

        var sign = row.NormalBalance == FsNormalBalance.Credit ? -1m : 1m;
        decimal? cur = row.AmountCur * sign;
        decimal? prv = row.AmountPrv * sign;

        return part switch
        {
            "cur" => Amount(cur, divisor),
            "prior" or "prv" => Amount(prv, divisor),
            "change" => cur is null || prv is null ? "—" : Amount(cur - prv, divisor),
            "change%" => cur is null || prv is null || prv == 0 ? "—" : Percent((cur.Value - prv.Value) / Math.Abs(prv.Value) * 100m),
            _ => $"[؟{t}]",
        };
    }

    private static FsRunRowDto? Find(IReadOnlyList<FsRunStatementDto> statements, string? tpl, string rowCode, string? linked)
    {
        bool Same(string a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        FsRunRowDto? In(FsRunStatementDto s) => s.Rows.FirstOrDefault(r => Same(r.Code, rowCode));

        if (tpl is not null)
        {
            return statements.Where(s => Same(s.TemplateCode, tpl)).Select(In).FirstOrDefault(r => r is not null);
        }

        return statements.Where(s => Same(s.TemplateCode, linked)).Select(In).FirstOrDefault(r => r is not null)
            ?? statements.Where(s => !s.IsNote).Select(In).FirstOrDefault(r => r is not null)
            ?? statements.Select(In).FirstOrDefault(r => r is not null);
    }

    private static string Amount(decimal? value, decimal divisor)
    {
        if (value is null)
        {
            return "—";
        }

        var scaled = Math.Round(value.Value / (divisor <= 0 ? 1 : divisor), MidpointRounding.AwayFromZero);

        if (scaled == 0)
        {
            return "—";
        }

        var text = Math.Abs(scaled).ToString("#,0", Fa);
        return scaled < 0 ? $"({text})" : text;
    }

    private static string Percent(decimal value)
        => Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0", Fa) + "٪";
}
