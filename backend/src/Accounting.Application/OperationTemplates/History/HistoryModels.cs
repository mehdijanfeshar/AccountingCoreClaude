using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.OperationTemplates.History;

// ═══════════════════ فاز ۶۳ — یادگیری از سوابق (DDL 085) ═══════════════════

public static class HistoryDefaults
{
    /// <summary>حداقل تعداد سند برای نگه‌داشتن یک الگو.</summary>
    public const int MinPatternVouchers = 3;

    public const string Channel = "History";
    public const string CodePrefix = "HISTORY:";

    /// <summary>دلیل ریسک ثابت و اجباری روی هر سند ساخته‌شده از سوابق.</summary>
    public const string RiskReason = "ساخته‌شده از سوابق، نه الگوی ستاد";

    public const string RiskWarning =
        "این سند از روی سندهای مشابه سال‌های قبل ساخته شده، نه الگوی تأییدشدهٔ ستاد؛ حسابدار آن را حتماً بررسی می‌کند.";

    public const string RebuildHint = "جدول سوابق ساخته نشده؛ مدیر ستاد «بازسازی» را بزند";

    public static string TemplateCode(Guid patternId) => CodePrefix + patternId.ToString("D");

    public static bool IsHistoryCode(string? code) => code?.StartsWith(CodePrefix, StringComparison.Ordinal) == true;

    /// <summary>عنوان ردیف بدون الگو در فهرست‌ها: «از سوابق: …».</summary>
    public static string Title(string? text)
    {
        var t = (text ?? "").Trim();
        if (t.Length > 60) t = t[..60].TrimEnd() + "…";
        return "از سوابق: " + t;
    }
}

/// <summary>سرویس سوابق قابل استفاده نیست (جدول خالی یا ساخته‌نشده) — ۵۰۳ با پیام فارسی.</summary>
public sealed class HistoryPatternsUnavailableException : Exception, IPublicProblemException
{
    private readonly string _detail;

    public HistoryPatternsUnavailableException(bool tableMissing = false, string script = "۰۸۵")
        : base("History pattern table is empty or missing.")
    {
        _detail = tableMissing
            ? "ساختار پایگاه داده برای سوابق آماده نیست؛ از مدیر سیستم بخواهید اسکریپت " + script + " را اجرا کند، سپس مدیر ستاد «بازسازی» را بزند."
            : HistoryDefaults.RebuildHint;
    }

    public int StatusCode => 503;
    public string PublicDetail => _detail;
}

// ─────────── ساختار ذخیره‌شده (JSON ستون‌ها) ───────────

public sealed record HistoryLevel(int Level, Guid LevelId, List<Guid> GroupIds);

public sealed record HistoryLine(Guid MoinId, int Side, decimal Share, List<HistoryLevel> Levels);

public sealed record KeywordCount([property: JsonPropertyName("w")] string Word, [property: JsonPropertyName("c")] int Count);

public static class HistoryJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static List<HistoryLine> Lines(string? json) => Parse<List<HistoryLine>>(json) ?? [];
    public static List<KeywordCount> Keywords(string? json) => Parse<List<KeywordCount>>(json) ?? [];
    public static List<string> Strings(string? json) => Parse<List<string>>(json) ?? [];

    /// <summary>JSON فهرست کلمه‌ها در حد ستون؛ از آخر کم می‌شود تا جا شود.</summary>
    public static string? KeywordsFit(List<KeywordCount> keywords, int max)
    {
        var list = keywords.ToList();
        while (list.Count > 0)
        {
            var s = Serialize(list);
            if (s.Length <= max) return s;
            list.RemoveAt(list.Count - 1);
        }
        return null;
    }

    private static T? Parse<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, Options); }
        catch (JsonException) { return null; }
    }

    /// <summary>امضای معین‌ها و سمت‌ها: «id:side|…» مرتب، با حروف کوچک (همان ستون SIGNATURE).</summary>
    public static string SignatureOf(IEnumerable<(Guid Moin, int Side)> lines) =>
        string.Join('|', lines.Select(l => (Id: l.Moin.ToString("D").ToLowerInvariant(), l.Side)).Distinct()
            .OrderBy(l => l.Id, StringComparer.Ordinal).ThenBy(l => l.Side).Select(l => $"{l.Id}:{l.Side}"));
}

// ─────────── خواندن خام از سندها (SQL تجمیعی اوراکل) ───────────

/// <param name="Vahed">null ⇒ ردیف سطح الگو؛ وگرنه ردیف همان واحد.</param>
public sealed record HistoryAggRow(string Sig, string? Vahed, bool IsPattern, int Count, int Units, long Median,
    string? LastDate, int MinYear, int MaxYear);

public sealed record HistoryShareRow(string Sig, Guid Moin, int Side, decimal Share);

public sealed record HistorySampleRow(string Sig, string Vahed, int UnitRank, int GlobalRank, string? HeadDesc, string? LineDescs);

public interface IHistorySourceReader
{
    /// <summary>
    /// سندهای دائم/بررسی‌شدهٔ بازهٔ سال (بدون افتتاحیه/اختتامیه/خودکار/اصلاحی) با ۲ تا ۸ ردیف و معین‌های فعال؛
    /// تجمیع در اوراکل به تفکیک امضا و (امضا، واحد). فقط الگوهای ≥ <paramref name="minCount"/> سند.
    /// </summary>
    Task<IReadOnlyList<HistoryAggRow>> ReadAggregatesAsync(int fromYear, int toYear, int minCount, CancellationToken ct);

    /// <summary>میانهٔ سهم هر (معین، سمت) از جمع سند، به درصد.</summary>
    Task<IReadOnlyList<HistoryShareRow>> ReadSharesAsync(int fromYear, int toYear, int minCount, CancellationToken ct);

    /// <summary>
    /// نمونهٔ سندها برای کلمهٔ کلیدی: <paramref name="globalSample"/> سند اخیر هر الگو و <paramref name="unitSample"/>
    /// سند اخیر هر (الگو، واحد). جریانی خوانده می‌شود تا حافظه پر نشود.
    /// </summary>
    IAsyncEnumerable<HistorySampleRow> ReadSamplesAsync(int fromYear, int toYear, int minCount, int globalSample, int unitSample, CancellationToken ct);
}

// ─────────── ذخیره و جستجو ───────────

public sealed record HistoryPatternFilter(int? FromYear, int? ToYear, string? VahedType, int MinCount, string? Search, int Skip, int Take);

public sealed record HistoryPatternPage(IReadOnlyList<HistoryPattern> Items, int Total);

/// <param name="Patterns">بدون LinesJson (سبک)؛ برای ساخت الگو با <see cref="IHistoryPatternStore.GetManyAsync"/> کامل بخوانید.</param>
/// <param name="OwnUnit">ردیف واحد کاربر برای الگوها (اگر دارد).</param>
public sealed record HistoryCandidates(IReadOnlyList<HistoryPattern> Patterns, IReadOnlyDictionary<Guid, HistoryPatternUnit> OwnUnit);

public interface IHistoryPatternStore
{
    /// <summary>
    /// کل محتوای دو جدول را جایگزین می‌کند. تراکنش را <b>هندلر</b> باز/ببندد (<c>IUnitOfWork</c>)؛ این متد برای حجم بالا
    /// به‌صورت تکه‌تکه ذخیره می‌کند (استثنای آگاهانهٔ قاعدهٔ «یک SaveChanges»؛ جدول محاسبه‌شده است).
    /// </summary>
    Task ReplaceAllAsync(IReadOnlyList<HistoryPattern> patterns, IReadOnlyList<HistoryPatternUnit> units, CancellationToken ct);

    Task<int> CountAsync(CancellationToken ct);

    /// <summary>امضاهای حذف‌شده (DDL 086)؛ اگر جدولش نیست خالی.</summary>
    Task<IReadOnlySet<string>> DismissedSignaturesAsync(CancellationToken ct);

    /// <summary>پاک‌کردن فهرست حذف‌شده‌ها تا بازسازی بعدی همه را دوباره کشف کند (بدون 086 کاری نمی‌کند).</summary>
    Task ClearDismissedAsync(CancellationToken ct);

    /// <summary>الگو و ردیف‌های واحدش را حذف و امضا را حذف‌شده ثبت می‌کند؛ تراکنش با هندلر. جدول 086 نبود ⇒ ۵۰۳.</summary>
    Task DismissAsync(HistoryPattern pattern, string userId, CancellationToken ct);

    /// <summary>همهٔ الگوهای مطابق فیلتر که در <paramref name="coveredSignatures"/> نیستند: ثبت حذف‌شده + حذف، تکه‌تکه (IN ≤ ۵۰۰)؛ تراکنش با هندلر.</summary>
    Task<(int Count, int SkippedCovered)> DismissUncoveredAsync(HistoryPatternFilter filter, IReadOnlySet<string> coveredSignatures, string userId, CancellationToken ct);
    Task<HistoryPatternPage> ListAsync(HistoryPatternFilter filter, CancellationToken ct);
    Task<HistoryPattern?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<HistoryPattern>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    /// <summary>الگوهایی که کلمهٔ کلیدی‌شان (کلی یا در سابقهٔ واحد) یکی از <paramref name="tokens"/> است.</summary>
    Task<HistoryCandidates> FindCandidatesAsync(IReadOnlyCollection<string> tokens, string vahedCode, int take, CancellationToken ct);

    /// <summary>عنوان گروه‌های تفصیلی (<c>TB_TAFSIL_GROUP</c>).</summary>
    Task<IReadOnlyDictionary<Guid, string>> GroupTitlesAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken ct);

    /// <summary>
    /// گروه‌هایی که واحد کاربر دست‌کم یک تفصیلی قابل‌انتخاب در آن‌ها دارد (قاعدهٔ B: لینک به خود واحد، یا «همه»، یا دستهٔ واحد).
    /// </summary>
    Task<IReadOnlySet<Guid>> FillableGroupsAsync(IReadOnlyCollection<Guid> groupIds, string vahedCode, CancellationToken ct);
}

// ─────────── DTOهای خروجی ───────────

public sealed record KeywordDto(string Word, int Count);

public sealed record HistoryDetailLevelDto(int Level, IReadOnlyList<string> GroupTitles);

public sealed record HistoryLineDto(Guid MoinId, string? MoinCode, string? MoinTitle, int Side, decimal Share,
    IReadOnlyList<HistoryDetailLevelDto> DetailLevels);

public sealed record CoveredByTemplateDto(Guid Id, string Title);

public sealed record HistoryPatternDto(
    Guid Id, IReadOnlyList<HistoryLineDto> Lines, int VoucherCount, int UnitCount, string? Years, long MedianAmount,
    string? LastDate, IReadOnlyList<KeywordDto> Keywords, IReadOnlyList<string> SampleDescriptions,
    CoveredByTemplateDto? CoveredByTemplate);

public sealed record HistoryPatternPageDto(IReadOnlyList<HistoryPatternDto> Items, int Total, int Page, int PageSize, bool NeedsRebuild);

public sealed record HistoryRebuildResult(int PatternCount, int VoucherCount, long ElapsedMs);

/// <summary>پیشنهادی که به الگو تبدیل شده حذف نمی‌شود — ۴۰۹.</summary>
public sealed class HistoryPatternCoveredException : Exception, IPublicProblemException
{
    public HistoryPatternCoveredException() : base("History pattern is covered by a template.") { }
    public int StatusCode => 409;
    public string PublicDetail => "این پیشنهاد به الگو تبدیل شده و قابل حذف نیست";
}

public sealed record HistoryDeleteResult(int Count);

public sealed record HistoryBulkDeleteResult(int Count, int SkippedCovered);
