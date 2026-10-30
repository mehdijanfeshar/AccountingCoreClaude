using System.Net;
using System.Text;
using System.Text.Json;
using Accounting.Application.OperationTemplates.History;
using Accounting.Domain.OperationTemplates;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.OperationTemplates;
using Accounting.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure.Tests.OperationTemplates;

/// <summary>جستجوی معنایی سوابق (DDL 089): درخواست Ollama، مخزن بردار و ثبت DI.</summary>
public sealed class HistoryEmbeddingTests : IDisposable
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly LegacyDbContext _context;

    public HistoryEmbeddingTests()
    {
        _connection.Open();
        _context = new LegacyDbContext(new DbContextOptionsBuilder<LegacyDbContext>().UseSqlite(_connection).Options);
        // کل مدل Legacy روی SQLite ساخته نمی‌شود (Sequence)؛ فقط دو جدول لازم
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE TB_OP_HISTORY_PATTERN (ID TEXT PRIMARY KEY, SIGNATURE TEXT NOT NULL, LINES_JSON TEXT, VOUCHER_COUNT INTEGER,
              UNIT_COUNT INTEGER, YEARS TEXT, FROM_YEAR INTEGER, TO_YEAR INTEGER, MEDIAN_AMOUNT INTEGER, LAST_DATE TEXT, KEYWORDS TEXT,
              SAMPLE_DESCS_JSON TEXT, VAHED_TYPES TEXT, REBUILT_AT TEXT);
            CREATE TABLE TB_OP_HISTORY_PATTERN_VEC (PATTERN_ID TEXT PRIMARY KEY, MODEL TEXT NOT NULL, TEXT_HASH TEXT, DIM INTEGER,
              VECTOR BLOB, CREATEDDATE TEXT);
            CREATE TABLE TB_OP_HISTORY_MEANING (PATTERN_ID TEXT NOT NULL, IDX INTEGER NOT NULL, TITLE TEXT NOT NULL, SHARE NUMERIC,
              SAMPLE_COUNT INTEGER, SAMPLES_JSON TEXT, KEYWORDS TEXT, CREATEDDATE TEXT, PRIMARY KEY (PATTERN_ID, IDX));
            """;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Ollama_PostsBatchToEmbed_AndParsesVectors()
    {
        var handler = new StubHttpMessageHandler((req, _) =>
        {
            Assert.Equal("http://ollama:11434/api/embed", req.RequestUri!.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"model":"bge-m3","embeddings":[[0.1,0.2],[0.3,0.4]]}""", Encoding.UTF8, "application/json"),
            };
        });
        var embedder = new OllamaHistoryEmbedder(new StubHttpClientFactory(handler),
            new HistoryEmbeddingOptions { Provider = "Ollama", BaseUrl = "http://ollama:11434/", Model = "bge-m3" });

        var vectors = await embedder.EmbedAsync(["خرید دارو", "حقوق"], CancellationToken.None);

        Assert.Equal(2, vectors.Count);
        Assert.Equal([0.3f, 0.4f], vectors[1]);
        using var body = JsonDocument.Parse(handler.RequestBodies.Single()!);
        Assert.Equal("bge-m3", body.RootElement.GetProperty("model").GetString());
        Assert.Equal(["خرید دارو", "حقوق"], body.RootElement.GetProperty("input").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Repository_UpsertReplaces_AndDeleteStaleRemovesOrphansAndOtherModels()
    {
        _context.Set<HistoryPattern>().Add(new HistoryPattern { Id = A, Signature = "a", SampleDescsJson = """["خرید دارو"]""" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var repo = new HistoryVectorRepository(_context);

        await repo.UpsertAsync([Vector(A, "m", "h1"), Vector(B, "m", "h1")], CancellationToken.None);
        await repo.UpsertAsync([Vector(A, "m", "h2")], CancellationToken.None);
        Assert.Equal("h2", (await repo.HashesAsync("m", CancellationToken.None))[A]);

        var deleted = await repo.DeleteStaleAsync("m", CancellationToken.None); // B الگو ندارد
        Assert.Equal(1, deleted);
        Assert.Equal([A], (await repo.LoadAsync("m", CancellationToken.None)).Select(v => v.PatternId));

        Assert.Equal(1, await repo.DeleteStaleAsync("other-model", CancellationToken.None));
        Assert.Equal(A, (await repo.PatternTextsAsync(CancellationToken.None)).Single().Id);
    }

    [Fact]
    public async Task Meanings_StampReplaceAndGroupedRead()
    {
        var repo = new HistoryVectorRepository(_context);
        Assert.Null(await repo.PatternsStampAsync(CancellationToken.None));
        Assert.Null(await repo.LastBuiltAsync(CancellationToken.None));

        var rebuilt = new DateTime(2026, 10, 29, 8, 0, 0, DateTimeKind.Utc);
        _context.Set<HistoryPattern>().AddRange(
            new HistoryPattern { Id = A, Signature = "a", FromYear = 1402, ToYear = 1404, RebuiltAtUtc = rebuilt },
            new HistoryPattern { Id = B, Signature = "b", FromYear = 1401, ToYear = 1403, RebuiltAtUtc = rebuilt });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        Assert.Equal((rebuilt, 1401, 1404), await repo.PatternsStampAsync(CancellationToken.None));

        await repo.ReplaceAllAsync([Meaning(A, 1, "ایاب"), Meaning(A, 0, "پذیرایی"), Meaning(B, 0, "حقوق")], CancellationToken.None);
        await repo.ReplaceAllAsync([Meaning(A, 1, "ایاب"), Meaning(A, 0, "پذیرایی")], CancellationToken.None); // جایگزینی کامل

        var got = await repo.GetForAsync([A, B], CancellationToken.None);
        Assert.Equal(["پذیرایی", "ایاب"], got[A].Select(m => m.Title));
        Assert.False(got.ContainsKey(B));
        Assert.Equal(rebuilt.AddHours(1), await repo.LastBuiltAsync(CancellationToken.None));
    }

    private static HistoryPatternMeaning Meaning(Guid id, int index, string title) => new()
    {
        PatternId = id, Index = index, Title = title, Share = 50m, SampleCount = 5, SamplesJson = "[]", KeywordsJson = "[]",
        CreatedAtUtc = new DateTime(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void Di_Disabled_RegistersNoEmbedderOrWorker()
    {
        var sp = Build(new Dictionary<string, string?>());
        Assert.Null(sp.GetService<IHistoryEmbedder>());
        Assert.Null(sp.GetService<IHistoryEmbeddingSignal>());
        Assert.NotNull(sp.GetService<HistoryVectorIndex>());
    }

    [Fact]
    public void Di_Ollama_RegistersEmbedderAndSignal()
    {
        var sp = Build(new Dictionary<string, string?> { ["Assistant:Embedding:Provider"] = "Ollama" });
        Assert.IsType<OllamaHistoryEmbedder>(sp.GetService<IHistoryEmbedder>());
        Assert.IsType<HistoryEmbeddingWorker>(sp.GetService<IHistoryEmbeddingSignal>());
    }

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddHistoryEmbedding(configuration);
        return services.BuildServiceProvider();
    }

    private static HistoryPatternVector Vector(Guid id, string model, string hash) =>
        new() { PatternId = id, Model = model, TextHash = hash, Dim = 1, Vector = HistoryVectorMath.ToBytes([1f]) };
}
