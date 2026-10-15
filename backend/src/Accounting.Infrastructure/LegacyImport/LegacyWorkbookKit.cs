using Accounting.Application.Common.Interfaces;
using ClosedXML.Excel;

namespace Accounting.Infrastructure.LegacyImport;

/// <summary>جمع‌آوری پیام‌های بررسی/اجرا برای برگرداندن به کاربر.</summary>
internal sealed class ImportReport
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Info { get; } = new();
    /// <summary>مشکلات قابل‌رفع (انتقال بخش‌بندی‌شده)؛ مشکل باز هم مانع ثبت است.</summary>
    public IssueBook? Issues { get; set; }
    public bool Ok => Errors.Count == 0 && (Issues?.Open ?? 0) == 0;
    public LegacyImportReportDto ToDto(bool committed) =>
        new(Ok, committed, Errors, Warnings, Info, Issues?.SectionsDto(), Issues?.IssuesDto());
}

/// <summary>قالب مشترک کاربرگ‌ها: ستون زرد = ورودی کارشناس، آبی = پیشنهاد، ستون کنترل سبز/قرمز.</summary>
internal sealed class LegacyWorkbookKit
{
    public XLWorkbook Wb { get; } = new();
    public static readonly XLColor Yellow = XLColor.FromHtml("#FFF2CC");
    public static readonly XLColor Suggest = XLColor.FromHtml("#EAF1F8");
    private static readonly XLColor Head = XLColor.FromHtml("#1F4E78");

    private readonly IXLWorksheet _guide;
    private int _g = 1;

    public LegacyWorkbookKit()
    {
        Wb.Style.Font.FontName = "Tahoma";
        Wb.Style.Font.FontSize = 10;
        _guide = Wb.Worksheets.Add("راهنما");
        _guide.RightToLeft = true;
        _guide.Column(1).Width = 34; _guide.Column(2).Width = 22; _guide.Column(3).Width = 22; _guide.Column(4).Width = 60;
    }

    public IXLWorksheet Sheet(string name, string[] headers, int[] widths)
    {
        var ws = Wb.Worksheets.Add(name);
        ws.RightToLeft = true;
        for (var i = 0; i < headers.Length; i++)
        {
            var c = ws.Cell(1, i + 1);
            c.Value = headers[i];
            c.Style.Font.Bold = true; c.Style.Font.FontColor = XLColor.White; c.Style.Fill.BackgroundColor = Head;
            c.Style.Alignment.WrapText = true;
            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Column(i + 1).Width = widths[i];
        }
        ws.Row(1).Height = 45;
        ws.SheetView.FreezeRows(1);
        return ws;
    }

    public static void Finish(IXLWorksheet ws, int lastRow, int lastCol, int[] inputCols, int[] suggCols, int[] moneyCols, int controlCol)
    {
        if (lastRow < 2) lastRow = 2;
        ws.Range(1, 1, lastRow, lastCol).SetAutoFilter();
        foreach (var c in inputCols)
        {
            ws.Range(2, c, lastRow, c).Style.Fill.BackgroundColor = Yellow;
            ws.Cell(1, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#BF8F00");
        }
        foreach (var c in suggCols) ws.Range(2, c, lastRow, c).Style.Fill.BackgroundColor = Suggest;
        foreach (var c in moneyCols) ws.Range(2, c, lastRow, c).Style.NumberFormat.Format = "#,##0;(#,##0);-";
        ws.Range(2, 1, lastRow, lastCol).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        ws.Range(2, 1, lastRow, lastCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        if (controlCol > 0)
        {
            var r = ws.Range(2, controlCol, lastRow, controlCol);
            r.AddConditionalFormat().WhenEquals("\"OK\"").Fill.SetBackgroundColor(XLColor.FromHtml("#C6EFCE")).Font.SetFontColor(XLColor.FromHtml("#006100"));
            r.AddConditionalFormat().WhenNotBlank().Fill.SetBackgroundColor(XLColor.FromHtml("#FFC7CE")).Font.SetFontColor(XLColor.FromHtml("#9C0006"));
        }
    }

    public void G(string a, string? b = null, bool bold = false, double size = 10)
    {
        _guide.Cell(_g, 1).Value = a;
        _guide.Cell(_g, 1).Style.Font.Bold = bold;
        _guide.Cell(_g, 1).Style.Font.FontSize = size;
        if (b != null)
        {
            _guide.Cell(_g, 2).Value = b;
            _guide.Range(_g, 2, _g, 4).Merge().Style.Alignment.WrapText = true;
            if (b.Length > 70) _guide.Row(_g).Height = 15 * Math.Ceiling(b.Length / 95.0);
        }
        _g++;
    }

    public void Gap() => _g++;

    public void Legend()
    {
        G("راهنمای رنگ‌ها", null, true, 11);
        Swatch("ستون زرد (✎)", Yellow, "فقط این ستون‌ها را پر یا اصلاح کنید");
        Swatch("ستون آبی کم‌رنگ", Suggest, "پیشنهاد خودکار — فقط برای راهنمایی، تغییر ندهید");
        Swatch("کنترل: OK", XLColor.FromHtml("#C6EFCE"), "ردیف کامل و درست است");
        Swatch("کنترل: متن قرمز", XLColor.FromHtml("#FFC7CE"), "ردیف نیاز به اقدام دارد (متن، مشکل را می‌گوید)");
        Gap();
    }

    private void Swatch(string label, XLColor color, string text)
    {
        _guide.Cell(_g, 1).Value = label;
        _guide.Cell(_g, 1).Style.Fill.BackgroundColor = color;
        _guide.Cell(_g, 2).Value = text;
        _guide.Range(_g, 2, _g, 4).Merge();
        _g++;
    }

    public void Progress(string label, string formula)
    {
        _guide.Cell(_g, 1).Value = label;
        _guide.Cell(_g, 2).FormulaA1 = formula;
        _guide.Cell(_g, 2).Style.Font.Bold = true;
        _g++;
    }

    public byte[] ToBytes()
    {
        _guide.Range(1, 1, _g, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        _guide.SetTabActive();
        using var ms = new MemoryStream();
        Wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ---------- خواندن ----------

    /// <summary>مقدار نهایی سلول (فرمول‌ها ارزیابی می‌شوند)؛ خطا یا خالی = "".</summary>
    public static string V(IXLCell c)
    {
        try
        {
            var v = c.Value;
            return v.IsBlank || v.IsError ? "" : v.ToString(System.Globalization.CultureInfo.InvariantCulture).Trim();
        }
        catch { return ""; }
    }

    public static IEnumerable<IXLRow> DataRows(IXLWorksheet ws)
        => ws.RowsUsed().Where(r => r.RowNumber() > 1 && V(r.Cell(1)) != "");

    public static XLWorkbook Open(byte[] bytes) => new(new MemoryStream(bytes));

    public static IXLWorksheet SheetOrThrow(XLWorkbook wb, string name)
        => wb.Worksheets.TryGetWorksheet(name, out var ws) ? ws
            : throw new InvalidDataException($"برگهٔ «{name}» در فایل نیست؛ فایلی را که از همین صفحه دریافت کرده‌اید بارگذاری کنید.");
}
