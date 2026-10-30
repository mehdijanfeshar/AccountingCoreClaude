using Accounting.Application.OperationTemplates.History;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.Tests.OperationTemplates;

/// <summary>جستجوی معنایی سوابق (RAG، DDL 089): متن ورودی، ریاضی بردار، نمایه، همگام‌سازی و ادغام رتبه‌ها.</summary>
public sealed class HistorySemanticTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    [Fact]
    public void EmbeddingText_JoinsSamplesThenTopKeywords_Normalized()
    {
        var text = HistoryEmbeddingText.For(new HistoryPatternText(A,
            """[{"w":"برق","c":3},{"w":"قبض","c":9}]""", """["پرداخت قبض برق","هزينه برق"]"""));

        Assert.Equal("پرداخت قبض برق\nهزینه برق\nقبض برق", text);
    }

    [Fact]
    public void EmbeddingText_NoContent_IsNull() =>
        Assert.Null(HistoryEmbeddingText.For(new HistoryPatternText(A, null, "[]")));

    [Fact]
    public void Hash_DependsOnModel() =>
        Assert.NotEqual(HistoryEmbeddingText.Hash("bge-m3", "x"), HistoryEmbeddingText.Hash("other", "x"));

    [Fact]
    public void VectorBytes_RoundTrip()
    {
        float[] v = [0.25f, -1.5f, 3f];
        Assert.Equal(v, HistoryVectorMath.FromBytes(HistoryVectorMath.ToBytes(v)));
    }

    [Fact]
    public void Normalize_GivesUnitLength_AndZeroStaysZero()
    {
        var n = HistoryVectorMath.Normalize([3f, 4f]);
        Assert.Equal(1.0, HistoryVectorMath.Dot(n, n), 5);
        Assert.Equal([0f, 0f], HistoryVectorMath.Normalize([0f, 0f]));
    }

    [Fact]
    public async Task Index_ReturnsNearestAboveThreshold_InOrder()
    {
        var store = new FakeVectorStore();
        store.Vectors.AddRange([Vec(A, [1, 0]), Vec(B, [0.8f, 0.6f]), Vec(C, [0, 1])]);
        var index = new HistoryVectorIndex();
        await index.EnsureLoadedAsync(store, "m", CancellationToken.None);

        var hits = index.Search(HistoryVectorMath.Normalize([1, 0.1f]), take: 5, minSimilarity: 0.5);

        Assert.Equal([A, B], hits.Select(h => h.Id));

        var close = index.Search(HistoryVectorMath.Normalize([1, 0.1f]), take: 5, minSimilarity: 0.5, relativeMargin: 0.06);
        Assert.Equal([A], close.Select(h => h.Id)); // B حدود ۰٫۲ پایین‌تر از بهترین
    }

    [Fact]
    public async Task Index_ReloadsOnlyAfterInvalidate()
    {
        var store = new FakeVectorStore();
        store.Vectors.Add(Vec(A, [1, 0]));
        var index = new HistoryVectorIndex();
        await index.EnsureLoadedAsync(store, "m", CancellationToken.None);
        store.Vectors.Add(Vec(B, [0, 1]));

        await index.EnsureLoadedAsync(store, "m", CancellationToken.None);
        Assert.Equal(1, index.Count);

        index.Invalidate();
        await index.EnsureLoadedAsync(store, "m", CancellationToken.None);
        Assert.Equal(2, index.Count);
    }

    [Fact]
    public async Task Sync_EmbedsOnlyNewOrChanged_AndInvalidatesIndex()
    {
        var store = new FakeVectorStore();
        store.Patterns.AddRange([
            new HistoryPatternText(A, null, """["خرید دارو"]"""),
            new HistoryPatternText(B, null, """["پرداخت حقوق"]"""),
            new HistoryPatternText(C, null, null), // بدون متن ⇒ بردار نمی‌گیرد
        ]);
        var unchanged = HistoryEmbeddingText.Hash("m", HistoryEmbeddingText.For(store.Patterns[0])!);
        store.Vectors.Add(new HistoryPatternVector { PatternId = A, Model = "m", TextHash = unchanged, Vector = HistoryVectorMath.ToBytes([1f]) });
        var embedder = new FakeEmbedder();
        var index = new HistoryVectorIndex();
        await index.EnsureLoadedAsync(store, "m", CancellationToken.None);

        var result = await new HistoryEmbeddingSync(store, embedder, index, new HistoryEmbeddingOptions()).RunAsync(CancellationToken.None);

        Assert.Equal(["پرداخت حقوق"], embedder.Seen);
        Assert.Equal(1, result.Embedded);
        Assert.Equal(B, store.Upserted.Single().PatternId);
        Assert.Equal(1.0, HistoryVectorMath.Dot(HistoryVectorMath.FromBytes(store.Upserted[0].Vector!), HistoryVectorMath.FromBytes(store.Upserted[0].Vector!)), 5);
        Assert.Equal(0, index.Count); // باطل شد
    }

    [Fact]
    public void Fuse_PrefersPatternStrongInBoth_AndKeepsSemanticOnly()
    {
        var a = new HistoryPattern { Id = A, VoucherCount = 5 };
        var b = new HistoryPattern { Id = B, VoucherCount = 50 };
        var c = new HistoryPattern { Id = C, VoucherCount = 1 };

        var order = MatchHistoryPatternsHandler.Fuse([
            (a, null, Lexical: 5, Semantic: 0.7),
            (b, null, Lexical: 9, Semantic: 0),
            (c, null, Lexical: 0, Semantic: 0.9),
        ]).Select(x => x.Pattern.Id).ToList();

        // A در هر دو رتبهٔ ۲ ⇒ اول؛ B (فقط کلمه‌ای) و C (فقط معنایی) هم‌امتیازند ⇒ سند بیشتر جلوتر
        Assert.Equal([A, B, C], order);
    }

    [Fact]
    public void Fuse_DropsPatternsWithNoSignal()
    {
        var a = new HistoryPattern { Id = A };
        Assert.Empty(MatchHistoryPatternsHandler.Fuse([(a, null, 0, 0)]));
    }

    private static HistoryPatternVector Vec(Guid id, float[] v) =>
        new() { PatternId = id, Model = "m", Vector = HistoryVectorMath.ToBytes(HistoryVectorMath.Normalize(v)) };

    private sealed class FakeEmbedder : IHistoryEmbedder
    {
        public List<string> Seen { get; } = [];
        public bool IsEnabled => true;
        public string ModelName => "m";

        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct)
        {
            Seen.AddRange(texts);
            return Task.FromResult<IReadOnlyList<float[]>>(texts.Select(_ => new[] { 3f, 4f }).ToList());
        }
    }

    private sealed class FakeVectorStore : IHistoryVectorStore
    {
        public List<HistoryPatternText> Patterns { get; } = [];
        public List<HistoryPatternVector> Vectors { get; } = [];
        public List<HistoryPatternVector> Upserted { get; } = [];

        public Task<IReadOnlyList<HistoryPatternText>> PatternTextsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<HistoryPatternText>>(Patterns);

        public Task<IReadOnlyDictionary<Guid, string?>> HashesAsync(string model, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string?>>(Vectors.Where(v => v.Model == model).ToDictionary(v => v.PatternId, v => v.TextHash));

        public Task<IReadOnlyList<HistoryPatternVector>> LoadAsync(string model, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<HistoryPatternVector>>(Vectors.Where(v => v.Model == model).ToList());

        public Task UpsertAsync(IReadOnlyList<HistoryPatternVector> vectors, CancellationToken ct)
        {
            Upserted.AddRange(vectors);
            return Task.CompletedTask;
        }

        public Task<int> DeleteStaleAsync(string model, CancellationToken ct) => Task.FromResult(0);
    }
}

public sealed class HistoryMonthWordsTests
{
    [Fact]
    public void Tokens_DropMonthNames()
    {
        var tokens = HistoryText.Tokens("پرداخت مالیات حقوق بهمن‌ماه و اسفند ماه کارکنان ماهانه");
        Assert.Equal(["پرداخت", "مالیات", "حقوق", "کارکنان"], tokens);
    }

    [Fact]
    public void WithoutMonths_MergesMonthlyDescriptions() =>
        Assert.Equal(HistoryText.WithoutMonths("مالیات حقوق بهمن"), HistoryText.WithoutMonths("مالیات حقوق اسفندماه"));
}
