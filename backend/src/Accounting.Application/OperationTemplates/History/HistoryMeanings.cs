using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.OperationTemplates.History;

// ═══════════════════ معناهای هر الگوی سابقه (گروه‌بندی معنایی شرح‌ها، DDL 089) ═══════════════════
// کشف الگو سندها را با «مجموعهٔ حساب‌ها» گروه می‌کند؛ یک مجموعه ممکن است چند کار متفاوت باشد
// (پذیرایی و ایاب‌وذهاب هر دو «هزینهٔ متفرقه / بانک»). نمونه‌شرح‌های هر الگو با مدل embedding داخلی
// بردار می‌گیرند و با پیوند میانگین (average linkage) گروه می‌شوند؛ هر گروه بزرگ یک «معنا» است.
// آزمایش bge-m3 (۲۰۲۶-۱۰-۲۹): شرح‌های هم‌معنا ۰٫۵۷–۰٫۶۸ و ناهم‌معنا تا ۰٫۶۵ ⇒ آستانهٔ تک‌جفت بی‌اعتبار است؛
// میانگین گروه‌ها با آستانهٔ ۰٫۶ نمونه را درست جدا کرد.

public interface IHistoryMeaningStore
{
    /// <summary>آخرین زمان ساخت معناها؛ جدول خالی ⇒ null. جدول نیست ⇒ <see cref="HistoryVectorsUnavailableException"/>.</summary>
    Task<DateTime?> LastBuiltAsync(CancellationToken ct);

    /// <summary>آخرین «بازسازی» الگوها و بازهٔ سال‌شان؛ جدول خالی ⇒ null.</summary>
    Task<(DateTime RebuiltAt, int FromYear, int ToYear)?> PatternsStampAsync(CancellationToken ct);

    /// <summary>کل محتوا را جایگزین می‌کند (تکه‌تکه).</summary>
    Task ReplaceAllAsync(IReadOnlyList<HistoryPatternMeaning> meanings, CancellationToken ct);

    /// <summary>معناهای الگوها؛ جدول 089 نیست ⇒ خالی (صفحه بدون معنا کار می‌کند).</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<HistoryPatternMeaning>>> GetForAsync(IReadOnlyCollection<Guid> patternIds, CancellationToken ct);
}

public sealed record HistoryMeaningDto(int Index, string Title, decimal Share, IReadOnlyList<string> Samples, IReadOnlyList<KeywordDto> Keywords);

public sealed record HistoryMeaningSyncResult(int Patterns, int Texts, int MultiMeaning, long ElapsedMs, bool Skipped);

/// <summary>یک شرح یکتا (پس از پوشاندن نام و رقم) با تعداد تکرارش در نمونه.</summary>
public sealed record MeaningItem(string Text, int Weight, float[] Vector);

public static class HistoryMeaningClusterer
{
    /// <summary>
    /// گروه‌بندی سلسله‌مراتبی با پیوند میانگین وزنی: هر بار دو گروهی که میانگین شباهت جفت‌هایشان بیشترین است
    /// یکی می‌شوند، تا وقتی بیشترین میانگین از <paramref name="threshold"/> کمتر شود. بردارها نرمال‌شده‌اند.
    /// خروجی: اندیس اعضای هر گروه، بزرگ‌ترین گروه (وزن) اول.
    /// </summary>
    public static List<List<int>> Cluster(IReadOnlyList<MeaningItem> items, double threshold)
    {
        var n = items.Count;
        var sim = new double[n, n];
        for (var i = 0; i < n; i++)
            for (var j = i + 1; j < n; j++)
                sim[i, j] = sim[j, i] = HistoryVectorMath.Dot(items[i].Vector, items[j].Vector);

        var groups = Enumerable.Range(0, n).Select(i => new List<int> { i }).ToList();
        while (groups.Count > 1)
        {
            var (bestA, bestB, best) = (-1, -1, double.MinValue);
            for (var a = 0; a < groups.Count; a++)
                for (var b = a + 1; b < groups.Count; b++)
                {
                    var s = Linkage(groups[a], groups[b], items, sim);
                    if (s > best) (bestA, bestB, best) = (a, b, s);
                }
            if (best < threshold) break;
            groups[bestA].AddRange(groups[bestB]);
            groups.RemoveAt(bestB);
        }
        return groups.OrderByDescending(g => g.Sum(i => items[i].Weight)).ToList();
    }

    private static double Linkage(List<int> a, List<int> b, IReadOnlyList<MeaningItem> items, double[,] sim)
    {
        double sum = 0, weight = 0;
        foreach (var i in a)
            foreach (var j in b)
            {
                double w = items[i].Weight * items[j].Weight;
                sum += w * sim[i, j];
                weight += w;
            }
        return weight == 0 ? 0 : sum / weight;
    }

    /// <summary>
    /// معناهای یک الگو از شرح‌هایش. گروهی «معنا» است که دست‌کم <paramref name="minWeight"/> سند و
    /// <paramref name="minShare"/> درصد نمونه را داشته باشد؛ کمتر از دو معنا ⇒ یک ردیف کلی (IDX = 0).
    /// </summary>
    public static List<HistoryPatternMeaning> Meanings(Guid patternId, IReadOnlyList<MeaningItem> items, double threshold,
        int minWeight, decimal minShare, DateTime now)
    {
        if (items.Count == 0) return [];
        var total = items.Sum(i => i.Weight);
        var groups = items.Count == 1 ? [[0]] : Cluster(items, threshold);
        var big = groups.Where(g =>
        {
            var w = g.Sum(i => items[i].Weight);
            return w >= minWeight && 100m * w / total >= minShare;
        }).ToList();
        if (big.Count < 2) big = [Enumerable.Range(0, items.Count).ToList()];

        return big.Select((g, index) =>
        {
            var members = g.OrderByDescending(i => items[i].Weight).ThenBy(i => items[i].Text, StringComparer.Ordinal).ToList();
            var weight = g.Sum(i => items[i].Weight);
            var tokens = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var i in g)
                foreach (var t in HistoryText.DistinctTokens(items[i].Text))
                    tokens[t] = tokens.GetValueOrDefault(t) + items[i].Weight;
            return new HistoryPatternMeaning
            {
                PatternId = patternId,
                Index = index,
                Title = items[Medoid(g, items)].Text,
                Share = Math.Round(100m * weight / total, 1, MidpointRounding.AwayFromZero),
                SampleCount = weight,
                SamplesJson = HistoryJson.Serialize(members.Take(3).Select(i => items[i].Text).ToList()),
                KeywordsJson = HistoryJson.KeywordsFit(HistoryText.Top(tokens, 8), 900),
                CreatedAtUtc = now,
            };
        }).ToList();
    }

    /// <summary>عضوی که بیشترین شباهت وزنی به کل گروه (خودش هم) را دارد ⇒ نماینده؛ پرتکرار در تساوی.</summary>
    private static int Medoid(List<int> g, IReadOnlyList<MeaningItem> items)
    {
        if (g.Count == 1) return g[0];
        return g.OrderByDescending(i => g.Sum(j => items[j].Weight * (i == j ? 1 : HistoryVectorMath.Dot(items[i].Vector, items[j].Vector))))
            .ThenByDescending(i => items[i].Weight).First();
    }
}

/// <summary>
/// ساخت معناهای همهٔ الگوها: فقط وقتی الگوها پس از آخرین ساخت «بازسازی» شده‌اند (یا هرگز ساخته نشده).
/// نمونه‌شرح‌ها دوباره از اوراکل خوانده می‌شوند (همان پرس‌وجوی بازسازی)؛ کار سنگین و پس‌زمینه است.
/// </summary>
public sealed class HistoryMeaningSync
{
    private const int GlobalSample = 100;
    private const int MaxDistinct = 30;

    private readonly IHistorySourceReader _source;
    private readonly IHistoryMeaningStore _store;
    private readonly IHistoryVectorStore _patterns;
    private readonly IHistoryEmbedder _embedder;
    private readonly HistoryEmbeddingOptions _options;

    public HistoryMeaningSync(IHistorySourceReader source, IHistoryMeaningStore store, IHistoryVectorStore patterns,
        IHistoryEmbedder embedder, HistoryEmbeddingOptions options)
    { _source = source; _store = store; _patterns = patterns; _embedder = embedder; _options = options; }

    public async Task<HistoryMeaningSyncResult> RunAsync(CancellationToken ct)
    {
        var started = DateTime.UtcNow;
        if (await _store.PatternsStampAsync(ct) is not { } stamp) return new(0, 0, 0, 0, Skipped: true);
        if (await _store.LastBuiltAsync(ct) is { } last && last >= stamp.RebuiltAt) return new(0, 0, 0, 0, Skipped: true);

        var live = (await _patterns.PatternTextsAsync(ct)).Select(p => p.Id).ToHashSet();

        // ۱. شرح‌های نمونهٔ هر الگو ⇒ شرح یکتای پوشانده با تعداد
        var texts = new Dictionary<Guid, Dictionary<string, int>>();
        await foreach (var s in _source.ReadSamplesAsync(stamp.FromYear, stamp.ToYear, HistoryDefaults.MinPatternVouchers, GlobalSample, 0, ct))
        {
            if (s.GlobalRank > GlobalSample) continue;
            var id = RebuildHistoryPatternsHandler.StableId(HistoryJson.SignatureOf(RebuildHistoryPatternsHandler.ParseSignature(s.Sig)));
            if (!live.Contains(id)) continue;
            // بدون نام طرف حساب، رقم و نام ماه: «مالیات حقوق بهمن» و «… اسفند» یک شرح‌اند
            var text = HistoryText.WithoutMonths(HistoryText.MaskForOtherUnits(string.IsNullOrWhiteSpace(s.HeadDesc) ? s.LineDescs : s.HeadDesc, 80));
            if (text.Length == 0) continue;
            if (!texts.TryGetValue(id, out var counts)) texts[id] = counts = new(StringComparer.Ordinal);
            counts[text] = counts.GetValueOrDefault(text) + 1;
        }

        // ۲. بردار شرح‌ها (فقط پرتکرارترین‌ها؛ شرح یکتای مشترک بین الگوها یک بار)
        var picked = texts.ToDictionary(kv => kv.Key, kv => kv.Value.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal)
            .Take(MaxDistinct).ToList());
        var distinct = picked.Values.SelectMany(v => v).Select(x => x.Key).Distinct(StringComparer.Ordinal).ToList();
        var vectors = new Dictionary<string, float[]>(StringComparer.Ordinal);
        foreach (var batch in distinct.Chunk(Math.Clamp(_options.BatchSize, 1, 64)))
        {
            var got = await _embedder.EmbedAsync(batch, ct);
            if (got.Count != batch.Length) throw new InvalidOperationException("تعداد بردارهای برگشتی با ورودی برابر نیست.");
            for (var i = 0; i < batch.Length; i++) vectors[batch[i]] = HistoryVectorMath.Normalize(got[i]);
        }

        // ۳. گروه‌بندی هر الگو
        var now = DateTime.UtcNow;
        var rows = new List<HistoryPatternMeaning>();
        var multi = 0;
        foreach (var (id, list) in picked)
        {
            var items = list.Select(x => new MeaningItem(x.Key, x.Value, vectors[x.Key])).ToList();
            var meanings = HistoryMeaningClusterer.Meanings(id, items, _options.MeaningSimilarity, HistoryDefaults.MinPatternVouchers,
                (decimal)_options.MeaningMinShare, now);
            if (meanings.Count > 1) multi++;
            rows.AddRange(meanings);
        }

        await _store.ReplaceAllAsync(rows, ct);
        return new(picked.Count, distinct.Count, multi, (long)(DateTime.UtcNow - started).TotalMilliseconds, Skipped: false);
    }
}
