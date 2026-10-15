using System.Text.Json;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Infrastructure.LegacyImport;

/// <summary>تصمیم کاربر روی یک مشکل انتقال (فایل <c>decisions-{vahed}-{year}.json</c> و تاریخچهٔ انتقال).</summary>
internal sealed record IssueDecision(string Option, string? Value, string By, DateTime At);

/// <summary>گزینه‌های رفع مشترک بین بخش‌ها.</summary>
internal static class IssueOptions
{
    public const string Replace = "replace";
    public const string NoLink = "nolink";
    public const string Skip = "skip";
    public const string Draft = "draft";

    public static readonly LegacyIssueOptionDto ReplaceMoein = new(Replace, "جایگزینی با معین دیگر (از فهرست)", "moein");
    public static readonly LegacyIssueOptionDto ReplaceTafsili = new(Replace, "جایگزینی با تفصیلی دیگر (از فهرست)", "tafsili");
}

/// <summary>بخش‌های انتقال اسناد یک سال؛ هر بخش جدول(های) خودش را دارد تا معلوم باشد مشکل از کجاست.</summary>
internal static class Sections
{
    public const string Coding = "coding";
    public const string Receipts = "receipts";
    public const string Incorrents = "incorrents";
    public const string BankCards = "bankcards";
    public const string AttribDefs = "attribdefs";
    public const string Vouchers = "vouchers";
    public const string LineAttribs = "lineattribs";
    public const string Elams = "elams";

    public static readonly (string Key, string Title, string[] Tables)[] Voucher =
    [
        (Coding, "نگاشت کدینگ (معین، تفصیلی، فعالیت/مرکز هزینه)", []),
        (Receipts, "فیش و حواله", ["TB_RECEIP"]),
        (Incorrents, "چک در جریان وصول", ["TB_CHEQUES_INCORRENT"]),
        (BankCards, "کارت بانک", ["TB_BANKCARTDETAIL"]),
        (AttribDefs, "تعریف ویژگی معین", ["TB_ATTRIBFORACCOUNTCODE"]),
        (Vouchers, "اسناد و ردیف‌ها", ["TB_VOUCHERSHEAD", "TB_VOUCHERSDETAIL", "TB_VOUCHERDETAIL_LINK_TAFSILI"]),
        (LineAttribs, "ویژگی ردیف‌ها", ["TB_ATTRIBSINVOUCHERS"]),
        (Elams, "اعلامیه‌ها", ["TB_ELAMHEAD", "TB_ELAMDETAIL", "TB_ELAMDETAIL_LINK_TAFSILI"]),
    ];

    public static string? OfTable(string? table) =>
        Voucher.FirstOrDefault(s => s.Tables.Contains(table, StringComparer.OrdinalIgnoreCase)).Key;
}

/// <summary>
/// دفتر مشکلات یک اجرای انتقال: هر مشکل با شناسهٔ پایدار یک بار ثبت می‌شود (تعداد استفاده و چند نمونه)، تصمیم
/// ذخیره‌شدهٔ کاربر به آن داده می‌شود، و وارد‌کننده نتیجهٔ اعمال تصمیم را برمی‌گرداند (<see cref="Resolve"/> یا
/// <see cref="Reject"/>). ثبت نهایی فقط وقتی مجاز است که هیچ مشکلی باز یا نامعتبر نباشد.
/// </summary>
internal sealed class IssueBook(IReadOnlyDictionary<string, IssueDecision> decisions)
{
    private sealed class Entry
    {
        public required string Id { get; init; }
        public required string Section { get; init; }
        public required string Message { get; init; }
        public string? Hint { get; init; }
        public required LegacyIssueOptionDto[] Options { get; init; }
        public int Uses { get; set; }
        public List<string> Samples { get; } = new();
        public string Status { get; set; } = "open";
        public string? Problem { get; set; }
    }

    private readonly Dictionary<string, Entry> _entries = new();
    private readonly Dictionary<string, int> _records = new();
    private readonly Dictionary<string, List<string>> _notes = new();

    public static IssueBook Empty { get; } = new(new Dictionary<string, IssueDecision>());

    /// <summary>
    /// مشکل را ثبت (یا استفادهٔ دیگری از آن را شمارش) می‌کند و تصمیم کاربر را، اگر یکی از گزینه‌های همین مشکل باشد،
    /// برمی‌گرداند. فراخوان باید تصمیم را اعمال و <see cref="Resolve"/>/<see cref="Reject"/> را صدا بزند.
    /// </summary>
    public IssueDecision? Raise(string id, string section, string message, string? hint, LegacyIssueOptionDto[] options, string? sample = null)
    {
        if (!_entries.TryGetValue(id, out var e))
        {
            e = new Entry { Id = id, Section = section, Message = message, Hint = hint, Options = options };
            _entries[id] = e;
        }
        e.Uses++;
        if (sample != null && e.Samples.Count < 5 && !e.Samples.Contains(sample)) e.Samples.Add(sample);
        if (!decisions.TryGetValue(id, out var d)) return null;
        if (options.All(o => o.Code != d.Option))
        {
            e.Status = "invalid";
            e.Problem = "تصمیم ذخیره‌شده برای این مورد دیگر مجاز نیست؛ دوباره انتخاب کنید.";
            return null;
        }
        return d;
    }

    public void Resolve(string id)
    {
        if (_entries.TryGetValue(id, out var e) && e.Status != "invalid") e.Status = "resolved";
    }

    public void Reject(string id, string problem)
    {
        if (!_entries.TryGetValue(id, out var e)) return;
        e.Status = "invalid";
        e.Problem = problem;
    }

    public void Count(string section, int n = 1) => _records[section] = _records.GetValueOrDefault(section) + n;

    public void Note(string section, string note)
    {
        if (!_notes.TryGetValue(section, out var l)) _notes[section] = l = new();
        l.Add(note);
    }

    public int Open => _entries.Values.Count(e => e.Status != "resolved");

    /// <summary>تصمیم‌هایی که در این اجرا واقعاً اعمال شدند (برای تاریخچهٔ انتقال).</summary>
    public Dictionary<string, IssueDecision> Applied() =>
        _entries.Values.Where(e => e.Status == "resolved" && decisions.ContainsKey(e.Id)).ToDictionary(e => e.Id, e => decisions[e.Id]);

    public IReadOnlyList<LegacySectionDto> SectionsDto() => Sections.Voucher.Select(s =>
    {
        var es = _entries.Values.Where(e => e.Section == s.Key).ToList();
        return new LegacySectionDto(s.Key, s.Title, _records.GetValueOrDefault(s.Key), es.Count(e => e.Status != "resolved"),
            es.Count(e => e.Status == "resolved"), _notes.GetValueOrDefault(s.Key) ?? new());
    }).ToList();

    public IReadOnlyList<LegacyIssueDto> IssuesDto() => _entries.Values
        .OrderBy(e => Array.FindIndex(Sections.Voucher, s => s.Key == e.Section)).ThenBy(e => e.Status == "resolved").ThenBy(e => e.Id, StringComparer.Ordinal)
        .Select(e =>
        {
            var d = decisions.GetValueOrDefault(e.Id);
            return new LegacyIssueDto(e.Id, e.Section, e.Message, e.Hint, e.Uses, e.Samples, e.Options, e.Status,
                d?.Option, d?.Value, d?.By, d?.At, e.Problem);
        }).ToList();

    public static Dictionary<string, IssueDecision> Read(byte[]? json)
    {
        if (json == null) return new();
        try { return JsonSerializer.Deserialize<Dictionary<string, IssueDecision>>(json) ?? new(); }
        catch (JsonException) { return new(); }
    }
}
