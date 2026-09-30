using System.Globalization;
using System.Text;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.ValueObjects;
using ClosedXML.Excel;

namespace Accounting.Infrastructure.Reporting;

/// <summary>
/// خروجی Excel یک اجرای صورت‌های مالی (بخش ۴۵-د، سند منبع §۱۶) با ClosedXML. هر صورت/یادداشت یک برگهٔ
/// راست‌به‌چپ. ستون‌ها:
/// <list type="bullet">
/// <item>A کد ردیف، B شرح، C یادداشت؛ D/E مبلغ نمایشی دوره جاری/سال قبل (ماهیت بستانکار قرینه).</item>
/// <item>F/G مبلغ با علامت حسابداری (بدهکار مثبت) — <b>پنهان</b>؛ ردیف فرمول در F/G فرمول Excel معادل فرمول
/// قالب است (SUM بازه، ارجاع سلول، <c>STMT</c> = ارجاع بین برگه‌ها، PRIOR = ستون G) و D/E فقط <c>=F</c> یا
/// <c>=-F</c>. پس حسابرس هر جمع را تا اقلامش در خود Excel دنبال می‌کند.</item>
/// </list>
/// مبالغ به ریال‌اند (بدون تبدیل واحد). فرمولی که قابل ترجمه نباشد با عدد Snapshot جایگزین می‌شود.
/// </summary>
public sealed class FsExcelExporter : IFsExcelExporter
{
    private const int FirstDataRow = 7;
    private const string NumberFormat = "#,##0;(#,##0);\"—\"";

    public byte[] Export(FsRunDetailDto run)
    {
        using var workbook = new XLWorkbook { RightToLeft = true };

        var sheets = new List<(FsRunStatementDto Statement, string SheetName, Dictionary<string, int> RowIndex)>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in run.Statements)
        {
            var name = UniqueSheetName(s.IsNote ? $"یادداشت {s.NoteNo}" : s.TitleFa, usedNames);
            var index = s.Rows
                .OrderBy(r => r.OrderNo)
                .Select((r, i) => (r.Code, Row: FirstDataRow + i))
                .ToDictionary(x => x.Code, x => x.Row, StringComparer.Ordinal);
            sheets.Add((s, name, index));
        }

        var bySheetCode = sheets.ToDictionary(x => x.Statement.TemplateCode, x => (x.SheetName, x.RowIndex), StringComparer.Ordinal);

        foreach (var (statement, sheetName, rowIndex) in sheets)
        {
            WriteSheet(workbook.Worksheets.Add(sheetName), run, statement, rowIndex, bySheetCode);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSheet(
        IXLWorksheet ws,
        FsRunDetailDto run,
        FsRunStatementDto statement,
        IReadOnlyDictionary<string, int> rowIndex,
        IReadOnlyDictionary<string, (string SheetName, Dictionary<string, int> RowIndex)> sheets)
    {
        ws.RightToLeft = true;
        var r = run.Run;

        ws.Cell(1, 2).Value = r.VahedName ?? r.VahedCode;
        ws.Cell(2, 2).Value = statement.IsNote && statement.NoteNo is not null ? $"{statement.NoteNo}. {statement.TitleFa}" : statement.TitleFa;
        ws.Cell(3, 2).Value = r.ToMonth == 12
            ? $"سال مالی منتهی به پایان اسفند {r.Year}"
            : $"دورهٔ {r.ToMonth} ماهه منتهی به پایان ماه {r.ToMonth} سال {r.Year}";
        ws.Cell(4, 2).Value = $"(مبالغ به ریال) — اجرای شمارهٔ {r.RunNo}" + (r.UsesDraft ? " — آزمایشی" : string.Empty);
        ws.Range(1, 2, 2, 2).Style.Font.Bold = true;

        ws.Cell(6, 1).Value = "کد";
        ws.Cell(6, 2).Value = "شرح";
        ws.Cell(6, 3).Value = "یادداشت";
        ws.Cell(6, 4).Value = r.Year;
        ws.Cell(6, 5).Value = r.HasPrior ? (int.Parse(r.Year, CultureInfo.InvariantCulture) - 1).ToString(CultureInfo.InvariantCulture) : string.Empty;
        ws.Cell(6, 6).Value = "داخلی جاری";
        ws.Cell(6, 7).Value = "داخلی قبل";
        ws.Range(6, 1, 6, 7).Style.Font.Bold = true;
        ws.Range(6, 1, 6, 7).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        foreach (var row in statement.Rows.OrderBy(x => x.OrderNo))
        {
            var i = rowIndex[row.Code];
            var isValue = row.RowType is FsRowType.Account or FsRowType.Formula or FsRowType.External;
            var isHeader = row.RowType == FsRowType.Header;

            ws.Cell(i, 1).Value = row.Code;
            ws.Cell(i, 2).Value = (statement.IsNote && isHeader && row.NoteRef is not null ? row.NoteRef + ". " : string.Empty) + (row.TitleFa ?? string.Empty);
            ws.Cell(i, 2).Style.Alignment.Indent = Math.Clamp(row.Format.Indent, 0, 10);

            if (!(statement.IsNote && isHeader) && row.NoteRef is not null)
            {
                ws.Cell(i, 3).Value = row.NoteRef;
            }

            if (row.Format.Bold || isHeader)
            {
                ws.Range(i, 1, i, 5).Style.Font.Bold = true;
            }

            if (!isValue)
            {
                continue;
            }

            SetInternal(ws.Cell(i, 6), row, row.AmountCur, "F", statement, rowIndex, sheets);

            if (r.HasPrior)
            {
                SetInternal(ws.Cell(i, 7), row, row.AmountPrv, "G", statement, rowIndex, sheets);
            }

            var sign = row.NormalBalance == FsNormalBalance.Credit ? "-" : string.Empty;
            ws.Cell(i, 4).FormulaA1 = $"{sign}F{i}";

            if (r.HasPrior)
            {
                ws.Cell(i, 5).FormulaA1 = $"{sign}G{i}";
            }

            var range = ws.Range(i, 4, i, 5);
            range.Style.NumberFormat.Format = NumberFormat;
            range.Style.Border.TopBorder = Border(row.Format.TopBorder);
            range.Style.Border.BottomBorder = Border(row.Format.BottomBorder);
        }

        ws.Range(FirstDataRow, 6, FirstDataRow + rowIndex.Count, 7).Style.NumberFormat.Format = NumberFormat;
        ws.Column(1).Width = 8;
        ws.Column(2).Width = 55;
        ws.Column(3).Width = 10;
        ws.Column(4).Width = 20;
        ws.Column(5).Width = 20;
        ws.Column(6).Hide();
        ws.Column(7).Hide();

        if (!r.HasPrior)
        {
            ws.Column(5).Hide();
        }

        if (!statement.IsNote)
        {
            ws.Cell(FirstDataRow + rowIndex.Count + 2, 2).Value = "یادداشت‌های توضیحی، بخش جدایی‌ناپذیر صورت‌های مالی است.";
        }

        ws.SheetView.FreezeRows(6);
    }

    /// <summary>ستون داخلی: عدد برای ردیف حساب/دستی؛ فرمول Excel برای ردیف فرمول (یا عدد اگر ترجمه نشد).</summary>
    private static void SetInternal(
        IXLCell cell,
        FsRunRowDto row,
        decimal? amount,
        string column,
        FsRunStatementDto statement,
        IReadOnlyDictionary<string, int> rowIndex,
        IReadOnlyDictionary<string, (string SheetName, Dictionary<string, int> RowIndex)> sheets)
    {
        if (row.RowType == FsRowType.Formula
            && FsFormula.TryParse(row.Formula, out var formula, out _)
            && TryTranslate(formula!.Root, column, statement.TemplateCode, rowIndex, sheets, out var excel))
        {
            cell.FormulaA1 = excel;
            return;
        }

        if (amount is { } a)
        {
            cell.Value = a;
        }
    }

    private static bool TryTranslate(
        FsExpr e,
        string column,
        string templateCode,
        IReadOnlyDictionary<string, int> rowIndex,
        IReadOnlyDictionary<string, (string SheetName, Dictionary<string, int> RowIndex)> sheets,
        out string result)
    {
        var sb = new StringBuilder();
        var ok = Translate(e, column, templateCode, rowIndex, sheets, sb);
        result = sb.ToString();
        return ok;
    }

    private static bool Translate(
        FsExpr e,
        string column,
        string templateCode,
        IReadOnlyDictionary<string, int> rowIndex,
        IReadOnlyDictionary<string, (string SheetName, Dictionary<string, int> RowIndex)> sheets,
        StringBuilder sb)
    {
        bool Sub(FsExpr x) => Translate(x, column, templateCode, rowIndex, sheets, sb);

        switch (e)
        {
            case FsNumberExpr n:
                sb.Append(n.Value.ToString(CultureInfo.InvariantCulture));
                return true;

            case FsRowRefExpr rr:
            {
                var col = rr.ColumnKey switch
                {
                    null => column,
                    FsColumns.Current => "F",
                    FsColumns.Prior => "G",
                    _ => null,
                };

                if (col is null || !rowIndex.TryGetValue(rr.RowCode, out var i))
                {
                    sb.Append('0');
                    return true;
                }

                sb.Append(col).Append(i.ToString(CultureInfo.InvariantCulture));
                return true;
            }

            case FsPriorExpr p:
                if (!rowIndex.TryGetValue(p.RowCode, out var pi))
                {
                    sb.Append('0');
                    return true;
                }

                sb.Append('G').Append(pi.ToString(CultureInfo.InvariantCulture));
                return true;

            case FsStatementRefExpr s:
                if (!sheets.TryGetValue(s.TemplateCode, out var other) || !other.RowIndex.TryGetValue(s.RowCode, out var si))
                {
                    return false;
                }

                sb.Append('\'').Append(other.SheetName.Replace("'", "''")).Append("'!")
                    .Append(column).Append(si.ToString(CultureInfo.InvariantCulture));
                return true;

            case FsSumRangeExpr sum:
                if (!rowIndex.TryGetValue(sum.FromRowCode, out var a) || !rowIndex.TryGetValue(sum.ToRowCode, out var b))
                {
                    return false;
                }

                sb.Append("SUM(").Append(column).Append(a.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(column).Append(b.ToString(CultureInfo.InvariantCulture)).Append(')');
                return true;

            case FsUnaryExpr u:
                sb.Append("-(");
                if (!Sub(u.Operand))
                {
                    return false;
                }

                sb.Append(')');
                return true;

            case FsBinaryExpr bin when bin.Op == '/':
            {
                // موتور تقسیم بر صفر را صفر می‌گیرد؛ Excel همین را با IF.
                var denominator = new StringBuilder();

                if (!Translate(bin.Right, column, templateCode, rowIndex, sheets, denominator))
                {
                    return false;
                }

                sb.Append("IF((").Append(denominator).Append(")=0,0,(");

                if (!Sub(bin.Left))
                {
                    return false;
                }

                sb.Append(")/(").Append(denominator).Append("))");
                return true;
            }

            case FsBinaryExpr bin:
                sb.Append('(');

                if (!Sub(bin.Left))
                {
                    return false;
                }

                sb.Append(bin.Op);

                if (!Sub(bin.Right))
                {
                    return false;
                }

                sb.Append(')');
                return true;

            case FsFunctionExpr { Name: "ABS" } f:
                sb.Append("ABS(");
                if (!Sub(f.Args[0]))
                {
                    return false;
                }

                sb.Append(')');
                return true;

            case FsFunctionExpr { Name: "ROUND" } f:
                sb.Append("ROUND(");
                if (!Sub(f.Args[0]))
                {
                    return false;
                }

                sb.Append(',').Append(((FsNumberExpr)f.Args[1]).Value.ToString(CultureInfo.InvariantCulture)).Append(')');
                return true;

            case FsIfExpr iff:
                sb.Append("IF(");
                if (!Sub(iff.CompareLeft))
                {
                    return false;
                }

                sb.Append(iff.CompareOp);

                if (!Sub(iff.CompareRight))
                {
                    return false;
                }

                sb.Append(',');

                if (!Sub(iff.Then))
                {
                    return false;
                }

                sb.Append(',');

                if (!Sub(iff.Else))
                {
                    return false;
                }

                sb.Append(')');
                return true;

            default:
                return false;
        }
    }

    private static XLBorderStyleValues Border(FsBorder border) => border switch
    {
        FsBorder.Single => XLBorderStyleValues.Thin,
        FsBorder.Double => XLBorderStyleValues.Double,
        _ => XLBorderStyleValues.None,
    };

    /// <summary>نام برگه: حداکثر ۳۱ نویسه، بدون <c>: \ / ? * [ ]</c>، یکتا.</summary>
    private static string UniqueSheetName(string title, HashSet<string> used)
    {
        var clean = new string(title.Where(c => c is not (':' or '\\' or '/' or '?' or '*' or '[' or ']')).ToArray()).Trim();

        if (clean.Length == 0)
        {
            clean = "برگه";
        }

        if (clean.Length > 28)
        {
            clean = clean[..28];
        }

        var name = clean;

        for (var n = 2; !used.Add(name); n++)
        {
            name = $"{clean} {n}";
        }

        return name;
    }
}
