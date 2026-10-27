using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Periods;
using Moq;
using Accounting.Application.OperationTemplates;
using Accounting.Application.OperationTemplates.Agent;
using Accounting.Domain.OperationTemplates;
using static Accounting.Application.Tests.OperationTemplates.TemplateBuilders;

namespace Accounting.Application.Tests.OperationTemplates;

public class IntentResultParserTests
{
    private static OperationTemplate Template()
    {
        var t = Simple();
        t.Parameters =
        [
            Amount(),
            DetailParam(),
            TextParam(),
            new TemplateParameter { Id = Guid.NewGuid(), Key = "date", Title = "تاریخ", Type = ParameterType.Date, AskPrompt = "کی؟", SortOrder = 4 },
        ];
        return t;
    }

    private static InterpretResultDto Parse(string json, OperationTemplate? t = null)
        => IntentResultParser.Parse(json, [t ?? Template()], "Test");

    [Fact]
    public void InvalidJson_FailsWithMessage()
    {
        var r = Parse("not json");
        Assert.True(r.Enabled);
        Assert.False(r.Succeeded);
        Assert.NotNull(r.Message);
        Assert.Null(r.TemplateId);
    }

    [Fact]
    public void NonObjectJson_Fails()
    {
        var r = Parse("[1,2]");
        Assert.False(r.Succeeded);
    }

    [Fact]
    public void UnknownTemplateCode_SucceedsWithoutTemplate_KeepsClarification()
    {
        var r = Parse("""{"templateCode":"NOPE","confidence":0.4,"voucherDate":null,"answers":[],"clarification":"کدام؟"}""");
        Assert.True(r.Succeeded);
        Assert.Null(r.TemplateId);
        Assert.Equal("کدام؟", r.Clarification);
        Assert.Equal(0.4, r.Confidence);
    }

    [Fact]
    public void TemplateCode_IsCaseInsensitive_AndTrimmed()
    {
        var t = Template();
        var r = Parse("""{"templateCode":" receive_cash ","confidence":1,"answers":[]}""", t);
        Assert.Equal(t.Id, r.TemplateId);
    }

    [Fact]
    public void Confidence_IsClamped()
    {
        Assert.Equal(1, Parse("""{"templateCode":"RECEIVE_CASH","confidence":7}""").Confidence);
        Assert.Equal(0, Parse("""{"templateCode":"RECEIVE_CASH","confidence":-2}""").Confidence);
        Assert.Null(Parse("""{"templateCode":"RECEIVE_CASH","confidence":"high"}""").Confidence);
    }

    [Theory]
    [InlineData("1,500,000", "1500000")]
    [InlineData("۲۵۰۰۰", "25000")]
    [InlineData("٣٠٠", "300")]
    public void Amount_IsNormalizedToLatinDigits(string raw, string expected)
    {
        var r = Parse($$"""{"templateCode":"RECEIVE_CASH","answers":[{"key":"amount","value":"{{raw}}"}]}""");
        Assert.Equal(expected, r.Answers.Single().Value);
        Assert.Equal(ParameterType.Amount, r.Answers.Single().Type);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("abc")]
    [InlineData("1234567890123456")]
    public void InvalidAmount_IsDropped(string raw)
    {
        var r = Parse($$"""{"templateCode":"RECEIVE_CASH","answers":[{"key":"amount","value":"{{raw}}"}]}""");
        Assert.Empty(r.Answers);
    }

    [Theory]
    [InlineData("1404/07/08", "14040708")]
    [InlineData("۱۴۰۴-۰۷-۰۸", "14040708")]
    [InlineData("14031230", "14031230")]
    public void Date_IsNormalized(string raw, string expected)
    {
        var r = Parse($$"""{"templateCode":"RECEIVE_CASH","answers":[{"key":"date","value":"{{raw}}"}]}""");
        Assert.Equal(expected, r.Answers.Single().Value);
    }

    [Theory]
    [InlineData("14041332")]
    [InlineData("14041301")]
    [InlineData("12001010")]
    [InlineData("14041230")]
    [InlineData("1404078")]
    public void InvalidDate_IsDropped(string raw)
    {
        var r = Parse($$"""{"templateCode":"RECEIVE_CASH","voucherDate":"{{raw}}","answers":[{"key":"date","value":"{{raw}}"}]}""");
        Assert.Empty(r.Answers);
        Assert.Null(r.VoucherDate);
    }

    [Fact]
    public void VoucherDate_PersianDigits_IsAccepted()
    {
        var r = Parse("""{"templateCode":"RECEIVE_CASH","voucherDate":"۱۴۰۴۰۷۰۸"}""");
        Assert.Equal("14040708", r.VoucherDate);
    }

    [Fact]
    public void UnknownKey_AndDuplicateKey_AreDropped()
    {
        var r = Parse("""
        {"templateCode":"RECEIVE_CASH","answers":[
          {"key":"amount","value":"100"},
          {"key":"AMOUNT","value":"200"},
          {"key":"ghost","value":"x"},
          {"key":"person","value":"شرکت هجرت"}
        ]}
        """);
        Assert.Equal(2, r.Answers.Count);
        Assert.Equal("100", r.Answers.Single(a => a.Key == "amount").Value);
        Assert.Equal("شرکت هجرت", r.Answers.Single(a => a.Key == "person").Value);
    }

    [Fact]
    public void LongTextAndDetail_AreTruncated()
    {
        var longText = new string('ا', 900);
        var r = Parse($$"""{"templateCode":"RECEIVE_CASH","answers":[{"key":"note1","value":"{{longText}}"},{"key":"person","value":"{{longText}}"}]}""");
        Assert.Equal(500, r.Answers.Single(a => a.Key == "note1").Value.Length);
        Assert.Equal(100, r.Answers.Single(a => a.Key == "person").Value.Length);
    }

    [Fact]
    public void BlankOrNonStringValues_AreSkipped()
    {
        var r = Parse("""{"templateCode":"RECEIVE_CASH","answers":[{"key":"amount","value":"  "},{"key":"note1","value":5},{"value":"x"}]}""");
        Assert.Empty(r.Answers);
    }

    [Fact]
    public void Clarification_IsLimitedTo300()
    {
        var r = Parse($$"""{"templateCode":null,"clarification":"{{new string('س', 400)}}"}""");
        Assert.Equal(300, r.Clarification!.Length);
    }

    [Fact]
    public void PromptBuilder_IncludesCatalogFiscalYearAndSentence()
    {
        var t = Template();
        t.Keywords = "دارو\nقرص خریدم";
        var p = IntentPromptBuilder.Build("یک میلیون از هجرت گرفتم", [t], "1404");
        Assert.Contains("code: RECEIVE_CASH", p.User);
        Assert.Contains("keywords: دارو | قرص خریدم", p.User);
        Assert.Contains("key: amount | type: Amount", p.User);
        Assert.EndsWith("یک میلیون از هجرت گرفتم" + Environment.NewLine, p.User);
        Assert.Contains("fiscal year is 1404", p.System);
        Assert.Equal(IntentPromptBuilder.Schema, p.JsonSchema);
    }
}

public class InterpretTrustAnswersTests
{
    private sealed class Ai(bool trust, bool interpret = true) : IIntentInterpreter
    {
        public bool InterpretEnabled => interpret;
        public bool IsEnabled => true;
        public string ProviderName => "Ollama";
        public bool RequiresPiiMasking => false;
        public bool TrustAnswers => trust;
        public Task<string> CompleteJsonAsync(IntentPrompt prompt, CancellationToken ct) =>
            Task.FromResult("""{"templateCode":"RECEIVE_CASH","confidence":0.9,"voucherDate":"14040708","answers":[{"key":"amount","value":"1000000"}]}""");
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task UntrustedModel_KeepsTemplate_DropsAnswers(bool trust, int answers)
    {
        var t = Simple();
        var repo = new Mock<IOperationTemplateRepository>();
        repo.Setup(r => r.ListActiveWithParametersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([t]);
        var units = Mock.Of<IUnitAccessReadRepository>();
        var policy = new AssistantUnitPolicy(units, new FsPeriodGuard(
            Mock.Of<IFsPeriodRepository>(), units));

        var r = await new InterpretSentenceHandler(new Ai(trust), repo.Object, policy)
            .Handle(new InterpretSentenceQuery("یک میلیون گرفتم") { VahedCode = "0101" }, default);

        Assert.Equal(t.Id, r.TemplateId);
        Assert.Equal(answers, r.Answers.Count);
        Assert.Equal(trust, r.VoucherDate is not null);
    }

    [Fact]
    public async Task InterpretDisabled_ReturnsDisabled_SoPageUsesRules()
    {
        var repo = new Mock<IOperationTemplateRepository>();
        var units = Mock.Of<IUnitAccessReadRepository>();
        var policy = new AssistantUnitPolicy(units, new FsPeriodGuard(Mock.Of<IFsPeriodRepository>(), units));
        var r = await new InterpretSentenceHandler(new Ai(true, interpret: false), repo.Object, policy)
            .Handle(new InterpretSentenceQuery("یک میلیون گرفتم") { VahedCode = "0101" }, default);
        Assert.False(r.Enabled);
        repo.Verify(x => x.ListActiveWithParametersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
