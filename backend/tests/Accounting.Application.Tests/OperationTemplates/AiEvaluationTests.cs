using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.OperationTemplates;
using Accounting.Application.OperationTemplates.Agent;
using Accounting.Domain.OperationTemplates;
using Moq;
using static Accounting.Application.Tests.OperationTemplates.TemplateBuilders;

namespace Accounting.Application.Tests.OperationTemplates;

public class AiEvaluationTests
{
    private const string Vahed = "0101";
    private static readonly Guid PersonId = Guid.NewGuid();

    private readonly OperationTemplate _template;
    private readonly Mock<IAiEvaluationRepository> _repo = new();
    private readonly Mock<IOperationTemplateRepository> _templates = new();
    private readonly Mock<IDetailReader> _details = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IHeadquartersAccessService> _hq = new();
    private readonly List<AiGoldenCase> _addedGolden = [];

    public AiEvaluationTests()
    {
        _template = Simple();
        _template.Parameters =
        [
            Amount(),
            DetailParam(),
            TextParam(),
            new TemplateParameter { Id = Guid.NewGuid(), Key = "when", Title = "تاریخ", Type = ParameterType.Date, AskPrompt = "کی؟", SortOrder = 4 },
        ];
        _templates.Setup(t => t.ListActiveWithParametersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([_template]);
        _user.Setup(u => u.UserId).Returns("hq");
        _hq.Setup(h => h.HasAbilityAsync(AbilityCatalog.AiEvaluate, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _details.Setup(d => d.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DetailInfo> { [PersonId] = new(PersonId, "12", "شرکت هجرت", new HashSet<Guid>(), true, true) });
        _repo.Setup(r => r.AddGoldenAsync(It.IsAny<AiGoldenCase>(), It.IsAny<CancellationToken>()))
            .Callback<AiGoldenCase, CancellationToken>((g, _) => _addedGolden.Add(g));
        _repo.Setup(r => r.GoldenSourceExecutionIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<Guid>());
    }

    private static InterpretResultDto Actual(Guid? templateId, params (string Key, string Value, ParameterType Type)[] answers) =>
        new(true, "Test", true, null, templateId, 0.9, null, answers.Select(a => new InterpretedAnswerDto(a.Key, a.Value, a.Type)).ToList(), null);

    private string InputJson(long amount = 1000) =>
        $$"""{"amount":"{{amount}}","person":"{{PersonId}}","note1":"خرید دارو","when":"2025-09-30"}""";

    // ─────────── امتیازدهی ───────────

    [Fact]
    public void Score_AllCorrect_IgnoresTextAndMatchesDetailByContainment()
    {
        var expected = new Dictionary<string, string> { ["amount"] = "1000", ["person"] = "شرکت هجرت", ["note1"] = "هرچه", ["when"] = "14040708" };
        var s = AiEvalScorer.Score(Actual(_template.Id, ("amount", "1000", ParameterType.Amount), ("person", "هجرت", ParameterType.Detail),
            ("when", "۱۴۰۴۰۷۰۸", ParameterType.Date)), _template.Code, _template.Code, expected, _template);

        Assert.True(s.TemplateOk);
        Assert.Equal(3, s.AnswersExpected);
        Assert.Equal(3, s.AnswersOk);
        Assert.Empty(s.Diffs);
    }

    [Fact]
    public void Score_WrongAmountAndMissingDetail_ListsDiffs()
    {
        var expected = new Dictionary<string, string> { ["amount"] = "1000", ["person"] = "شرکت هجرت" };
        var s = AiEvalScorer.Score(Actual(_template.Id, ("amount", "100", ParameterType.Amount)), "receive_cash", _template.Code, expected, _template);
        Assert.True(s.TemplateOk);
        Assert.Equal(0, s.AnswersOk);
        Assert.Equal(["amount", "person"], s.Diffs.Select(d => d.Key));
    }

    [Fact]
    public void Score_WrongTemplate_NoAnswerCredit()
    {
        var expected = new Dictionary<string, string> { ["amount"] = "1000" };
        var s = AiEvalScorer.Score(Actual(null, ("amount", "1000", ParameterType.Amount)), null, _template.Code, expected, _template);
        Assert.False(s.TemplateOk);
        Assert.Equal(1, s.AnswersExpected);
        Assert.Equal(0, s.AnswersOk);
        Assert.Equal("template", s.Diffs.Single().Key);
    }

    [Fact]
    public void Score_ExpectedNoTemplate_AndNoneChosen_IsCorrect()
    {
        var s = AiEvalScorer.Score(Actual(null), null, " ", new Dictionary<string, string>(), null);
        Assert.True(s.TemplateOk);
        Assert.Equal(0, s.AnswersExpected);
    }

    [Fact]
    public void Score_SingleLetterDetail_DoesNotMatch()
    {
        var expected = new Dictionary<string, string> { ["person"] = "شرکت هجرت" };
        var s = AiEvalScorer.Score(Actual(_template.Id, ("person", "ت", ParameterType.Detail)), _template.Code, _template.Code, expected, _template);
        Assert.Equal(0, s.AnswersOk);
    }

    [Fact]
    public void Expected_FromExecution_ConvertsDateAndDetail_SkipsText()
    {
        var e = AiEvalExpected.FromExecution(_template, InputJson(), new Dictionary<Guid, DetailInfo>
        {
            [PersonId] = new(PersonId, "12", "شرکت هجرت", new HashSet<Guid>(), true, true),
        });
        Assert.Equal("1000", e["amount"]);
        Assert.Equal("شرکت هجرت", e["person"]);
        Assert.Equal("14040708", e["when"]);
        Assert.False(e.ContainsKey("note1"));
        Assert.Equal([PersonId], AiEvalExpected.DetailIds(_template, InputJson()));
        Assert.Equal(e.Count, AiEvalExpected.Parse(AiEvalExpected.Serialize(e)).Count);
        Assert.Empty(AiEvalExpected.Parse("{bad"));
    }

    // ─────────── دسترسی ───────────

    [Fact]
    public async Task WithoutAbility_Denied()
    {
        _hq.Setup(h => h.HasAbilityAsync(AbilityCatalog.AiEvaluate, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            new ListAiGoldenCasesHandler(_repo.Object, _user.Object, _hq.Object).Handle(new ListAiGoldenCasesQuery(), default));
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            Run(new FakeInterpreter("{}")).Handle(new RunAiEvaluationCommand("golden") { VahedCode = Vahed }, default));
    }

    // ─────────── نمونهٔ طلایی ───────────

    [Fact]
    public async Task SaveGolden_NormalizesKeys_AndRejectsUnknownKeyOrTemplate()
    {
        var h = new SaveAiGoldenCaseHandler(_repo.Object, _templates.Object, _uow.Object, _user.Object, _hq.Object);

        var dto = await h.Handle(new SaveAiGoldenCaseCommand(null, " صد هزار از هجرت ", "receive_cash",
            new() { ["AMOUNT"] = "100000", ["person"] = " " }, null), default);

        Assert.Equal("RECEIVE_CASH", dto.ExpectedTemplateCode);
        Assert.Equal("صد هزار از هجرت", dto.Sentence);
        Assert.Equal("100000", dto.ExpectedAnswers["amount"]);
        Assert.False(dto.ExpectedAnswers.ContainsKey("person"));

        await Assert.ThrowsAsync<BusinessRuleException>(() => h.Handle(new SaveAiGoldenCaseCommand(null, "x", "RECEIVE_CASH", new() { ["ghost"] = "1" }, null), default));
        await Assert.ThrowsAsync<BusinessRuleException>(() => h.Handle(new SaveAiGoldenCaseCommand(null, "x", "NOPE", null, null), default));
    }

    [Fact]
    public async Task SaveGolden_WithoutTemplate_MeansExpectNone()
    {
        var dto = await new SaveAiGoldenCaseHandler(_repo.Object, _templates.Object, _uow.Object, _user.Object, _hq.Object)
            .Handle(new SaveAiGoldenCaseCommand(null, "هوا چطوره؟", null, new() { ["amount"] = "1" }, "بی‌ربط"), default);
        Assert.Null(dto.ExpectedTemplateCode);
        Assert.Empty(dto.ExpectedAnswers);
    }

    [Fact]
    public async Task ImportGolden_SkipsKnownAndUnknownTemplates_LimitsTake()
    {
        var known = Guid.NewGuid();
        _repo.Setup(r => r.GoldenSourceExecutionIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<Guid> { known });
        _repo.Setup(r => r.ListShadowSourcesAsync(Vahed, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ShadowSourceRow(known, Vahed, "قبلی", _template.Code, InputJson()),
            new ShadowSourceRow(Guid.NewGuid(), Vahed, "الگوی حذف‌شده", "OLD", InputJson()),
            new ShadowSourceRow(Guid.NewGuid(), Vahed, "هزار ریال از هجرت", _template.Code, InputJson()),
            new ShadowSourceRow(Guid.NewGuid(), Vahed, "دومی", _template.Code, InputJson(5)),
        ]);

        var n = await new ImportAiGoldenCasesHandler(_repo.Object, _templates.Object, _details.Object, _uow.Object, _user.Object, _hq.Object)
            .Handle(new ImportAiGoldenCasesCommand(Take: 1) { VahedCode = Vahed }, default);

        Assert.Equal(1, n);
        var g = _addedGolden.Single();
        Assert.Equal("هزار ریال از هجرت", g.Sentence);
        Assert.Contains("شرکت هجرت", g.ExpectedAnswersJson);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────── اجرای سنجش ───────────

    private sealed class FakeInterpreter(string json, bool enabled = true) : IIntentInterpreter
    {
        public List<string> Sentences { get; } = [];
        public bool IsEnabled => enabled;
        public string ProviderName => "Fake";
        public string? ModelName => "m1";
        public bool RequiresPiiMasking => true;

        public Task<string> CompleteJsonAsync(IntentPrompt prompt, CancellationToken ct)
        {
            Sentences.Add(prompt.User);
            return json == "throw" ? throw new HttpRequestException("x") : Task.FromResult(json);
        }
    }

    private RunAiEvaluationHandler Run(IIntentInterpreter interpreter) =>
        new(interpreter, _repo.Object, _templates.Object, _details.Object, _uow.Object, _user.Object, _hq.Object);

    [Fact]
    public async Task GoldenRun_ScoresAndStoresMaskedSentence()
    {
        _repo.Setup(r => r.ListGoldenAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new AiGoldenCase { Id = Guid.NewGuid(), Sentence = "هزار ریال از هجرت، کد ملی 0499370899", ExpectedTemplateCode = _template.Code,
                ExpectedAnswersJson = """{"amount":"1000","person":"شرکت هجرت"}""" },
        ]);
        AiEvalRun? run = null;
        IReadOnlyList<AiEvalItem>? items = null;
        _repo.Setup(r => r.AddRunAsync(It.IsAny<AiEvalRun>(), It.IsAny<IReadOnlyList<AiEvalItem>>(), It.IsAny<CancellationToken>()))
            .Callback<AiEvalRun, IReadOnlyList<AiEvalItem>, CancellationToken>((r, i, _) => { run = r; items = i; });
        var ai = new FakeInterpreter("""{"templateCode":"RECEIVE_CASH","confidence":0.9,"answers":[{"key":"amount","value":"1000"},{"key":"person","value":"هجرت"}]}""");

        var dto = await Run(ai).Handle(new RunAiEvaluationCommand("golden") { VahedCode = Vahed, Year = "1404" }, default);

        Assert.Equal("golden", dto.Run.Kind);
        Assert.Equal(1, run!.Total);
        Assert.Equal(1, run.TemplateHits);
        Assert.Equal(2, run.AnswerTotal);
        Assert.Equal(2, run.AnswerHits);
        Assert.Equal(0, run.Failures);
        Assert.DoesNotContain("0499370899", items!.Single().MaskedSentence);
        Assert.DoesNotContain("0499370899", ai.Sentences.Single());
        Assert.Equal("m1", run.Model);
    }

    [Fact]
    public async Task ShadowRun_ComparesWithWhatUserRecorded_CountsFailures()
    {
        _repo.Setup(r => r.ListShadowSourcesAsync(null, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ShadowSourceRow(Guid.NewGuid(), "0202", "پنج ریال گرفتم", _template.Code, InputJson(5)),
        ]);
        AiEvalRun? run = null;
        _repo.Setup(r => r.AddRunAsync(It.IsAny<AiEvalRun>(), It.IsAny<IReadOnlyList<AiEvalItem>>(), It.IsAny<CancellationToken>()))
            .Callback<AiEvalRun, IReadOnlyList<AiEvalItem>, CancellationToken>((r, _, _) => run = r);

        var dto = await Run(new FakeInterpreter("throw")).Handle(new RunAiEvaluationCommand("shadow", AllUnits: true) { VahedCode = Vahed }, default);

        Assert.Equal("shadow", dto.Run.Kind);
        Assert.Equal(1, run!.Failures);
        Assert.Equal(0, run.TemplateHits);
        Assert.Equal("HttpRequestException", dto.Items.Single().Error);
        _details.Verify(d => d.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), "0202", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Run_NoCases_OrAiOff_IsBusinessRule()
    {
        _repo.Setup(r => r.ListGoldenAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(new FakeInterpreter("{}")).Handle(new RunAiEvaluationCommand("golden") { VahedCode = Vahed }, default));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(new FakeInterpreter("{}", enabled: false)).Handle(new RunAiEvaluationCommand("golden") { VahedCode = Vahed }, default));
    }

    [Fact]
    public void RunValidator_OnlyKnownKinds()
    {
        var v = new RunAiEvaluationValidator();
        Assert.True(v.Validate(new RunAiEvaluationCommand("shadow")).IsValid);
        Assert.False(v.Validate(new RunAiEvaluationCommand("other")).IsValid);
    }
}
