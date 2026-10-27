using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Periods;
using Accounting.Application.OperationTemplates;
using Accounting.Application.OperationTemplates.Agent;
using Accounting.Domain.OperationTemplates;
using MediatR;
using Moq;
using static Accounting.Application.Tests.OperationTemplates.TemplateBuilders;

namespace Accounting.Application.Tests.OperationTemplates;

public class AgentChatHandlerTests
{
    private const string Vahed = "0101";
    private const string NationalCode = "0499370899";

    /// <summary>مدل ساختگی: پاسخ‌های از پیش نوشته را به ترتیب برمی‌گرداند و درخواست‌ها را نگه می‌دارد.</summary>
    private sealed class ScriptedModel(bool external, params Func<AgentModelRequest, AgentModelReply>[] script) : IAgentChatModel
    {
        private int _i;
        public List<AgentModelRequest> Requests { get; } = [];
        public bool IsEnabled => true;
        public string ProviderName => external ? "Claude" : "Ollama";
        public string? ModelName => "test";
        public bool RequiresPiiMasking => external;
        public bool IsExternal => external;

        public Task<AgentModelReply> CompleteAsync(AgentModelRequest request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(script[Math.Min(_i++, script.Length - 1)](request));
        }
    }

    private readonly Mock<IAgentConversationRepository> _conversations = new();
    private readonly Mock<IOperationTemplateRepository> _templates = new();
    private readonly Mock<IHesabyarAuditRepository> _audit = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<ISender> _mediator = new();
    private readonly List<AgentMessage> _saved = [];
    private OperationAiTrace? _trace;
    private readonly OperationTemplate _template = Simple();

    public AgentChatHandlerTests()
    {
        _user.Setup(u => u.UserId).Returns("u1");
        _templates.Setup(t => t.ListActiveWithParametersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([_template]);
        _conversations.Setup(c => c.AddMessageAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .Callback<AgentMessage, CancellationToken>((m, _) => _saved.Add(m));
        _audit.Setup(a => a.AddTraceAsync(It.IsAny<OperationAiTrace>(), It.IsAny<CancellationToken>()))
            .Callback<OperationAiTrace, CancellationToken>((t, _) => _trace = t);
        _mediator.Setup(m => m.Send(It.IsAny<PreviewOperationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult(true,
                new VoucherDraft(Vahed, new DateOnly(2025, 9, 30), "دریافت", "RECEIVE_CASH",
                    [new VoucherDraftLine(Acc1, "صندوق", [], 100, 0, ""), new VoucherDraftLine(Acc2, "درآمد", [], 0, 100, "")]),
                []));
    }

    private SendAgentMessageHandler Handler(IAgentChatModel model, AssistantAgentOptions? options = null)
    {
        var units = Mock.Of<IUnitAccessReadRepository>();
        var policy = new AssistantUnitPolicy(units, new FsPeriodGuard(Mock.Of<IFsPeriodRepository>(), units));
        return new SendAgentMessageHandler(model, _conversations.Object, _templates.Object, policy,
            new AgentToolbox(_mediator.Object, Mock.Of<ISubsidiaryAccountReader>(), Mock.Of<IDetailReader>()),
            _audit.Object, _uow.Object, _user.Object, options);
    }

    private static SendAgentMessageCommand Cmd(string message, Guid? conversationId = null) =>
        new(conversationId, message) { VahedCode = Vahed, Year = "1404" };

    private static AgentModelReply Text(string s) => new(s, []);

    private static AgentModelReply Call(string tool, string args) => new(null, [new AgentToolCall("t" + Guid.NewGuid().ToString("N")[..6], tool, args)]);

    [Fact]
    public async Task Disabled_ReturnsDisabled_NoWrites()
    {
        var model = new Mock<IAgentChatModel>();
        model.Setup(m => m.IsEnabled).Returns(false);
        var r = await Handler(model.Object).Handle(Cmd("سلام"), default);
        Assert.False(r.Enabled);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToolLoop_PreviewThenReply_ReturnsProposalLinkedToTrace_AndPersistsAll()
    {
        var model = new ScriptedModel(false,
            _ => Call(AgentToolbox.PreviewVoucher, """{"template_code":"RECEIVE_CASH","answers":[{"key":"amount","value":"100"}]}"""),
            _ => Text("سند دریافت ۱۰۰ ریال آماده است؛ کارت را ببینید و «بررسی و ثبت» را بزنید."));

        var r = await Handler(model).Handle(Cmd("صد ریال نقد گرفتم"), default);

        Assert.True(r.Succeeded);
        Assert.NotNull(r.ConversationId);
        Assert.Contains("بررسی و ثبت", r.Reply);
        Assert.Equal(_template.Id, r.Proposal!.TemplateId);
        Assert.Equal(_trace!.Id, r.Proposal.AiTraceId);
        Assert.Equal(AgentPromptBuilder.PromptVersion, _trace.PromptVersion);
        Assert.Single(r.Steps);
        Assert.Equal([AgentMessageRole.User, AgentMessageRole.Tool, AgentMessageRole.Assistant], _saved.Select(m => m.Role));
        Assert.Equal([1, 2, 3], _saved.Select(m => m.Seq));
        Assert.NotNull(_saved[2].ProposalJson);
        Assert.Equal(_trace.Id, _saved[2].AiTraceId);
        _conversations.Verify(c => c.AddConversationAsync(It.Is<AgentConversation>(x => x.Title == "صد ریال نقد گرفتم" && x.CreatedBy == "u1"), It.IsAny<CancellationToken>()));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // نوبت دوم مدل: نتیجهٔ ابزار با همان شناسهٔ صدا زدن
        var second = model.Requests[1].Turns;
        Assert.Equal(AgentTurnRole.Tool, second[^1].Role);
        Assert.Equal(second[^2].ToolCalls![0].Id, second[^1].ToolCallId);
    }

    [Fact]
    public async Task ExternalModel_ReceivesMaskedText_ReplyIsRestored_DataToolsOff()
    {
        var model = new ScriptedModel(true, req =>
        {
            var user = req.Turns[^1].Text!;
            Assert.DoesNotContain(NationalCode, user);
            Assert.Contains("[[NID_1]]", user);
            Assert.DoesNotContain(req.Tools, t => t.Name == AgentToolbox.SearchCounterparty);
            return Text("کد ملی [[NID_1]] را دیدم.");
        });

        var r = await Handler(model).Handle(Cmd($"کد ملی {NationalCode} است"), default);

        Assert.Contains(NationalCode, r.Reply);
        Assert.DoesNotContain(NationalCode, _trace!.MaskedSentence);
        Assert.DoesNotContain(NationalCode, _trace.ResultJson);
    }

    [Fact]
    public async Task ExternalModel_WithAllowDataTools_GetsSearchTool()
    {
        var model = new ScriptedModel(true, req =>
        {
            Assert.Contains(req.Tools, t => t.Name == AgentToolbox.SearchCounterparty);
            return Text("باشه");
        });
        await Handler(model, new AssistantAgentOptions { AllowDataToolsForExternal = true }).Handle(Cmd("سلام"), default);
    }

    [Fact]
    public async Task ServiceError_ReturnsFriendlyReply_StillSaved()
    {
        var model = new Mock<IAgentChatModel>();
        model.Setup(m => m.IsEnabled).Returns(true);
        model.Setup(m => m.ProviderName).Returns("Ollama");
        model.Setup(m => m.CompleteAsync(It.IsAny<AgentModelRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("boom"));

        var r = await Handler(model.Object).Handle(Cmd("سلام"), default);

        Assert.False(r.Succeeded);
        Assert.Contains("در دسترس نیست", r.Reply);
        Assert.Equal("HttpRequestException", _trace!.Error);
        Assert.DoesNotContain("boom", _trace.ResultJson);
        Assert.Equal(2, _saved.Count);
    }

    [Fact]
    public async Task EndlessToolCalls_StopAtMaxRounds()
    {
        var model = new ScriptedModel(false, _ => Call(AgentToolbox.PreviewVoucher, """{"template_code":"NOPE","answers":[]}"""));
        var r = await Handler(model, new AssistantAgentOptions { MaxToolRounds = 2 }).Handle(Cmd("x"), default);
        Assert.False(r.Succeeded);
        Assert.Equal(3, model.Requests.Count);
        Assert.Contains("نتوانستم", r.Reply);
    }

    [Fact]
    public async Task ExistingConversation_ReplaysHistory_ContinuesSeq_AndDropsLeadingAssistant()
    {
        var id = Guid.NewGuid();
        _conversations.Setup(c => c.FindAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentConversation { Id = id, VahedCode = Vahed, CreatedBy = "u1", Title = "t" });
        _conversations.Setup(c => c.ListMessagesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new AgentMessage { Seq = 1, Role = AgentMessageRole.User, Content = "اول" },
            new AgentMessage { Seq = 2, Role = AgentMessageRole.Tool, Content = "{}" },
            new AgentMessage { Seq = 3, Role = AgentMessageRole.Assistant, Content = "چند؟",
                ProposalJson = """{"templateCode":"RECEIVE_CASH","voucherDate":"14040708","answers":[{"key":"amount","value":"5"}]}""" },
        ]);
        var model = new ScriptedModel(false, _ => Text("باشه"));

        await Handler(model, new AssistantAgentOptions { HistoryMessages = 1 }).Handle(Cmd("دوم", id), default);

        var turns = model.Requests[0].Turns;
        Assert.Equal(AgentTurnRole.User, turns[0].Role);
        Assert.Equal("دوم", turns.Single().Text);
        Assert.Equal([4, 5], _saved.Select(m => m.Seq));
        _conversations.Verify(c => c.AddConversationAsync(It.IsAny<AgentConversation>(), It.IsAny<CancellationToken>()), Times.Never);

        // با تاریخچهٔ بیشتر: پیام دستیار خلاصهٔ پیشنهاد را با خود دارد
        model = new ScriptedModel(false, _ => Text("باشه"));
        await Handler(model).Handle(Cmd("سوم", id), default);
        Assert.Contains("previewed template RECEIVE_CASH", model.Requests[0].Turns[1].Text);
    }

    [Theory]
    [InlineData("other", Vahed, false)]
    [InlineData("u1", "9999", false)]
    [InlineData("u1", Vahed, true)]
    public async Task OthersOrDeletedConversation_IsNotFound(string owner, string vahed, bool deleted)
    {
        var id = Guid.NewGuid();
        _conversations.Setup(c => c.FindAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentConversation { Id = id, VahedCode = vahed, CreatedBy = owner, Title = "t", IsDeleted = deleted });
        await Assert.ThrowsAsync<NotFoundException>(() => Handler(new ScriptedModel(false, _ => Text("x"))).Handle(Cmd("x", id), default));
    }

    [Fact]
    public async Task NoUnit_IsBusinessRule()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Handler(new ScriptedModel(false, _ => Text("x"))).Handle(new SendAgentMessageCommand(null, "x"), default));
    }

    [Fact]
    public void Validator_RejectsEmptyAndLong()
    {
        var v = new SendAgentMessageValidator();
        Assert.False(v.Validate(new SendAgentMessageCommand(null, "")).IsValid);
        Assert.False(v.Validate(new SendAgentMessageCommand(null, new string('x', 1001))).IsValid);
        Assert.True(v.Validate(new SendAgentMessageCommand(null, "سلام")).IsValid);
    }

    [Fact]
    public void Prompt_ListsCatalog_AndSwitchesDetailRuleByDataTools()
    {
        var withTools = AgentPromptBuilder.Build([_template], "1404", true);
        var without = AgentPromptBuilder.Build([_template], "1404", false);
        Assert.Contains("code: RECEIVE_CASH", withTools);
        Assert.Contains("search_counterparty", withTools);
        Assert.DoesNotContain("search_counterparty", without);
        Assert.Contains("fiscal year is 1404", without);
    }

    [Fact]
    public async Task GetConversation_HidesToolMessages_ParsesProposal()
    {
        var id = Guid.NewGuid();
        _conversations.Setup(c => c.FindAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentConversation { Id = id, VahedCode = Vahed, CreatedBy = "u1", Title = "t" });
        _conversations.Setup(c => c.ListMessagesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new AgentMessage { Seq = 1, Role = AgentMessageRole.User, Content = "اول" },
            new AgentMessage { Seq = 2, Role = AgentMessageRole.Tool, Content = "{}" },
            new AgentMessage { Seq = 3, Role = AgentMessageRole.Assistant, Content = "تمام", ProposalJson = "{bad" },
        ]);

        var dto = await new GetAgentConversationHandler(_conversations.Object, _user.Object).Handle(new GetAgentConversationQuery(id) { VahedCode = Vahed }, default);

        Assert.Equal(["user", "assistant"], dto.Messages.Select(m => m.Role));
        Assert.Null(dto.Messages[1].Proposal);
    }

    [Fact]
    public async Task DeleteConversation_SoftDeletes()
    {
        var id = Guid.NewGuid();
        var c = new AgentConversation { Id = id, VahedCode = Vahed, CreatedBy = "u1", Title = "t" };
        _conversations.Setup(x => x.FindAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(c);
        await new DeleteAgentConversationHandler(_conversations.Object, _uow.Object, _user.Object)
            .Handle(new DeleteAgentConversationCommand(id) { VahedCode = Vahed }, default);
        Assert.True(c.IsDeleted);
    }

    [Fact]
    public void PiiMaskSession_SharesTokensAcrossTexts_AndRestores()
    {
        var s = new PiiMaskSession();
        var a = s.Mask($"کد {NationalCode} و 09123456789");
        var b = s.Mask($"دوباره {NationalCode}");
        var c = s.Mask("کد دیگر 0499370902 اینجا");

        Assert.Contains("[[NID_1]]", a);
        Assert.Contains("[[TEL_1]]", a);
        Assert.Equal("دوباره [[NID_1]]", b);
        Assert.Contains("[[NID_2]]", c);
        Assert.Equal($"{NationalCode} / 0499370902", s.Restore("[[NID_1]] / [[NID_2]]"));
        Assert.Equal("بدون داده", s.Mask("بدون داده"));
    }

    // ─────────── گارد ادعای ثبت بی‌پیش‌نویس و خاموش کردن گفتگو ───────────

    [Fact]
    public async Task ClaimWithoutPreview_IsReplaced_AndSuggestsManual()
    {
        var r = await Handler(new ScriptedModel(false, _ => Text("موفقیت آمیز: سند خرید دارو ثبت خواهد شد. لطفا کارت را بررسی و ثبت کنید."))).Handle(Cmd("دارو"), default);
        Assert.Equal(AgentReplyGuard.Replacement, r.Reply);
        Assert.True(r.SuggestManual);
        Assert.Null(r.Proposal);
        Assert.Equal(AgentReplyGuard.Replacement, _saved.Single(m => m.Role == AgentMessageRole.Assistant).Content);
    }

    [Fact]
    public async Task OrdinaryQuestion_IsNotReplaced()
    {
        var r = await Handler(new ScriptedModel(false, _ => Text("برای پیش‌نویس سند، مبلغ خرید چقدر بود؟ با چه تاریخی ثبت شود؟"))).Handle(Cmd("دارو"), default);
        Assert.StartsWith("برای پیش‌نویس", r.Reply);
        Assert.False(r.SuggestManual);
    }

    [Theory]
    [InlineData("سند ثبت شد", true)]
    [InlineData("سند ثبت می‌شود", true)]
    [InlineData("پیش‌نویس ساخته شد", true)]
    [InlineData("لطفاً كارت را ببينيد", true)]
    [InlineData("مبلغ چقدر است؟", false)]
    [InlineData("شماره کارت بانکی را بگویید", false)]
    public void Guard_DetectsOnlySuccessClaims(string reply, bool claims) => Assert.Equal(claims, AgentReplyGuard.ClaimsVoucher(reply));

    [Fact]
    public async Task AgentDisabledByOptions_ReturnsDisabled_StatusOff()
    {
        var options = new AssistantAgentOptions { Enabled = false };
        var model = new ScriptedModel(false, _ => Text("x"));
        var r = await Handler(model, options).Handle(Cmd("سلام"), default);
        Assert.False(r.Enabled);
        Assert.Empty(model.Requests);
        var status = await new GetAgentStatusHandler(model, options).Handle(new GetAgentStatusQuery(), default);
        Assert.False(status.Enabled);
    }
}
