using Accounting.Application.OperationTemplates;
using Accounting.Domain.OperationTemplates;
using Moq;
using static Accounting.Application.Tests.OperationTemplates.TemplateBuilders;

namespace Accounting.Application.Tests.OperationTemplates;

public class OperationRiskEvaluatorTests
{
    private const string Vahed = "0101";
    private readonly Mock<IOperationTemplateRepository> _repo = new();
    private readonly List<RiskHistoryRow> _history = [];

    public OperationRiskEvaluatorTests()
    {
        _repo.Setup(r => r.ListRiskHistoryAsync(Vahed, It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _history);
        _repo.Setup(r => r.UsageAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private static VoucherDraft Draft(long debit, long? credit = null) => new(
        Vahed, new DateOnly(2026, 10, 20), "شرح", "T",
        [
            new VoucherDraftLine(Acc1, "الف", [], debit, 0, ""),
            new VoucherDraftLine(Acc2, "ب", [], 0, credit ?? debit, ""),
        ]);

    private static RiskRequest Request(OperationTemplate? t, long amount = 1_000, bool compose = false, bool attachment = false,
        Dictionary<string, string?>? values = null, Dictionary<string, string?>? sources = null, bool aiConfirmed = false, long? credit = null) =>
        new(t, Vahed, Draft(amount, credit), values ?? new Dictionary<string, string?>(), compose, attachment, sources, aiConfirmed);

    private Task<RiskAssessment> Evaluate(RiskRequest r, AssistantRiskOptions? o = null) =>
        new OperationRiskEvaluator(_repo.Object, o).EvaluateAsync(r, default);

    [Fact]
    public async Task PlainTemplate_IsLow_WithBalancedControl()
    {
        var a = await Evaluate(Request(Simple()));
        Assert.Equal(RiskLevel.Low, a.Level);
        Assert.Empty(a.Reasons);
        Assert.Empty(a.Warnings);
        Assert.Contains(a.Controls, c => c.Key == "balanced" && c.Passed);
        Assert.Equal(1_000, a.TotalAmount);
        Assert.Equal("low", a.ToDto().Level);
    }

    [Fact]
    public async Task UnbalancedDraft_FailsBalancedControl()
    {
        var a = await Evaluate(Request(Simple(), amount: 1_000, credit: 900));
        Assert.Contains(a.Controls, c => c.Key == "balanced" && !c.Passed);
    }

    [Fact]
    public async Task TemplateRiskBase_SetsFloor()
    {
        var t = Simple();
        t.RiskBase = RiskLevel.High;
        Assert.Equal(RiskLevel.High, (await Evaluate(Request(t))).Level);
    }

    [Fact]
    public async Task AmountAboveMax_IsHigh_WithWarning()
    {
        var t = Simple();
        t.MaxAmount = 999;
        var a = await Evaluate(Request(t, amount: 1_000));
        Assert.Equal(RiskLevel.High, a.Level);
        Assert.NotEmpty(a.Warnings);
    }

    [Fact]
    public async Task AmountEqualToMax_IsNotRaised()
    {
        var t = Simple();
        t.MaxAmount = 1_000;
        Assert.Equal(RiskLevel.Low, (await Evaluate(Request(t, amount: 1_000))).Level);
    }

    [Fact]
    public async Task HighestFactorWins()
    {
        var t = Simple();
        t.RiskBase = RiskLevel.Medium;
        t.MaxAmount = 10;
        var a = await Evaluate(Request(t, compose: true));
        Assert.Equal(RiskLevel.High, a.Level);
        Assert.True(a.Reasons.Count >= 3);
    }

    [Theory]
    [InlineData(false, RiskLevel.Medium)]
    [InlineData(true, RiskLevel.Low)]
    public async Task RequiredAttachment(bool hasAttachment, RiskLevel expected)
    {
        var t = Simple();
        t.RequireAttachment = true;
        Assert.Equal(expected, (await Evaluate(Request(t, attachment: hasAttachment))).Level);
    }

    [Fact]
    public async Task UnknownChoiceAnswer_IsMedium()
    {
        var t = Simple();
        t.Parameters.Add(new TemplateParameter
        {
            Id = Guid.NewGuid(), Key = "kind", Title = "نوع", Type = ParameterType.Choice, AskPrompt = "نوع؟", SortOrder = 5,
        });
        var a = await Evaluate(Request(t, values: new() { ["kind"] = " unknown " }));
        Assert.Equal(RiskLevel.Medium, a.Level);
        Assert.Contains(a.Reasons, r => r.Contains("نوع"));
    }

    [Fact]
    public async Task Compose_IsMedium_EvenWithoutTemplate()
    {
        var a = await Evaluate(Request(null, compose: true));
        Assert.Equal(RiskLevel.Medium, a.Level);
        Assert.Null(a.Basis.TemplateTitle);
    }

    [Theory]
    [InlineData(false, RiskLevel.Medium)]
    [InlineData(true, RiskLevel.Low)]
    public async Task AiFilledAnswers_NeedExplicitConfirmation(bool confirmed, RiskLevel expected)
    {
        var a = await Evaluate(Request(Simple(), sources: new() { ["amount"] = "AI" }, aiConfirmed: confirmed));
        Assert.Equal(expected, a.Level);
    }

    [Theory]
    [InlineData(301, RiskLevel.Medium)] // > ۲ × میانه (۱۵۰)
    [InlineData(300, RiskLevel.Low)]    // دقیقاً ۲ برابر ⇒ نه
    public async Task AmountAnomaly_AgainstMedianOfUnitHistory(long amount, RiskLevel expected)
    {
        foreach (var x in new long[] { 100, 150, 200 })
            _history.Add(new RiskHistoryRow("{}", x, DateTime.UtcNow.AddMonths(-2)));
        Assert.Equal(expected, (await Evaluate(Request(Simple(), amount: amount))).Level);
    }

    [Fact]
    public async Task AmountAnomaly_NeedsMinimumSamples()
    {
        _history.Add(new RiskHistoryRow("{}", 100, DateTime.UtcNow.AddMonths(-2)));
        _history.Add(new RiskHistoryRow("{}", 100, DateTime.UtcNow.AddMonths(-2)));
        Assert.Equal(RiskLevel.Low, (await Evaluate(Request(Simple(), amount: 1_000_000))).Level);
    }

    [Fact]
    public async Task AmountAnomaly_FallsBackToAmountParameterForOldRows()
    {
        // ردیف‌های پیش از DDL 084 جمع ذخیره‌شده ندارند ⇒ اولین پارامتر مبلغ ورودی.
        foreach (var _ in Enumerable.Range(0, 3))
            _history.Add(new RiskHistoryRow("{\"amount\":\"100\"}", null, DateTime.UtcNow.AddMonths(-2)));
        Assert.Equal(RiskLevel.Medium, (await Evaluate(Request(Simple(), amount: 500))).Level);
    }

    [Fact]
    public async Task SameCounterpartyAndAmountThisMonth_IsDuplicate()
    {
        var t = Simple();
        t.Parameters.Add(DetailParam());
        var detail = Guid.NewGuid().ToString();
        _history.Add(new RiskHistoryRow($"{{\"person\":\"{detail.ToUpperInvariant()}\"}}", 1_000, DateTime.UtcNow.AddMinutes(-5)));

        var a = await Evaluate(Request(t, amount: 1_000, values: new() { ["person"] = detail }));
        Assert.Equal(RiskLevel.Medium, a.Level);
        Assert.Contains(a.Reasons, r => r.Contains("همین ماه"));
    }

    [Fact]
    public async Task DifferentCounterparty_IsNotDuplicate()
    {
        var t = Simple();
        t.Parameters.Add(DetailParam());
        _history.Add(new RiskHistoryRow($"{{\"person\":\"{Guid.NewGuid()}\"}}", 1_000, DateTime.UtcNow.AddMinutes(-5)));

        var a = await Evaluate(Request(t, amount: 1_000, values: new() { ["person"] = Guid.NewGuid().ToString() }));
        Assert.Equal(RiskLevel.Low, a.Level);
    }

    [Fact]
    public async Task UsageCount_ComesFromUnitUsage()
    {
        var t = Simple();
        _repo.Setup(r => r.UsageAsync(Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TemplateUsageRow(t.Id, 7, DateTime.UtcNow)]);
        var a = await Evaluate(Request(t));
        Assert.Equal(7, a.Basis.UsageCountInUnit);
        Assert.Equal("دریافت وجه", a.Basis.TemplateTitle);
    }
}
