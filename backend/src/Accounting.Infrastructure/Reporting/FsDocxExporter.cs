using System.Globalization;
using System.Text.Json;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Narratives;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.ValueObjects;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Accounting.Infrastructure.Reporting;

/// <summary>
/// ح-۶ — Word یادداشت‌های توضیحی (OpenXml، راست‌به‌چپ). سند Tiptap به پاراگراف/عنوان/فهرست/نقل‌قول و قالب‌های
/// پررنگ، کج، زیرخط و خط‌خورده تبدیل می‌شود؛ فهرست‌ها با پیشوند متنی (بدون تعریف شماره‌گذاری Word) تا فایل ساده بماند.
/// </summary>
public sealed class FsDocxExporter : IFsDocxExporter
{
    private const string Font = "B Nazanin";
    private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

    public byte[] ExportNarratives(FsRunDetailDto run, IReadOnlyList<FsRunNarrativeDto> narratives, decimal divisor, string unitLabel)
    {
        using var stream = new MemoryStream();

        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            var body = new Body();
            main.Document = new Document(body);

            var r = run.Run;
            body.Append(Para(r.VahedName ?? r.VahedCode, bold: true, size: 32, center: true));
            body.Append(Para("یادداشت‌های توضیحی صورت‌های مالی", bold: true, size: 28, center: true));
            body.Append(Para(Period(r.Year, r.ToMonth), size: 24, center: true));
            body.Append(Para($"(مبالغ به {unitLabel})", size: 20, center: true));
            body.Append(Para(string.Empty));

            var noteStatements = run.Statements.Where(s => s.IsNote).OrderBy(s => NoteOrder(s.NoteNo)).ToList();
            var noteCodes = noteStatements.Select(s => s.TemplateCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var standalone = narratives.Where(n => n.LinkedTemplateCode is null || !noteCodes.Contains(n.LinkedTemplateCode)).OrderBy(n => n.OrderNo).ToList();
            var textNo = 0;

            foreach (var n in standalone)
            {
                textNo++;
                body.Append(Heading($"{Num(textNo)}. {n.TitleFa}", 2));
                AppendContent(body, n.ContentJson, run, n.LinkedTemplateCode, divisor);
            }

            foreach (var s in noteStatements)
            {
                body.Append(Heading($"{(s.NoteNo is null ? string.Empty : Digits(s.NoteNo) + ". ")}{s.TitleFa}", 2));

                foreach (var n in narratives.Where(n => string.Equals(n.LinkedTemplateCode, s.TemplateCode, StringComparison.OrdinalIgnoreCase)).OrderBy(n => n.OrderNo))
                {
                    AppendContent(body, n.ContentJson, run, n.LinkedTemplateCode, divisor);
                }

                body.Append(NoteTable(s, r.HasPrior, r.Year, divisor));
                body.Append(Para(string.Empty));
            }

            body.Append(Para(
                $"اجرای شمارهٔ {Num(r.RunNo)}" + (run.ContentHash is null ? string.Empty : $" — اثر انگشت {run.ContentHash[..Math.Min(16, run.ContentHash.Length)]}"),
                size: 16));

            body.Append(new SectionProperties(
                new PageSize { Width = 11906U, Height = 16838U },
                new PageMargin { Top = 1134, Bottom = 1134, Left = 1134U, Right = 1134U, Header = 709U, Footer = 709U },
                new BiDi()));

            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static void AppendContent(Body body, string? json, FsRunDetailDto run, string? linked, decimal divisor)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("content", out var content))
            {
                foreach (var node in content.EnumerateArray())
                {
                    AppendBlock(body, node, run, linked, divisor, prefix: null, indent: 0);
                }
            }
        }
        catch (JsonException)
        {
            body.Append(Para("[متن یادداشت قابل خواندن نیست]"));
        }
    }

    private static void AppendBlock(Body body, JsonElement node, FsRunDetailDto run, string? linked, decimal divisor, string? prefix, int indent)
    {
        var type = node.TryGetProperty("type", out var t) ? t.GetString() : null;

        switch (type)
        {
            case "heading":
                var level = node.TryGetProperty("attrs", out var a) && a.TryGetProperty("level", out var l) ? l.GetInt32() : 3;
                var hp = InlinePara(node, run, linked, divisor, prefix, indent, bold: true, size: level <= 1 ? 28 : level == 2 ? 26 : 24);
                body.Append(hp);
                break;

            case "paragraph":
                body.Append(InlinePara(node, run, linked, divisor, prefix, indent));
                break;

            case "blockquote":
                foreach (var child in Children(node))
                {
                    AppendBlock(body, child, run, linked, divisor, prefix, indent + 1);
                }

                break;

            case "bulletList":
            case "orderedList":
                var i = 0;

                foreach (var item in Children(node))
                {
                    i++;
                    var marker = type == "bulletList" ? "• " : $"{Num(i)}. ";
                    var first = true;

                    foreach (var child in Children(item))
                    {
                        AppendBlock(body, child, run, linked, divisor, first ? marker : null, indent + 1);
                        first = false;
                    }
                }

                break;

            case "horizontalRule":
                body.Append(Para("—————"));
                break;

            default:
                foreach (var child in Children(node))
                {
                    AppendBlock(body, child, run, linked, divisor, prefix, indent);
                }

                break;
        }
    }

    private static Paragraph InlinePara(
        JsonElement node, FsRunDetailDto run, string? linked, decimal divisor, string? prefix, int indent, bool bold = false, int size = 24)
    {
        var p = new Paragraph(ParaProps(center: false, indent));

        if (prefix is not null)
        {
            p.Append(TextRun(prefix, bold, false, false, false, size));
        }

        foreach (var child in Children(node))
        {
            var type = child.TryGetProperty("type", out var t) ? t.GetString() : null;

            if (type == "hardBreak")
            {
                p.Append(new Run(new Break()));
                continue;
            }

            if (type != "text" || !child.TryGetProperty("text", out var textEl))
            {
                continue;
            }

            var text = FsNarrativeVariables.Replace(textEl.GetString() ?? string.Empty, run.Statements, linked, divisor);
            bool b = bold, i = false, u = false, s = false;

            if (child.TryGetProperty("marks", out var marks))
            {
                foreach (var m in marks.EnumerateArray())
                {
                    switch (m.TryGetProperty("type", out var mt) ? mt.GetString() : null)
                    {
                        case "bold": b = true; break;
                        case "italic": i = true; break;
                        case "underline": u = true; break;
                        case "strike": s = true; break;
                    }
                }
            }

            p.Append(TextRun(text, b, i, u, s, size));
        }

        return p;
    }

    private static IEnumerable<JsonElement> Children(JsonElement node)
        => node.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.Array ? c.EnumerateArray() : [];

    private static Table NoteTable(FsRunStatementDto s, bool hasPrior, string year, decimal divisor)
    {
        var table = new Table(new TableProperties(
            new BiDiVisual(),
            new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 2 })));

        var prior = (int.Parse(year, CultureInfo.InvariantCulture) - 1).ToString(CultureInfo.InvariantCulture);
        var header = new List<string> { "شرح", Digits(year) };

        if (hasPrior)
        {
            header.Add(Digits(prior));
        }

        table.Append(Row(header, bold: true));

        foreach (var row in s.Rows.Where(x => x.RowType != FsRowType.Blank))
        {
            var isValue = row.RowType is FsRowType.Account or FsRowType.Formula or FsRowType.External;
            var sign = row.NormalBalance == FsNormalBalance.Credit ? -1m : 1m;
            var cells = new List<string>
            {
                (row.RowType == FsRowType.Header && row.NoteRef is not null ? Digits(row.NoteRef) + ". " : string.Empty) + (row.TitleFa ?? string.Empty),
                isValue ? Amount(row.AmountCur * sign, divisor) : string.Empty,
            };

            if (hasPrior)
            {
                cells.Add(isValue ? Amount(row.AmountPrv * sign, divisor) : string.Empty);
            }

            table.Append(Row(cells, bold: row.RowType == FsRowType.Header || row.Format.Bold));
        }

        return table;
    }

    private static TableRow Row(IEnumerable<string> cells, bool bold)
    {
        var tr = new TableRow();

        foreach (var (text, index) in cells.Select((c, i) => (c, i)))
        {
            var p = new Paragraph(new ParagraphProperties(
                new BiDi(),
                new Justification { Val = index == 0 ? JustificationValues.Left : JustificationValues.Right }));
            p.Append(TextRun(text, bold, false, false, false, 22));
            tr.Append(new TableCell(p));
        }

        return tr;
    }

    private static Paragraph Heading(string text, int level)
        => Para(text, bold: true, size: level == 1 ? 30 : 26);

    private static Paragraph Para(string text, bool bold = false, int size = 24, bool center = false)
    {
        var p = new Paragraph(ParaProps(center, 0));
        p.Append(TextRun(text, bold, false, false, false, size));
        return p;
    }

    private static ParagraphProperties ParaProps(bool center, int indent)
    {
        var props = new ParagraphProperties(new BiDi());

        if (center)
        {
            props.Append(new Justification { Val = JustificationValues.Center });
        }

        if (indent > 0)
        {
            props.Append(new Indentation { Start = (indent * 360).ToString(CultureInfo.InvariantCulture) });
        }

        return props;
    }

    private static Run TextRun(string text, bool bold, bool italic, bool underline, bool strike, int size)
    {
        var props = new RunProperties(
            new RunFonts { Ascii = Font, HighAnsi = Font, ComplexScript = Font },
            new RightToLeftText(),
            new FontSize { Val = size.ToString(CultureInfo.InvariantCulture) },
            new FontSizeComplexScript { Val = size.ToString(CultureInfo.InvariantCulture) });

        if (bold)
        {
            props.Append(new Bold(), new BoldComplexScript());
        }

        if (italic)
        {
            props.Append(new Italic(), new ItalicComplexScript());
        }

        if (underline)
        {
            props.Append(new Underline { Val = UnderlineValues.Single });
        }

        if (strike)
        {
            props.Append(new Strike());
        }

        return new Run(props, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static string Amount(decimal? value, decimal divisor) => value is null ? string.Empty : Format(value.Value, divisor);

    private static string Format(decimal value, decimal divisor)
    {
        var scaled = Math.Round(value / divisor, MidpointRounding.AwayFromZero);

        if (scaled == 0)
        {
            return "—";
        }

        var text = Math.Abs(scaled).ToString("#,0", Fa);
        return scaled < 0 ? $"({text})" : text;
    }

    private static string Num(int n) => n.ToString(Fa);

    private static string Digits(string s)
        => new(s.Select(c => c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c).ToArray());

    private static string Period(string year, int toMonth)
    {
        string[] months = ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"];
        var month = months[Math.Clamp(toMonth, 1, 12) - 1];
        return toMonth == 12 ? $"سال مالی منتهی به پایان اسفند {Digits(year)}" : $"دورهٔ منتهی به پایان {month} {Digits(year)}";
    }

    private static decimal NoteOrder(string? noteNo)
        => decimal.TryParse(noteNo?.Split('،', ',')[0].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : decimal.MaxValue;
}
