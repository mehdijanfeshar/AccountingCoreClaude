using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Accounting.Application.OperationTemplates.History;
using Accounting.Domain.OperationTemplates;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.OperationTemplates;

// جستجوی معنایی سوابق (RAG، DDL 089) — مدل embedding روی Ollama داخلی، بردارها در BLOB، همگام‌سازی در پس‌زمینه.

public static class HistoryEmbeddingDi
{
    public const string HttpClientName = "history-embedding";

    /// <summary>بخش <c>Assistant:Embedding</c>؛ Provider=None (پیش‌فرض) ⇒ تطبیق جمله فقط کلمه‌ای.</summary>
    public static IServiceCollection AddHistoryEmbedding(this IServiceCollection s, IConfiguration configuration)
    {
        var options = configuration.GetSection(HistoryEmbeddingOptions.Section).Get<HistoryEmbeddingOptions>() ?? new HistoryEmbeddingOptions();
        s.AddSingleton(options);
        s.AddSingleton<HistoryVectorIndex>();
        s.AddSingleton<HistoryEmbeddingState>();
        s.AddScoped<HistoryVectorRepository>();
        s.AddScoped<IHistoryVectorStore>(sp => sp.GetRequiredService<HistoryVectorRepository>());
        s.AddScoped<IHistoryMeaningStore>(sp => sp.GetRequiredService<HistoryVectorRepository>());
        if (!options.IsEnabled) return s;

        if (!string.Equals(options.Provider.Trim(), "Ollama", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Assistant:Embedding:Provider «{options.Provider}» پشتیبانی نمی‌شود (None یا Ollama).");

        s.AddHttpClient(HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 600)));
        s.AddSingleton<IHistoryEmbedder, OllamaHistoryEmbedder>();
        s.AddScoped<HistoryEmbeddingSync>();
        s.AddScoped<HistoryMeaningSync>();
        s.AddSingleton<HistoryEmbeddingWorker>();
        s.AddSingleton<IHistoryEmbeddingSignal>(sp => sp.GetRequiredService<HistoryEmbeddingWorker>());
        s.AddHostedService(sp => sp.GetRequiredService<HistoryEmbeddingWorker>());
        return s;
    }
}

/// <summary>Ollama <c>POST /api/embed</c>.</summary>
public sealed class OllamaHistoryEmbedder : IHistoryEmbedder
{
    private readonly IHttpClientFactory _http;
    private readonly HistoryEmbeddingOptions _options;

    public OllamaHistoryEmbedder(IHttpClientFactory http, HistoryEmbeddingOptions options)
    {
        _http = http;
        _options = options;
    }

    public bool IsEnabled => true;
    public string ModelName => string.IsNullOrWhiteSpace(_options.Model) ? "bge-m3" : _options.Model.Trim();

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        if (texts.Count == 0) return [];
        var input = new JsonArray();
        foreach (var t in texts) input.Add(t);
        var body = new JsonObject
        {
            ["model"] = ModelName,
            ["input"] = input,
            ["truncate"] = true,
            ["keep_alive"] = string.IsNullOrWhiteSpace(_options.KeepAlive) ? "30m" : _options.KeepAlive,
        };

        var baseUrl = (string.IsNullOrWhiteSpace(_options.BaseUrl) ? "http://localhost:11434" : _options.BaseUrl).TrimEnd('/');
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await _http.CreateClient(HistoryEmbeddingDi.HttpClientName).PostAsync($"{baseUrl}/api/embed", content, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("embeddings").EnumerateArray()
            .Select(e => e.EnumerateArray().Select(x => x.GetSingle()).ToArray())
            .ToList();
    }
}

/// <summary>ORA-00942 روی جدول بردارها ⇒ <see cref="HistoryVectorsUnavailableException"/>؛ روی جدول الگوها ⇒ ۵۰۳ معمول سوابق.</summary>
public sealed class HistoryVectorRepository : IHistoryVectorStore, IHistoryMeaningStore
{
    private const int InChunk = 500;
    private readonly LegacyDbContext _db;
    public HistoryVectorRepository(LegacyDbContext db) => _db = db;

    public async Task<IReadOnlyList<HistoryPatternText>> PatternTextsAsync(CancellationToken ct)
    {
        try
        {
            return await _db.Set<HistoryPattern>().AsNoTracking()
                .Select(p => new HistoryPatternText(p.Id, p.KeywordsJson, p.SampleDescsJson)).ToListAsync(ct);
        }
        catch (DbException ex) when (IsMissingTable(ex))
        {
            throw new HistoryPatternsUnavailableException(tableMissing: true);
        }
    }

    public Task<IReadOnlyDictionary<Guid, string?>> HashesAsync(string model, CancellationToken ct) => Guard(async () =>
        (IReadOnlyDictionary<Guid, string?>)await _db.Set<HistoryPatternVector>().AsNoTracking().Where(v => v.Model == model)
            .Select(v => new { v.PatternId, v.TextHash }).ToDictionaryAsync(v => v.PatternId, v => v.TextHash, ct));

    public Task<IReadOnlyList<HistoryPatternVector>> LoadAsync(string model, CancellationToken ct) => Guard(async () =>
        (IReadOnlyList<HistoryPatternVector>)await _db.Set<HistoryPatternVector>().AsNoTracking().Where(v => v.Model == model).ToListAsync(ct));

    public Task UpsertAsync(IReadOnlyList<HistoryPatternVector> vectors, CancellationToken ct) => Guard(async () =>
    {
        foreach (var chunk in vectors.Chunk(InChunk))
        {
            var ids = chunk.Select(v => v.PatternId).ToList();
            await _db.Set<HistoryPatternVector>().Where(v => ids.Contains(v.PatternId)).ExecuteDeleteAsync(ct);
            _db.Set<HistoryPatternVector>().AddRange(chunk);
            await _db.SaveChangesAsync(ct);
            _db.ChangeTracker.Clear();
        }
        return 0;
    });

    public Task<int> DeleteStaleAsync(string model, CancellationToken ct) => Guard(async () =>
    {
        var live = _db.Set<HistoryPattern>().Select(p => p.Id);
        return await _db.Set<HistoryPatternVector>()
            .Where(v => v.Model != model || !live.Contains(v.PatternId))
            .ExecuteDeleteAsync(ct);
    });

    // ─────────── معناها (IHistoryMeaningStore) ───────────

    public Task<DateTime?> LastBuiltAsync(CancellationToken ct) => Guard(() =>
        _db.Set<HistoryPatternMeaning>().AsNoTracking().MaxAsync(m => m.CreatedAtUtc, ct));

    public async Task<(DateTime RebuiltAt, int FromYear, int ToYear)?> PatternsStampAsync(CancellationToken ct)
    {
        try
        {
            var patterns = _db.Set<HistoryPattern>().AsNoTracking();
            var at = await patterns.MaxAsync(p => p.RebuiltAtUtc, ct);
            var from = await patterns.MinAsync(p => p.FromYear, ct);
            var to = await patterns.MaxAsync(p => p.ToYear, ct);
            return at is { } a && from is { } f && to is { } t ? (a, f, t) : null;
        }
        catch (DbException ex) when (IsMissingTable(ex))
        {
            throw new HistoryPatternsUnavailableException(tableMissing: true);
        }
    }

    public Task ReplaceAllAsync(IReadOnlyList<HistoryPatternMeaning> meanings, CancellationToken ct) => Guard(async () =>
    {
        await _db.Set<HistoryPatternMeaning>().ExecuteDeleteAsync(ct);
        foreach (var chunk in meanings.Chunk(1000))
        {
            _db.Set<HistoryPatternMeaning>().AddRange(chunk);
            await _db.SaveChangesAsync(ct);
            _db.ChangeTracker.Clear();
        }
        return 0;
    });

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<HistoryPatternMeaning>>> GetForAsync(IReadOnlyCollection<Guid> patternIds, CancellationToken ct)
    {
        var result = new Dictionary<Guid, IReadOnlyList<HistoryPatternMeaning>>();
        try
        {
            foreach (var chunk in patternIds.Distinct().Chunk(InChunk))
            {
                var part = chunk.ToList();
                var rows = await _db.Set<HistoryPatternMeaning>().AsNoTracking().Where(m => part.Contains(m.PatternId)).ToListAsync(ct);
                foreach (var g in rows.GroupBy(m => m.PatternId))
                    result[g.Key] = g.OrderBy(m => m.Index).ToList();
            }
        }
        catch (DbException ex) when (IsMissingTable(ex))
        {
            // 089 اجرا نشده ⇒ بدون معنا
        }
        return result;
    }

    private static bool IsMissingTable(DbException ex) => ex.Message.Contains("ORA-00942", StringComparison.Ordinal);

    private static async Task<T> Guard<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (DbException ex) when (IsMissingTable(ex))
        {
            throw new HistoryVectorsUnavailableException();
        }
    }
}

/// <summary>
/// همگام‌سازی بردارها: چند ثانیه پس از شروع برنامه، پس از هر «بازسازی» (سیگنال) و هر ۶ ساعت.
/// خطا فقط ثبت می‌شود؛ تطبیق جمله در این مدت کلمه‌ای کار می‌کند.
/// </summary>
public sealed class HistoryEmbeddingWorker : BackgroundService, IHistoryEmbeddingSignal
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan Period = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopes;
    private readonly HistoryEmbeddingState _state;
    private readonly ILogger<HistoryEmbeddingWorker> _logger;
    private readonly Channel<bool> _signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public HistoryEmbeddingWorker(IServiceScopeFactory scopes, HistoryEmbeddingState state, ILogger<HistoryEmbeddingWorker> logger)
    { _scopes = scopes; _state = state; _logger = logger; }

    public void Request() => _signal.Writer.TryWrite(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                wait.CancelAfter(Period);
                try
                {
                    await _signal.Reader.ReadAsync(wait.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // دورهٔ ۶ ساعته
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        _state.Running = true;
        try
        {
            using var scope = _scopes.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<HistoryEmbeddingSync>().RunAsync(ct);
            _state.LastResult = result;
            _state.LastError = null;
            _logger.LogInformation("History embeddings synced: {Embedded} new, {Skipped} unchanged, {Deleted} removed in {Ms} ms",
                result.Embedded, result.Skipped, result.Deleted, result.ElapsedMs);

            // معناهای کشف الگو — فقط پس از بازسازی تازه (خودش بررسی می‌کند)
            var meanings = await scope.ServiceProvider.GetRequiredService<HistoryMeaningSync>().RunAsync(ct);
            if (!meanings.Skipped)
            {
                _state.LastMeanings = meanings;
                _logger.LogInformation("History meanings built: {Patterns} patterns, {Texts} texts, {Multi} with several meanings in {Ms} ms",
                    meanings.Patterns, meanings.Texts, meanings.MultiMeaning, meanings.ElapsedMs);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (HistoryVectorsUnavailableException)
        {
            _state.LastError = "جدول بردارها ساخته نشده؛ اسکریپت ۰۸۹ را اجرا کنید.";
            _logger.LogWarning("History embeddings skipped: TB_OP_HISTORY_PATTERN_VEC missing (DDL 089)");
        }
        catch (HistoryPatternsUnavailableException)
        {
            _state.LastError = "جدول سوابق ساخته نشده (اسکریپت ۰۸۵).";
        }
        catch (Exception ex)
        {
            _state.LastError = "ساخت بردار ناموفق بود: " + ex.Message;
            _logger.LogWarning(ex, "History embedding sync failed");
        }
        finally
        {
            _state.LastRunUtc = DateTime.UtcNow;
            _state.Running = false;
        }
    }
}
