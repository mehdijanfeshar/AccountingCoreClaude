using System.Globalization;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Engine;

/// <summary>یک صورت یا یادداشت در ورودی شماره‌گذاری.</summary>
public sealed record FsNumberingStatement(
    string TemplateCode,
    bool IsNote,
    int TemplateOrderNo,
    string? ParentTemplateCode,
    string? ParentRowCode,
    IReadOnlyList<FsNumberingRow> Rows);

public sealed record FsNumberingRow(string Code, string? ParentCode, int OrderNo, FsRowType RowType, string? NoteRef);

/// <summary>نتیجهٔ شماره‌گذاری یادداشت‌های یک اجرا.</summary>
public sealed class FsNoteNumberingResult
{
    /// <summary>کد قالب یادداشت ⇒ شماره (مثلاً «5»).</summary>
    public Dictionary<string, string> NoteNumbers { get; } = new(StringComparer.Ordinal);

    /// <summary>یادداشت‌ها به ترتیب شماره — ترتیب ارائه بعد از صورت‌ها.</summary>
    public List<string> NoteOrder { get; } = new();

    /// <summary>(قالب، ردیف) ⇒ متن ستون «یادداشت»: شمارهٔ یادداشت‌های وصل به ردیف صورت، یا زیرشمارهٔ «عنوان» یادداشت.</summary>
    public Dictionary<(string Stmt, string Row), string> RowNoteRefs { get; } = new();

    /// <summary>یادداشت ⇒ (صورت، ردیف) والدی که واقعاً پیدا شد؛ یادداشت بی‌والد اینجا نیست.</summary>
    public Dictionary<string, (string Stmt, string Row)> ResolvedParents { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// شماره‌گذاری خودکار یادداشت‌ها و زیر‌یادداشت‌ها (بخش ۴۵-ج، سند منبع §۹ «شماره‌گذاری خودکار بر اساس
/// ترتیب ارائه»). خالص:
/// <list type="number">
/// <item>صورت‌ها به ترتیب ارائه، ردیف‌ها به ترتیب؛ هر یادداشتِ وصل به آن ردیف (به ترتیب قالب) شمارهٔ
/// بعدی را از <c>startNo</c> می‌گیرد. چند یادداشت روی یک ردیف ⇒ «5، 6».</item>
/// <item>یادداشت بی‌والد یا با والد ناموجود در اجرا، در انتها شماره می‌گیرد.</item>
/// <item>داخل یادداشت، هر ردیف «عنوان» زیر‌یادداشت است: سطح اول «5-1»، «5-2»؛ عنوانِ زیر عنوان «5-1-1».
/// اگر عنوان شمارهٔ دستی (<c>NOTE_REF</c>) داشته باشد همان می‌ماند و شمارنده جلو نمی‌رود.</item>
/// </list>
/// یادداشت فقط به ردیف «صورت» وصل می‌شود، نه یادداشت دیگر (زیر‌یادداشت = عنوان داخل یادداشت).
/// </summary>
public static class FsNoteNumbering
{
    public static FsNoteNumberingResult Number(IReadOnlyList<FsNumberingStatement> items, int startNo)
    {
        var result = new FsNoteNumberingResult();
        var statements = items.Where(s => !s.IsNote).ToList();
        var notes = items.Where(s => s.IsNote).ToList();

        var rowExists = statements
            .SelectMany(s => s.Rows.Select(r => (s.TemplateCode, r.Code)))
            .ToHashSet();

        var linked = notes
            .Where(n => n.ParentTemplateCode is not null && n.ParentRowCode is not null
                && rowExists.Contains((n.ParentTemplateCode, n.ParentRowCode)))
            .GroupBy(n => (n.ParentTemplateCode!, n.ParentRowCode!))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(n => n.TemplateOrderNo).ThenBy(n => n.TemplateCode, StringComparer.Ordinal).ToList());

        var next = startNo;

        foreach (var s in statements)
        {
            foreach (var row in s.Rows.OrderBy(r => r.OrderNo))
            {
                if (!linked.TryGetValue((s.TemplateCode, row.Code), out var rowNotes))
                {
                    continue;
                }

                var numbers = new List<string>();

                foreach (var n in rowNotes)
                {
                    var no = (next++).ToString(CultureInfo.InvariantCulture);
                    result.NoteNumbers[n.TemplateCode] = no;
                    result.NoteOrder.Add(n.TemplateCode);
                    result.ResolvedParents[n.TemplateCode] = (s.TemplateCode, row.Code);
                    numbers.Add(no);
                }

                result.RowNoteRefs[(s.TemplateCode, row.Code)] = string.Join("، ", numbers);
            }
        }

        foreach (var n in notes.Where(n => !result.NoteNumbers.ContainsKey(n.TemplateCode))
                     .OrderBy(n => n.TemplateOrderNo).ThenBy(n => n.TemplateCode, StringComparer.Ordinal))
        {
            result.NoteNumbers[n.TemplateCode] = (next++).ToString(CultureInfo.InvariantCulture);
            result.NoteOrder.Add(n.TemplateCode);
        }

        foreach (var n in notes)
        {
            NumberHeaders(n, result.NoteNumbers[n.TemplateCode], result);
        }

        return result;
    }

    private static void NumberHeaders(FsNumberingStatement note, string noteNo, FsNoteNumberingResult result)
    {
        var headers = note.Rows.Where(r => r.RowType == FsRowType.Header).ToDictionary(r => r.Code, StringComparer.Ordinal);
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        var numbers = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in note.Rows.OrderBy(r => r.OrderNo).Where(r => r.RowType == FsRowType.Header))
        {
            if (!string.IsNullOrWhiteSpace(row.NoteRef))
            {
                numbers[row.Code] = row.NoteRef.Trim();
                continue;
            }

            // والدِ «عنوان» شمارهٔ پایه است؛ عنوان بدون والد عنوان ⇒ زیرِ خود یادداشت.
            var parentKey = row.ParentCode is { } p && headers.ContainsKey(p) && numbers.ContainsKey(p) ? p : string.Empty;
            var baseNo = parentKey.Length == 0 ? noteNo : numbers[parentKey];
            var k = counters.GetValueOrDefault(parentKey) + 1;
            counters[parentKey] = k;
            numbers[row.Code] = $"{baseNo}-{k.ToString(CultureInfo.InvariantCulture)}";
            result.RowNoteRefs[(note.TemplateCode, row.Code)] = numbers[row.Code];
        }
    }
}
