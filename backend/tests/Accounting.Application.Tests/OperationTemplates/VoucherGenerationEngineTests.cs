using Accounting.Application.OperationTemplates;
using Accounting.Domain.OperationTemplates;
using Moq;
using static Accounting.Application.Tests.OperationTemplates.TemplateBuilders;

namespace Accounting.Application.Tests.OperationTemplates;

public class VoucherGenerationEngineTests
{
    private readonly Mock<ISubsidiaryAccountReader> _accounts = new();
    private readonly Mock<IDetailReader> _details = new();
    private static readonly DateOnly Date = new(2026, 3, 21);

    public VoucherGenerationEngineTests()
    {
        Accts(Account(Acc1, "صندوق"), Account(Acc2, "درآمد"));
        Dets();
    }

    private void Accts(params SubsidiaryAccountInfo[] a)
        => _accounts.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(a.ToDictionary(i => i.Id));

    private void Dets(params DetailInfo[] d)
        => _details.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(d.ToDictionary(i => i.Id));

    private Task<EngineResult> Run(OperationTemplate t, params (string, string?)[] values)
        => new VoucherGenerationEngine(_accounts.Object, _details.Object)
            .GenerateAsync(t, "0002", Date, values.ToDictionary(v => v.Item1, v => v.Item2), default);

    private static DetailInfo Detail(Guid id, string title = "علی", bool active = true, bool visible = true, params Guid[] groups)
        => new(id, "1", title, groups.ToHashSet(), active, visible);

    [Fact]
    public async Task InactiveTemplate_Fails()
    {
        var t = Simple(); t.IsActive = false;
        var r = await Run(t, ("amount", "1000"));
        Assert.Equal(EngineErrorCode.TemplateInactive, r.Errors.Single().Code);
    }

    [Fact]
    public async Task HappyPath_BalancedDraft_WithFormattedDescription()
    {
        var r = await Run(Simple(), ("amount", "1500000"));
        Assert.True(r.IsSuccess);
        Assert.Equal(1_500_000, r.Draft!.TotalDebit);
        Assert.Equal(r.Draft.TotalDebit, r.Draft.TotalCredit);
        Assert.Equal("دریافت 1,500,000 ریال", r.Draft.Description);
        Assert.Equal(2, r.Draft.Lines.Count);
        Assert.Equal("RECEIVE_CASH", r.Draft.SourceTemplateCode);
    }

    [Fact]
    public async Task MissingRequiredParameter_ReturnsAskPrompt()
    {
        var r = await Run(Simple());
        var e = r.Errors.Single();
        Assert.Equal(EngineErrorCode.MissingParameter, e.Code);
        Assert.Equal("amount", e.ParameterKey);
        Assert.Equal("مبلغ چقدر است؟", e.AskPrompt);
    }

    [Fact]
    public async Task UnknownParameter_Reported()
    {
        var r = await Run(Simple(), ("amount", "10"), ("zzz", "1"));
        Assert.Contains(r.Errors, e => e.Code == EngineErrorCode.UnknownParameter && e.ParameterKey == "zzz");
    }

    [Theory]
    [InlineData("12.5")]
    [InlineData("-5")]
    [InlineData("abc")]
    public async Task NonIntegerAmount_Invalid(string raw)
        => Assert.Equal(EngineErrorCode.InvalidParameterValue, (await Run(Simple(), ("amount", raw))).Errors.Single().Code);

    [Fact]
    public async Task ZeroAmount_NonPositive()
        => Assert.Equal(EngineErrorCode.NonPositiveAmount, (await Run(Simple(), ("amount", "0"))).Errors.Single().Code);

    [Fact]
    public async Task PercentLinesWithBalancingLine_ProducesBalancedDraft()
    {
        var t = Simple();
        t.Lines = [
            Line(LineSide.Debit, Acc1, percent: 33.33m, sort: 1),
            Line(LineSide.Debit, Acc1, balancing: true, sort: 2),
            Line(LineSide.Credit, Acc2, percent: 100m, sort: 3),
        ];
        var r = await Run(t, ("amount", "1000"));
        Assert.True(r.IsSuccess);
        Assert.Equal(1000, r.Draft!.TotalCredit);
        Assert.Equal(1000, r.Draft.TotalDebit);
        Assert.Equal(333, r.Draft.Lines[0].Debit);   // 333.3 گرد شد
        Assert.Equal(667, r.Draft.Lines[1].Debit);   // باقی‌مانده
    }

    [Fact]
    public async Task BalancingLineWithNoGap_Unbalanced()
    {
        var t = Simple();
        t.Lines = [
            Line(LineSide.Debit, Acc1, sort: 1),
            Line(LineSide.Credit, Acc2, sort: 2),
            Line(LineSide.Debit, Acc1, balancing: true, sort: 3),
        ];
        var r = await Run(t, ("amount", "1000"));
        Assert.Equal(EngineErrorCode.Unbalanced, r.Errors.Single().Code);
    }

    [Fact]
    public async Task UnbalancedWithoutBalancingLine_Fails()
    {
        var t = Simple();
        t.Lines[1].Percent = 50;
        Assert.Equal(EngineErrorCode.Unbalanced, (await Run(t, ("amount", "1000"))).Errors.Single().Code);
    }

    [Fact]
    public async Task AccountNotFoundOrInactive_Fail()
    {
        Accts(Account(Acc1));
        Assert.Equal(EngineErrorCode.AccountNotFound, (await Run(Simple(), ("amount", "10"))).Errors.Single().Code);

        Accts(Account(Acc1, active: false), Account(Acc2));
        Assert.Equal(EngineErrorCode.AccountInactive, (await Run(Simple(), ("amount", "10"))).Errors.Single().Code);
    }

    [Fact]
    public async Task OptionalAmountEmpty_SkipsLine()
    {
        var t = Simple();
        t.Parameters.Add(Amount("fee", required: false, sort: 2));
        t.Lines.Add(Line(LineSide.Debit, Acc1, "fee", sort: 3));
        t.Lines.Add(Line(LineSide.Credit, Acc2, "fee", sort: 4));
        var r = await Run(t, ("amount", "1000"));
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Draft!.Lines.Count);

        r = await Run(t, ("amount", "1000"), ("fee", "50"));
        Assert.Equal(4, r.Draft!.Lines.Count);
    }

    [Fact]
    public async Task PersianDigitsNormalizedBeforeEngine_ViaInputNormalizer()
    {
        var t = Simple();
        var norm = InputNormalizer.NormalizeAll(t, new Dictionary<string, string?> { ["amount"] = "۱,۲۳۴,۰۰۰" });
        var r = await new VoucherGenerationEngine(_accounts.Object, _details.Object).GenerateAsync(t, "0002", Date, norm, default);
        Assert.Equal(1_234_000, r.Draft!.TotalDebit);
    }

    // ---------------- تفصیلی
    private OperationTemplate WithDetail(bool required = true)
    {
        var t = Simple();
        t.Parameters.Add(DetailParam(required: required));
        t.Lines[0].Details = [ParamDetail(1, "person")];
        Accts(Account(Acc1, "الف", true, Level(1, required, Group1)), Account(Acc2));
        return t;
    }

    [Fact]
    public async Task Detail_InvalidGuid_Fails()
        => Assert.Equal(EngineErrorCode.InvalidParameterValue, (await Run(WithDetail(), ("amount", "10"), ("person", "x"))).Errors.Single().Code);

    [Fact]
    public async Task Detail_NotFound_Fails()
    {
        var r = await Run(WithDetail(), ("amount", "10"), ("person", Guid.NewGuid().ToString()));
        Assert.Equal(EngineErrorCode.DetailNotFound, r.Errors.Single().Code);
    }

    [Fact]
    public async Task Detail_InactiveOrNotInUnit_Fails()
    {
        var id = Guid.NewGuid();
        Dets(Detail(id, active: false, groups: Group1));
        var t = WithDetail();
        Assert.Contains((await Run(t, ("amount", "10"), ("person", id.ToString()))).Errors, e => e.Code == EngineErrorCode.DetailInactive);

        Dets(Detail(id, visible: false, groups: Group1));
        Assert.Contains((await Run(t, ("amount", "10"), ("person", id.ToString()))).Errors, e => e.Code == EngineErrorCode.DetailNotInUnit);
    }

    [Fact]
    public async Task Detail_WrongGroup_Fails()
    {
        var id = Guid.NewGuid();
        Dets(Detail(id, groups: Guid.NewGuid()));
        var r = await Run(WithDetail(), ("amount", "10"), ("person", id.ToString()));
        Assert.Contains(r.Errors, e => e.Code == EngineErrorCode.DetailWrongGroup);
    }

    [Fact]
    public async Task Detail_Valid_AttachedToLineWithLevelId()
    {
        var id = Guid.NewGuid();
        Dets(Detail(id, "علی", groups: Group1));
        var r = await Run(WithDetail(), ("amount", "10"), ("person", id.ToString()));
        Assert.True(r.IsSuccess);
        var d = r.Draft!.Lines[0].Details.Single();
        Assert.Equal(1, d.Level);
        Assert.Equal(id, d.DetailId);
        Assert.Equal(LevelId1, d.LevelId);
    }

    [Fact]
    public async Task RequiredLevelWithEmptyOptionalParameter_MissingRequiredDetailLevel()
    {
        var t = WithDetail(required: false);
        Accts(Account(Acc1, "الف", true, Level(1, true, Group1)), Account(Acc2));
        var r = await Run(t, ("amount", "10"));
        Assert.Contains(r.Errors, e => e.Code == EngineErrorCode.MissingRequiredDetailLevel);
    }

    [Fact]
    public async Task FixedDetail_InactiveRejected_AndLevelNotDefinedRejected()
    {
        var id = Guid.NewGuid();
        var t = Simple();
        t.Lines[0].Details = [FixedDetail(1, id)];
        Accts(Account(Acc1, "الف", true, Level(1, false, Group1)), Account(Acc2));
        Dets(Detail(id, active: false, groups: Group1));
        Assert.Contains((await Run(t, ("amount", "10"))).Errors, e => e.Code == EngineErrorCode.DetailInactive);

        t.Lines[0].Details = [FixedDetail(3, id)];
        Assert.Contains((await Run(t, ("amount", "10"))).Errors, e => e.Code == EngineErrorCode.DetailLevelNotDefined);
    }

    [Fact]
    public async Task DescriptionRemovesRepeatedWords()
    {
        var t = Simple();
        t.Parameters.Add(TextParam());
        t.VoucherDescriptionPattern = "خرید دارو {note1}";
        var r = await Run(t, ("amount", "10"), ("note1", "خرید دارو از شرکت هجرت"));
        Assert.Equal("خرید دارو از شرکت هجرت", r.Draft!.Description);
    }

    [Fact]
    public async Task EmptyOptionalPlaceholder_TrailingSeparatorTrimmed()
    {
        var t = Simple();
        t.Parameters.Add(TextParam());
        t.VoucherDescriptionPattern = "دریافت وجه - {note1}";
        var r = await Run(t, ("amount", "10"));
        Assert.Equal("دریافت وجه", r.Draft!.Description);
    }

    [Fact]
    public async Task DateParameter_RenderedAsJalali_InvalidFormatRejected()
    {
        var t = Simple();
        t.Parameters.Add(new TemplateParameter { Key = "date1", Title = "تاریخ", Type = ParameterType.Date, AskPrompt = "؟", SortOrder = 5 });
        t.VoucherDescriptionPattern = "دریافت {date1}";
        var r = await Run(t, ("amount", "10"), ("date1", "2026-03-21"));
        Assert.Equal("دریافت 1405/01/01", r.Draft!.Description);

        r = await Run(t, ("amount", "10"), ("date1", "21/03/2026"));
        Assert.Equal(EngineErrorCode.InvalidParameterValue, r.Errors.Single().Code);
    }
}

public class TemplateSupportTests
{
    [Theory]
    [InlineData("۱۲۳", ParameterType.Text, "123")]
    [InlineData("٠١٢", ParameterType.Text, "012")]
    [InlineData(" 1,234 ", ParameterType.Amount, "1234")]
    [InlineData("۱٬۲۳۴", ParameterType.Amount, "1234")]
    [InlineData("1,234", ParameterType.Text, "1,234")]
    public void Normalize_DigitsAndThousandSeparators(string raw, ParameterType type, string expected)
        => Assert.Equal(expected, InputNormalizer.Normalize(raw, type));

    [Fact]
    public void Normalize_NullStaysNull()
        => Assert.Null(InputNormalizer.Normalize(null, ParameterType.Amount));

    [Fact]
    public void NormalizeAll_NullValues_ReturnsEmpty()
        => Assert.Empty(InputNormalizer.NormalizeAll(TemplateBuilders.Simple(), null));

    [Fact]
    public void VoucherDate_DateParameterWins_ElseRequested_ElseToday()
    {
        var t = TemplateBuilders.Simple();
        var today = new DateOnly(2026, 5, 5);
        var requested = new DateOnly(2026, 4, 4);
        var empty = new Dictionary<string, string?>();
        Assert.Equal(today, TemplateVoucherDate.Resolve(t, empty, null, today));
        Assert.Equal(requested, TemplateVoucherDate.Resolve(t, empty, requested, today));

        t.Parameters.Add(new TemplateParameter { Key = "date1", Type = ParameterType.Date, SortOrder = 9 });
        Assert.Equal(new DateOnly(2026, 1, 2),
            TemplateVoucherDate.Resolve(t, new Dictionary<string, string?> { ["date1"] = "2026-01-02" }, requested, today));
        // تاریخ نامعتبر ⇒ به requested برمی‌گردد
        Assert.Equal(requested,
            TemplateVoucherDate.Resolve(t, new Dictionary<string, string?> { ["date1"] = "bad" }, requested, today));
    }

    [Fact]
    public void FiscalYearGuard_JalaliYearAndCheck()
    {
        var d = new DateOnly(2026, 3, 21); // 1405/01/01
        Assert.Equal("1405", FiscalYearGuard.JalaliYear(d));
        Assert.Equal("1405/01/01", FiscalYearGuard.Jalali(d));
        Assert.Null(FiscalYearGuard.Check(d, null));
        Assert.Null(FiscalYearGuard.Check(d, " "));
        Assert.Null(FiscalYearGuard.Check(d, " 1405 "));
        var err = FiscalYearGuard.Check(d, "1404");
        Assert.Equal("voucherDate", err!.ParameterKey);
        Assert.Equal(EngineErrorCode.InvalidParameterValue, err.Code);
    }
}
