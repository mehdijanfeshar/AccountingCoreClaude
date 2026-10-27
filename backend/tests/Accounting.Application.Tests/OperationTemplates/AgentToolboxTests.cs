using System.Text.Json;
using Accounting.Application.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.OperationTemplates;
using Accounting.Application.OperationTemplates.Agent;
using Accounting.Application.Queries.AccountCodes.GetTafsiliLevelItems;
using Accounting.Application.Reports.AccountReview;
using Accounting.Application.Reports.AccountReview.GetAccountReview;
using Accounting.Domain.OperationTemplates;
using MediatR;
using Moq;
using static Accounting.Application.Tests.OperationTemplates.TemplateBuilders;

namespace Accounting.Application.Tests.OperationTemplates;

public class AgentToolboxTests
{
    private const string Vahed = "0101";
    private static readonly Guid PersonId = Guid.NewGuid();

    private readonly Mock<ISender> _mediator = new();
    private readonly Mock<ISubsidiaryAccountReader> _accounts = new();
    private readonly Mock<IDetailReader> _details = new();
    private PreviewOperationQuery? _sentPreview;

    private readonly OperationTemplate _template;

    public AgentToolboxTests()
    {
        _template = Simple();
        _template.Code = "PAY_PERSON";
        _template.Title = "پرداخت به شخص";
        _template.Parameters =
        [
            Amount(),
            DetailParam(),
            new TemplateParameter { Id = Guid.NewGuid(), Key = "when", Title = "تاریخ", Type = ParameterType.Date, AskPrompt = "کی؟", SortOrder = 4 },
            new TemplateParameter
            {
                Id = Guid.NewGuid(), Key = "kind", Title = "نوع", Type = ParameterType.Choice, AskPrompt = "نوع؟", SortOrder = 5,
                OptionsJson = """[{"value":"cash","label":"نقدی"},{"value":"bank","label":"بانکی"}]""",
            },
        ];
        _template.Lines =
        [
            Line(LineSide.Debit, Acc1, sort: 1, details: ParamDetail(1, "person")),
            Line(LineSide.Credit, Acc2, sort: 2),
        ];

        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, SubsidiaryAccountInfo> { [Acc1] = Account(Acc1, "بدهکاران", true, Level(1)) });
        _mediator.Setup(m => m.Send(It.IsAny<PreviewOperationQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<OperationResult>, CancellationToken>((q, _) => _sentPreview = (PreviewOperationQuery)q)
            .ReturnsAsync(() => new OperationResult(true, Draft(), [], PlainText: "پرداخت به علی"));
        Search(("علی رضایی", PersonId));
    }

    private void Search(params (string Label, Guid Id)[] items) =>
        _mediator.Setup(m => m.Send(It.IsAny<GetTafsiliLevelItemsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<TafsiliLookupItemDto>
            {
                Items = items.Select(i => new TafsiliLookupItemDto(i.Id, "12", i.Label, i.Label)).ToList(),
                TotalCount = items.Length, PageNumber = 1, PageSize = 5,
            });

    private static VoucherDraft Draft() => new(Vahed, new DateOnly(2025, 9, 30), "شرح", "PAY_PERSON",
        [
            new VoucherDraftLine(Acc1, "بدهکاران", [new VoucherDraftDetail(1, LevelId1, PersonId, "علی رضایی")], 5000, 0, ""),
            new VoucherDraftLine(Acc2, "صندوق", [], 0, 5000, ""),
        ]);

    private AgentToolContext Ctx(bool dataTools = true) => new() { VahedCode = Vahed, Year = "1404", Templates = [_template], DataTools = dataTools };

    private Task<(string ResultJson, AgentStepDto Step)> Run(string tool, string args, AgentToolContext ctx) =>
        new AgentToolbox(_mediator.Object, _accounts.Object, _details.Object).RunAsync(new AgentToolCall("c1", tool, args), ctx, default);

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Specs_WithoutDataTools_OnlyPreview()
    {
        Assert.Equal([AgentToolbox.PreviewVoucher], AgentToolbox.Specs(false).Select(s => s.Name));
        Assert.Equal(4, AgentToolbox.Specs(true).Count);
        foreach (var s in AgentToolbox.Specs(true)) JsonDocument.Parse(s.InputSchemaJson).Dispose();
    }

    [Fact]
    public async Task DataTool_WhenDisabled_IsUnknown()
    {
        var (json, step) = await Run(AgentToolbox.SearchCounterparty, """{"template_code":"PAY_PERSON","parameter_key":"person","name":"علی"}""", Ctx(false));
        Assert.False(step.Succeeded);
        Assert.True(Parse(json).TryGetProperty("error", out _));
        _mediator.Verify(m => m.Send(It.IsAny<GetTafsiliLevelItemsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Search_UsesBoundAccountAndLevel_RemembersLabels()
    {
        GetTafsiliLevelItemsQuery? sent = null;
        _mediator.Setup(m => m.Send(It.IsAny<GetTafsiliLevelItemsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PagedResult<TafsiliLookupItemDto>>, CancellationToken>((q, _) => sent = (GetTafsiliLevelItemsQuery)q)
            .ReturnsAsync(new PagedResult<TafsiliLookupItemDto> { Items = [new(PersonId, "12", "علی", "علی رضایی")], TotalCount = 1 });
        var ctx = Ctx();

        var (json, step) = await Run(AgentToolbox.SearchCounterparty, """{"template_code":"pay_person","parameter_key":"PERSON","name":" علی "}""", ctx);

        Assert.True(step.Succeeded);
        Assert.Equal(Acc1, sent!.AccountCodeId);
        Assert.Equal(LevelId1, sent.LevelId);
        Assert.Equal("علی", sent.Search);
        Assert.Equal(PersonId, Parse(json).GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal("علی رضایی", ctx.DetailLabels[PersonId]);
    }

    [Fact]
    public async Task Search_NonDetailParameter_IsError()
    {
        var (_, step) = await Run(AgentToolbox.SearchCounterparty, """{"template_code":"PAY_PERSON","parameter_key":"amount","name":"x"}""", Ctx());
        Assert.False(step.Succeeded);
    }

    [Fact]
    public async Task Preview_ConvertsAnswers_AndBuildsProposal()
    {
        var ctx = Ctx();
        var (json, step) = await Run(AgentToolbox.PreviewVoucher, $$"""
            {"template_code":"PAY_PERSON","voucher_date":"14040708","answers":[
              {"key":"amount","value":"۵,۰۰۰"},
              {"key":"person","value":"{{PersonId}}"},
              {"key":"when","value":"1404/07/08"},
              {"key":"kind","value":"بانکی"},
              {"key":"ghost","value":"x"}]}
            """, ctx);

        Assert.True(step.Succeeded);
        Assert.True(Parse(json).GetProperty("success").GetBoolean());
        Assert.Equal(new DateOnly(2025, 9, 30), _sentPreview!.VoucherDate);
        Assert.Equal("5000", _sentPreview.Values["amount"]);
        Assert.Equal(PersonId.ToString(), _sentPreview.Values["person"]);
        Assert.Equal("2025-09-30", _sentPreview.Values["when"]);
        Assert.Equal("bank", _sentPreview.Values["kind"]);
        Assert.False(_sentPreview.Values.ContainsKey("ghost"));
        Assert.All(_sentPreview.AnswerSources!.Values, v => Assert.Equal("ai", v));
        Assert.Equal("1404", _sentPreview.Year);

        var p = ctx.Proposal!;
        Assert.Equal(_template.Id, p.TemplateId);
        Assert.Equal("14040708", p.VoucherDate);
        Assert.Equal("علی رضایی", p.Answers.Single(a => a.Key == "person").Display);
        Assert.Equal("14040708", p.Answers.Single(a => a.Key == "when").Value);
        Assert.Equal("5,000 ریال", p.Answers.Single(a => a.Key == "amount").Display);
    }

    [Fact]
    public async Task Preview_DetailByUniqueName_IsResolved()
    {
        Search(("علی رضایی", PersonId));
        await Run(AgentToolbox.PreviewVoucher, """{"template_code":"PAY_PERSON","answers":[{"key":"amount","value":"10"},{"key":"person","value":"رضایی"}]}""", Ctx(false));
        Assert.Equal(PersonId.ToString(), _sentPreview!.Values["person"]);
    }

    [Fact]
    public async Task Preview_DetailAmbiguousName_AsksWithoutCallingPreview()
    {
        Search(("علی ۱", Guid.NewGuid()), ("علی ۲", Guid.NewGuid()));
        var (json, step) = await Run(AgentToolbox.PreviewVoucher, """{"template_code":"PAY_PERSON","answers":[{"key":"person","value":"علی"}]}""", Ctx(false));
        Assert.False(step.Succeeded);
        var err = Parse(json).GetProperty("errors")[0];
        Assert.Equal("person", err.GetProperty("parameter_key").GetString());
        Assert.Equal("کدام شخص؟", err.GetProperty("ask").GetString());
        Assert.Null(_sentPreview);
    }

    [Theory]
    [InlineData("amount", "-5")]
    [InlineData("when", "1404/13/01")]
    [InlineData("kind", "چک")]
    public async Task Preview_InvalidAnswer_ReturnsError(string key, string value)
    {
        var (_, step) = await Run(AgentToolbox.PreviewVoucher, $$"""{"template_code":"PAY_PERSON","answers":[{"key":"{{key}}","value":"{{value}}"}]}""", Ctx());
        Assert.False(step.Succeeded);
        Assert.Null(_sentPreview);
    }

    [Fact]
    public async Task Preview_EngineErrors_PassedToModel_NoProposal()
    {
        _mediator.Setup(m => m.Send(It.IsAny<PreviewOperationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult(false, null, [new EngineError(EngineErrorCode.MissingParameter, "مبلغ لازم است", "amount", "مبلغ چقدر است؟")]));
        var ctx = Ctx();
        var (json, step) = await Run(AgentToolbox.PreviewVoucher, """{"template_code":"PAY_PERSON","answers":[]}""", ctx);
        Assert.False(step.Succeeded);
        Assert.Equal("amount", Parse(json).GetProperty("errors")[0].GetProperty("parameter_key").GetString());
        Assert.Null(ctx.Proposal);
    }

    [Fact]
    public async Task Preview_WithoutDataTools_HidesCounterpartyNamesFromModel()
    {
        var (json, _) = await Run(AgentToolbox.PreviewVoucher, $$"""{"template_code":"PAY_PERSON","answers":[{"key":"person","value":"{{PersonId}}"}]}""", Ctx(false));
        Assert.DoesNotContain("رضایی", json);
    }

    [Fact]
    public async Task Preview_UnknownTemplate_IsError()
    {
        var (_, step) = await Run(AgentToolbox.PreviewVoucher, """{"template_code":"NOPE","answers":[]}""", Ctx());
        Assert.False(step.Succeeded);
    }

    [Fact]
    public async Task CounterpartyBalance_ReadsTafsiliLevelUnderMoin()
    {
        _details.Setup(d => d.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DetailInfo> { [PersonId] = new(PersonId, "12", "علی رضایی", new HashSet<Guid>(), true, true) });
        GetAccountReviewQuery? sent = null;
        _mediator.Setup(m => m.Send(It.IsAny<GetAccountReviewQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<AccountReviewResultDto>, CancellationToken>((q, _) => sent = (GetAccountReviewQuery)q)
            .ReturnsAsync(new AccountReviewResultDto(
                [new AccountReviewRowDto("12", "علی رضایی", "تفصیلی ۱", 700, 200, 500, 0, false)],
                AccountReviewLevel.Tafsili1, "تفصیلی ۱", [], []));

        var (json, step) = await Run(AgentToolbox.CounterpartyBalance,
            $$"""{"template_code":"PAY_PERSON","parameter_key":"person","counterparty_id":"{{PersonId}}"}""", Ctx());

        Assert.True(step.Succeeded);
        Assert.Equal(AccountReviewLevel.Tafsili1, sent!.Level);
        Assert.Equal("110101", sent.Scope!.Single().Code);
        Assert.Equal("1404", sent.Year);
        var r = Parse(json);
        Assert.Equal(500, r.GetProperty("balance").GetDecimal());
        Assert.Equal("بدهکار", r.GetProperty("side").GetString());
    }

    [Fact]
    public async Task CounterpartyBalance_InvisibleDetail_IsError()
    {
        _details.Setup(d => d.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DetailInfo> { [PersonId] = new(PersonId, "12", "x", new HashSet<Guid>(), true, false) });
        var (_, step) = await Run(AgentToolbox.CounterpartyBalance,
            $$"""{"template_code":"PAY_PERSON","parameter_key":"person","counterparty_id":"{{PersonId}}"}""", Ctx());
        Assert.False(step.Succeeded);
        _mediator.Verify(m => m.Send(It.IsAny<GetAccountReviewQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AccountBalance_MatchesNameWithArabicLetters_AndCodePrefix()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetAccountReviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountReviewResultDto(
                [
                    new AccountReviewRowDto("110101", "صندوق ریالی", "معین", 900, 100, 800, 0, true),
                    new AccountReviewRowDto("210101", "بستانکاران", "معین", 0, 300, 0, 300, true),
                ],
                AccountReviewLevel.Moin, "معین", [], []));

        var (byName, _) = await Run(AgentToolbox.AccountBalance, """{"query":"صندوق"}""", Ctx());
        var (byCode, _) = await Run(AgentToolbox.AccountBalance, """{"query":"۲۱"}""", Ctx());

        Assert.Equal("110101", Parse(byName).GetProperty("accounts")[0].GetProperty("code").GetString());
        var credit = Parse(byCode).GetProperty("accounts")[0];
        Assert.Equal("بستانکار", credit.GetProperty("side").GetString());
        Assert.Equal(300, credit.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task RoleDenied_InsideTool_BecomesToolError()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetAccountReviewQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RoleAccessDeniedException("x"));
        var (json, step) = await Run(AgentToolbox.AccountBalance, """{"query":"صندوق"}""", Ctx());
        Assert.False(step.Succeeded);
        Assert.Contains("اجازهٔ دیدن", json);
    }

    [Fact]
    public async Task InvalidArgumentsJson_IsError()
    {
        var (_, step) = await Run(AgentToolbox.PreviewVoucher, "{not json", Ctx());
        Assert.False(step.Succeeded);
    }

    [Theory]
    [InlineData("14040708", 2025, 9, 30)]
    [InlineData("۱۴۰۳/۱۲/۳۰", 2025, 3, 20)]
    public void JalaliToDate_Converts(string jalali, int y, int m, int d)
    {
        Assert.Equal(new DateOnly(y, m, d), AgentToolbox.JalaliToDate(jalali));
        Assert.Equal(jalali.Replace("/", "").Replace("۱", "1").Replace("۴", "4").Replace("۰", "0").Replace("۳", "3").Replace("۲", "2"),
            AgentToolbox.DateToJalali(new DateOnly(y, m, d)));
    }

    [Theory]
    [InlineData("14041230")]
    [InlineData("1404070")]
    [InlineData("abc")]
    public void JalaliToDate_Invalid_IsNull(string jalali) => Assert.Null(AgentToolbox.JalaliToDate(jalali));
}
