using System.Runtime.CompilerServices;
using Accounting.Application.OperationTemplates.History;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.Tests.OperationTemplates;

/// <summary>معناهای الگوی سابقه (DDL 089): گروه‌بندی پیوند میانگین، انتخاب معنا و همگام‌سازی.</summary>
public sealed class HistoryMeaningTests
{
    private static readonly Guid P = Guid.Parse("00000000-0000-0000-0000-0000000000aa");
    private static readonly DateTime Now = new(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

    // دو خوشهٔ جدا (پذیرایی ~ محور x، ایاب‌وذهاب ~ محور y) و یک شرح دورافتاده
    private static readonly MeaningItem Food1 = Item("پذیرایی جلسه", 6, [1f, 0.1f, 0]);
    private static readonly MeaningItem Food2 = Item("خرید میوه و شیرینی", 3, [0.95f, 0.2f, 0]);
    private static readonly MeaningItem Taxi1 = Item("کرایه تاکسی ماموریت", 4, [0.1f, 1f, 0]);
    private static readonly MeaningItem Taxi2 = Item("هزینه ایاب و ذهاب", 3, [0.2f, 0.95f, 0]);
    private static readonly MeaningItem Odd = Item("تعمیر شیرآلات", 1, [0, 0, 1f]);

    [Fact]
    public void Cluster_SeparatesMeanings_BiggestFirst()
    {
        var groups = HistoryMeaningClusterer.Cluster([Taxi1, Food1, Odd, Food2, Taxi2], threshold: 0.6);

        Assert.Equal(3, groups.Count);
        Assert.Equal([1, 3], groups[0].Order()); // پذیرایی (وزن ۹)
        Assert.Equal([0, 4], groups[1].Order()); // ایاب‌وذهاب (وزن ۷)
        Assert.Equal([2], groups[2]);
    }

    [Fact]
    public void Cluster_LowThreshold_MergesAll() =>
        Assert.Single(HistoryMeaningClusterer.Cluster([Food1, Taxi1, Odd], threshold: -1));

    [Fact]
    public void Meanings_TwoBigGroups_SmallOneDropped()
    {
        var m = HistoryMeaningClusterer.Meanings(P, [Food1, Food2, Taxi1, Taxi2, Odd], 0.6, minWeight: 3, minShare: 10, Now);

        Assert.Equal(2, m.Count);
        Assert.Equal([0, 1], m.Select(x => x.Index));
        Assert.Equal("پذیرایی جلسه", m[0].Title);
        Assert.Equal(52.9m, m[0].Share); // ۹ از ۱۷
        Assert.Equal(9, m[0].SampleCount);
        Assert.Equal(["پذیرایی جلسه", "خرید میوه و شیرینی"], HistoryJson.Strings(m[0].SamplesJson));
        Assert.Contains(HistoryJson.Keywords(m[1].KeywordsJson), k => k.Word == "تاکسی");
        Assert.All(m, x => Assert.Equal(P, x.PatternId));
    }

    [Fact]
    public void Meanings_OneMeaning_GivesSingleWholeRow()
    {
        var m = HistoryMeaningClusterer.Meanings(P, [Food1, Food2, Odd], 0.6, minWeight: 3, minShare: 10, Now);

        var row = Assert.Single(m);
        Assert.Equal(0, row.Index);
        Assert.Equal(100m, row.Share);
        Assert.Equal(10, row.SampleCount);
    }

    [Fact]
    public void Meanings_Empty_GivesNothing() =>
        Assert.Empty(HistoryMeaningClusterer.Meanings(P, [], 0.6, 3, 10, Now));

    [Fact]
    public void ForMeaning_UsesMeaningTextAndScalesCount()
    {
        var pattern = new HistoryPattern { Id = P, Signature = "s", LinesJson = "[]", VoucherCount = 40, KeywordsJson = "[]", SampleDescsJson = "[]" };
        var meaning = new HistoryPatternMeaning { PatternId = P, Index = 1, Title = "ایاب", Share = 25m, KeywordsJson = """[{"w":"تاکسی","c":3}]""", SamplesJson = """["کرایه تاکسی"]""" };

        var copy = HistoryPatternToTemplateHandler.ForMeaning(pattern, meaning);

        Assert.Equal(10, copy.VoucherCount);
        Assert.Equal(meaning.KeywordsJson, copy.KeywordsJson);
        Assert.Equal(meaning.SamplesJson, copy.SampleDescsJson);
        Assert.Equal("[]", copy.LinesJson);
        Assert.Equal(40, pattern.VoucherCount); // اصل دست‌نخورده
    }

    [Fact]
    public async Task Sync_SkipsWhenBuiltAfterLastRebuild()
    {
        var store = new FakeMeaningStore { LastBuilt = Now.AddHours(1), Stamp = (Now, 1401, 1404) };
        var result = await Sync(store, new FakeSource()).RunAsync(CancellationToken.None);

        Assert.True(result.Skipped);
        Assert.Null(store.Saved);
    }

    [Fact]
    public async Task Sync_GroupsSamplesPerPattern_MaskingNamesAndSkippingUnknownPatterns()
    {
        var sig = $"{Guid.Parse("11111111-1111-1111-1111-111111111111"):D}:1|{Guid.Parse("22222222-2222-2222-2222-222222222222"):D}:2";
        var id = RebuildHistoryPatternsHandler.StableId(HistoryJson.SignatureOf(RebuildHistoryPatternsHandler.ParseSignature(sig)));
        var source = new FakeSource();
        source.Rows.AddRange([
            new HistorySampleRow(sig, "1001", 1, 1, "پذیرایی جلسه از شرکت الف", null),
            new HistorySampleRow(sig, "1001", 2, 2, "پذیرایی جلسه از شرکت ب", null),
            new HistorySampleRow(sig, "1002", 1, 3, null, "کرایه تاکسی"),
            new HistorySampleRow(sig, "1002", 1, 150, "خارج از نمونه", null),
            new HistorySampleRow("x:1|y:2", "1001", 1, 1, "الگوی ناشناخته", null),
        ]);
        var store = new FakeMeaningStore { Stamp = (Now, 1401, 1404) };

        var result = await Sync(store, source, id).RunAsync(CancellationToken.None);

        Assert.False(result.Skipped);
        Assert.Equal(1, result.Patterns);
        var row = Assert.Single(store.Saved!); // پذیرایی ۲ و تاکسی ۱ ⇒ زیر حداقل ۳ ⇒ یک معنای کلی
        Assert.Equal(id, row.PatternId);
        Assert.Equal("پذیرایی جلسه", row.Title); // نام طرف حساب پوشانده و دو شرح یکی شد
        Assert.Equal(3, row.SampleCount);
        Assert.Equal((1401, 1404), (source.From, source.To));
    }

    private static HistoryMeaningSync Sync(FakeMeaningStore store, FakeSource source, params Guid[] live) =>
        new(source, store, new FakePatterns(live), new KeywordEmbedder(), new HistoryEmbeddingOptions());

    private static MeaningItem Item(string text, int weight, float[] v) => new(text, weight, HistoryVectorMath.Normalize(v));

    /// <summary>بردار ساختگی: «تاکسی» محور y، بقیه محور x.</summary>
    private sealed class KeywordEmbedder : IHistoryEmbedder
    {
        public bool IsEnabled => true;
        public string ModelName => "m";

        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<float[]>>(texts.Select(t => t.Contains("تاکسی") ? new[] { 0f, 1f } : new[] { 1f, 0f }).ToList());

    }

    private sealed class FakeSource : IHistorySourceReader
    {
        public List<HistorySampleRow> Rows { get; } = [];
        public int From { get; private set; }
        public int To { get; private set; }

        public Task<IReadOnlyList<HistoryAggRow>> ReadAggregatesAsync(int fromYear, int toYear, int minCount, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<HistoryShareRow>> ReadSharesAsync(int fromYear, int toYear, int minCount, CancellationToken ct) => throw new NotSupportedException();

        public async IAsyncEnumerable<HistorySampleRow> ReadSamplesAsync(int fromYear, int toYear, int minCount, int globalSample, int unitSample,
            [EnumeratorCancellation] CancellationToken ct)
        {
            (From, To) = (fromYear, toYear);
            foreach (var r in Rows) yield return r;
            await Task.CompletedTask;
        }
    }

    private sealed class FakeMeaningStore : IHistoryMeaningStore
    {
        public DateTime? LastBuilt { get; set; }
        public (DateTime RebuiltAt, int FromYear, int ToYear)? Stamp { get; set; }
        public IReadOnlyList<HistoryPatternMeaning>? Saved { get; private set; }

        public Task<DateTime?> LastBuiltAsync(CancellationToken ct) => Task.FromResult(LastBuilt);
        public Task<(DateTime RebuiltAt, int FromYear, int ToYear)?> PatternsStampAsync(CancellationToken ct) => Task.FromResult(Stamp);

        public Task ReplaceAllAsync(IReadOnlyList<HistoryPatternMeaning> meanings, CancellationToken ct)
        {
            Saved = meanings;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<HistoryPatternMeaning>>> GetForAsync(IReadOnlyCollection<Guid> patternIds, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class FakePatterns(Guid[] live) : IHistoryVectorStore
    {
        public Task<IReadOnlyList<HistoryPatternText>> PatternTextsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<HistoryPatternText>>(live.Select(id => new HistoryPatternText(id, null, null)).ToList());

        public Task<IReadOnlyDictionary<Guid, string?>> HashesAsync(string model, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<HistoryPatternVector>> LoadAsync(string model, CancellationToken ct) => throw new NotSupportedException();
        public Task UpsertAsync(IReadOnlyList<HistoryPatternVector> vectors, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> DeleteStaleAsync(string model, CancellationToken ct) => throw new NotSupportedException();
    }
}
