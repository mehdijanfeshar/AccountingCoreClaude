using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Accounting.Domain.OperationTemplates;
using MediatR;

namespace Accounting.Application.OperationTemplates.History;

// ═══════════════════ جستجوی معنایی سوابق (RAG، DDL 089) ═══════════════════
// هر الگو یک بردار از «شرح‌های نمونه + کلمه‌های کلیدی» می‌گیرد (مدل embedding داخلی روی Ollama، مثل bge-m3).
// تطبیق جمله = امتیاز کلمه‌ای فعلی + نزدیکی معنایی (ادغام رتبه‌ها). خاموش یا خطا ⇒ همان روش کلمه‌ای.
// اوراکل 19 نوع VECTOR ندارد: بردار در BLOB، جستجو در حافظه (چند هزار الگو ⇒ چند میلی‌ثانیه).

/// <summary>تنظیمات <c>Assistant:Embedding</c>. Provider = None (پیش‌فرض) یا Ollama.</summary>
public sealed class HistoryEmbeddingOptions
{
    public const string Section = "Assistant:Embedding";

    public string Provider { get; set; } = "None";
    public string Model { get; set; } = "bge-m3";
    public string BaseUrl { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 60;
    public string KeepAlive { get; set; } = "30m";

    /// <summary>چند متن در هر درخواست ساخت بردار.</summary>
    public int BatchSize { get; set; } = 16;

    /// <summary>کمترین شباهت کسینوسی برای اینکه الگو «از نظر معنی نزدیک» حساب شود.</summary>
    public double MinSimilarity { get; set; } = 0.5;

    /// <summary>
    /// فقط الگوهایی که حداکثر این‌قدر از بهترین شباهت فاصله دارند. آزمایش bge-m3 (۲۰۲۶-۱۰-۲۹): جملهٔ کوتاه فارسی،
    /// الگوی درست ۰٫۶–۰٫۸ و نزدیک‌ترین الگوی نامربوط تا ۰٫۶ ⇒ فاصلهٔ نسبی از آستانهٔ مطلق مطمئن‌تر است.
    /// </summary>
    public double RelativeMargin { get; set; } = 0.06;

    /// <summary>چند نزدیک‌ترین الگو به‌عنوان کاندیدای معنایی.</summary>
    public int SemanticTake { get; set; } = 30;

    /// <summary>کشف الگو: آستانهٔ میانگین شباهت برای یکی کردن دو گروه شرح (پیوند میانگین).</summary>
    public double MeaningSimilarity { get; set; } = 0.6;

    /// <summary>کشف الگو: کمترین سهم یک معنا از نمونه‌شرح‌های الگو، به درصد.</summary>
    public double MeaningMinShare { get; set; } = 10;

    /// <summary>سقف زمان ساخت بردار جملهٔ کاربر؛ بیشتر شد ⇒ فقط روش کلمه‌ای.</summary>
    public int QueryTimeoutSeconds { get; set; } = 8;

    public bool IsEnabled => !string.Equals(Provider?.Trim(), "None", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(Provider);
}

/// <summary>مدل embedding. فقط سرویس داخلی؛ متن به بیرون فرستاده نمی‌شود.</summary>
public interface IHistoryEmbedder
{
    bool IsEnabled { get; }
    string ModelName { get; }

    /// <summary>یک بردار به ازای هر متن، به همان ترتیب.</summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct);
}

public sealed record HistoryPatternText(Guid Id, string? KeywordsJson, string? SampleDescsJson);

public interface IHistoryVectorStore
{
    /// <summary>متن همهٔ الگوهای فعلی (بدون CLOB).</summary>
    Task<IReadOnlyList<HistoryPatternText>> PatternTextsAsync(CancellationToken ct);

    /// <summary>هش متن بردارهای موجود همین مدل، به تفکیک الگو.</summary>
    Task<IReadOnlyDictionary<Guid, string?>> HashesAsync(string model, CancellationToken ct);

    /// <summary>بردارهای همین مدل که الگویشان هنوز هست.</summary>
    Task<IReadOnlyList<HistoryPatternVector>> LoadAsync(string model, CancellationToken ct);

    /// <summary>درج یا جایگزینی (SaveChanges خودش؛ جدول محاسبه‌شده است).</summary>
    Task UpsertAsync(IReadOnlyList<HistoryPatternVector> vectors, CancellationToken ct);

    /// <summary>حذف بردار الگوهای ازبین‌رفته یا مدل دیگر.</summary>
    Task<int> DeleteStaleAsync(string model, CancellationToken ct);
}

/// <summary>جدول 089 ساخته نشده ⇒ جستجوی معنایی خاموش می‌ماند (کلمه‌ای کار می‌کند).</summary>
public sealed class HistoryVectorsUnavailableException() : Exception("TB_OP_HISTORY_PATTERN_VEC is missing (DDL 089).");

public static class HistoryEmbeddingText
{
    private const int MaxKeywords = 12;

    /// <summary>متن ورودی مدل: شرح‌های نمونه سپس کلمه‌های پرتکرار. بدون متن ⇒ null.</summary>
    public static string? For(HistoryPatternText p)
    {
        var parts = HistoryJson.Strings(p.SampleDescsJson).Select(HistoryText.Normalize).Where(s => s.Length > 0).ToList();
        var words = HistoryJson.Keywords(p.KeywordsJson).OrderByDescending(k => k.Count).Take(MaxKeywords).Select(k => k.Word).ToList();
        if (words.Count > 0) parts.Add(string.Join(' ', words));
        return parts.Count == 0 ? null : string.Join("\n", parts);
    }

    public static string Hash(string model, string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(model + "\n" + text)));
}

public static class HistoryVectorMath
{
    public static float[] Normalize(float[] v)
    {
        double sum = 0;
        foreach (var x in v) sum += (double)x * x;
        var len = Math.Sqrt(sum);
        if (len == 0) return v;
        var r = new float[v.Length];
        for (var i = 0; i < v.Length; i++) r[i] = (float)(v[i] / len);
        return r;
    }

    public static double Dot(float[] a, float[] b)
    {
        double s = 0;
        var n = Math.Min(a.Length, b.Length);
        for (var i = 0; i < n; i++) s += (double)a[i] * b[i];
        return s;
    }

    public static byte[] ToBytes(float[] v)
    {
        var bytes = new byte[v.Length * 4];
        for (var i = 0; i < v.Length; i++) BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), v[i]);
        return bytes;
    }

    public static float[] FromBytes(byte[] bytes)
    {
        var v = new float[bytes.Length / 4];
        for (var i = 0; i < v.Length; i++) v[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * 4));
        return v;
    }
}

/// <summary>
/// نمایهٔ بردارها در حافظه (Singleton). بار اول از DB خوانده می‌شود؛ پس از هر همگام‌سازی باطل و دوباره خوانده می‌شود.
/// </summary>
public sealed class HistoryVectorIndex
{
    private sealed record Snapshot(string Model, Guid[] Ids, float[][] Vectors);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile Snapshot? _snapshot;

    public int Count => _snapshot?.Ids.Length ?? 0;

    public void Invalidate() => _snapshot = null;

    public async Task EnsureLoadedAsync(IHistoryVectorStore store, string model, CancellationToken ct)
    {
        if (_snapshot is { } s && s.Model == model) return;
        await _gate.WaitAsync(ct);
        try
        {
            if (_snapshot is { } again && again.Model == model) return;
            var rows = (await store.LoadAsync(model, ct)).Where(r => r.Vector is { Length: > 0 }).ToList();
            _snapshot = new Snapshot(model, rows.Select(r => r.PatternId).ToArray(),
                rows.Select(r => HistoryVectorMath.FromBytes(r.Vector!)).ToArray());
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// نزدیک‌ترین الگوها به بردار نرمال‌شدهٔ <paramref name="query"/>: شباهت ≥ <paramref name="minSimilarity"/> و
    /// حداکثر <paramref name="relativeMargin"/> کمتر از بهترین (null ⇒ بدون قید نسبی).
    /// </summary>
    public IReadOnlyList<(Guid Id, double Similarity)> Search(float[] query, int take, double minSimilarity, double? relativeMargin = null)
    {
        if (_snapshot is not { } s || s.Ids.Length == 0) return [];
        var hits = new List<(Guid Id, double Similarity)>();
        for (var i = 0; i < s.Ids.Length; i++)
        {
            var sim = HistoryVectorMath.Dot(query, s.Vectors[i]);
            if (sim >= minSimilarity) hits.Add((s.Ids[i], sim));
        }
        if (hits.Count == 0) return [];
        var floor = relativeMargin is { } m ? hits.Max(h => h.Similarity) - m : double.MinValue;
        return hits.Where(h => h.Similarity >= floor).OrderByDescending(h => h.Similarity).Take(take).ToList();
    }
}

/// <summary>درخواست همگام‌سازی بردارها (پس از بازسازی) برای سرویس پس‌زمینه.</summary>
public interface IHistoryEmbeddingSignal
{
    void Request();
}

public sealed record HistoryEmbeddingSyncResult(int Patterns, int Embedded, int Skipped, int Deleted, long ElapsedMs);

/// <summary>
/// همگام‌سازی بردارها با جدول الگوها: فقط الگوهای تازه یا متن‌عوض‌شده بردار می‌گیرند (هش متن)، بقیه دست‌نخورده.
/// بسته‌به‌بسته ذخیره می‌شود تا قطع شدن کار، ساخته‌شده‌ها را از دست ندهد.
/// </summary>
public sealed class HistoryEmbeddingSync
{
    private readonly IHistoryVectorStore _store;
    private readonly IHistoryEmbedder _embedder;
    private readonly HistoryVectorIndex _index;
    private readonly HistoryEmbeddingOptions _options;

    public HistoryEmbeddingSync(IHistoryVectorStore store, IHistoryEmbedder embedder, HistoryVectorIndex index, HistoryEmbeddingOptions options)
    { _store = store; _embedder = embedder; _index = index; _options = options; }

    public async Task<HistoryEmbeddingSyncResult> RunAsync(CancellationToken ct)
    {
        var started = DateTime.UtcNow;
        var model = _embedder.ModelName;
        var deleted = await _store.DeleteStaleAsync(model, ct);
        var existing = await _store.HashesAsync(model, ct);
        var todo = new List<(Guid Id, string Text, string Hash)>();
        var patterns = await _store.PatternTextsAsync(ct);
        foreach (var p in patterns)
        {
            if (HistoryEmbeddingText.For(p) is not { } text) continue;
            var hash = HistoryEmbeddingText.Hash(model, text);
            if (existing.TryGetValue(p.Id, out var old) && old == hash) continue;
            todo.Add((p.Id, text, hash));
        }

        var embedded = 0;
        foreach (var batch in todo.Chunk(Math.Clamp(_options.BatchSize, 1, 64)))
        {
            var vectors = await _embedder.EmbedAsync(batch.Select(b => b.Text).ToList(), ct);
            if (vectors.Count != batch.Length) throw new InvalidOperationException("تعداد بردارهای برگشتی با ورودی برابر نیست.");
            var now = DateTime.UtcNow;
            await _store.UpsertAsync(batch.Select((b, i) =>
            {
                var v = HistoryVectorMath.Normalize(vectors[i]);
                return new HistoryPatternVector
                {
                    PatternId = b.Id, Model = model, TextHash = b.Hash, Dim = v.Length,
                    Vector = HistoryVectorMath.ToBytes(v), CreatedAtUtc = now,
                };
            }).ToList(), ct);
            embedded += batch.Length;
        }

        if (embedded > 0 || deleted > 0) _index.Invalidate();
        return new HistoryEmbeddingSyncResult(patterns.Count, embedded, patterns.Count - todo.Count, deleted,
            (long)(DateTime.UtcNow - started).TotalMilliseconds);
    }
}

/// <summary>وضعیت جستجوی معنایی برای صفحهٔ کشف الگو.</summary>
public sealed record HistorySemanticStatusDto(bool Enabled, string? Model, int Vectors, bool Running, DateTime? LastRunUtc,
    HistoryEmbeddingSyncResult? LastResult, string? LastError, HistoryMeaningSyncResult? LastMeanings = null);

/// <summary>آخرین اجرای همگام‌سازی (Singleton؛ سرویس پس‌زمینه پر می‌کند).</summary>
public sealed class HistoryEmbeddingState
{
    public volatile bool Running;
    public DateTime? LastRunUtc { get; set; }
    public HistoryEmbeddingSyncResult? LastResult { get; set; }
    public HistoryMeaningSyncResult? LastMeanings { get; set; }
    public string? LastError { get; set; }
}

public sealed record GetHistorySemanticStatusQuery : IRequest<HistorySemanticStatusDto>;

public sealed class GetHistorySemanticStatusHandler : IRequestHandler<GetHistorySemanticStatusQuery, HistorySemanticStatusDto>
{
    private readonly IHistoryEmbedder? _embedder;
    private readonly HistoryVectorIndex? _index;
    private readonly IHistoryVectorStore? _store;
    private readonly HistoryEmbeddingState? _state;

    public GetHistorySemanticStatusHandler(IHistoryEmbedder? embedder = null, HistoryVectorIndex? index = null,
        IHistoryVectorStore? store = null, HistoryEmbeddingState? state = null)
    { _embedder = embedder; _index = index; _store = store; _state = state; }

    public async Task<HistorySemanticStatusDto> Handle(GetHistorySemanticStatusQuery q, CancellationToken ct)
    {
        if (_embedder is not { IsEnabled: true } || _index is null || _store is null)
            return new HistorySemanticStatusDto(false, null, 0, false, null, null, null);
        try
        {
            await _index.EnsureLoadedAsync(_store, _embedder.ModelName, ct);
        }
        catch (HistoryVectorsUnavailableException)
        {
            return new HistorySemanticStatusDto(true, _embedder.ModelName, 0, false, _state?.LastRunUtc, _state?.LastResult,
                "جدول بردارها ساخته نشده؛ اسکریپت ۰۸۹ را اجرا کنید.");
        }
        return new HistorySemanticStatusDto(true, _embedder.ModelName, _index.Count, _state?.Running ?? false,
            _state?.LastRunUtc, _state?.LastResult, _state?.LastError, _state?.LastMeanings);
    }
}
