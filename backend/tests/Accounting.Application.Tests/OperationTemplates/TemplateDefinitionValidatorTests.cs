using Accounting.Application.OperationTemplates;
using Accounting.Domain.OperationTemplates;
using Moq;
using static Accounting.Application.Tests.OperationTemplates.TemplateBuilders;

namespace Accounting.Application.Tests.OperationTemplates;

public class TemplateDefinitionValidatorTests
{
    private readonly Mock<ISubsidiaryAccountReader> _accounts = new();

    public TemplateDefinitionValidatorTests()
    {
        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, SubsidiaryAccountInfo>
            {
                [Acc1] = Account(Acc1), [Acc2] = Account(Acc2),
            });
    }

    private Task<IReadOnlyList<string>> Validate(OperationTemplate t)
        => new TemplateDefinitionValidator(_accounts.Object).ValidateAsync(t, default);

    [Fact]
    public async Task ValidTemplate_NoErrors()
        => Assert.Empty(await Validate(Simple()));

    [Theory]
    [InlineData("")]
    [InlineData("lower_case")]
    [InlineData("AB")]
    [InlineData("1ABC")]
    public async Task BadCode_Reported(string code)
    {
        var t = Simple();
        t.Code = code;
        Assert.Contains(await Validate(t), e => e.Contains("کد الگو"));
    }

    [Fact]
    public async Task MissingTitleDescriptionPattern_Reported()
    {
        var t = Simple();
        t.Title = ""; t.Description = " "; t.VoucherDescriptionPattern = "";
        var e = await Validate(t);
        Assert.Contains(e, x => x.Contains("عنوان الگو"));
        Assert.Contains(e, x => x.Contains("توضیح الگو"));
        Assert.Contains(e, x => x.Contains("شرح سند"));
    }

    [Fact]
    public async Task MissingDebitOrCreditSide_Reported()
    {
        var t = Simple();
        t.Lines = [Line(LineSide.Debit, Acc1)];
        Assert.Contains(await Validate(t), e => e.Contains("بدهکار و یک ردیف بستانکار"));
    }

    [Fact]
    public async Task DuplicateParameterKey_Reported()
    {
        var t = Simple();
        t.Parameters.Add(Amount("AMOUNT", sort: 2)); // مقایسه بدون حساسیت به حروف
        Assert.Contains(await Validate(t), e => e.Contains("تکراری"));
    }

    [Fact]
    public async Task InvalidParameterKey_Reported()
    {
        var t = Simple();
        t.Parameters.Add(Amount("1bad", sort: 2));
        Assert.Contains(await Validate(t), e => e.Contains("نامعتبر"));
    }

    [Fact]
    public async Task DetailParameterWithoutGroup_AndNonDetailWithGroup_Reported()
    {
        var t = Simple();
        var d = DetailParam(); d.DetailGroupId = null;
        var a = Amount("amt2", sort: 3); a.DetailGroupId = Group1;
        t.Parameters.AddRange([d, a]);
        var e = await Validate(t);
        Assert.Contains(e, x => x.Contains("باید گروه تفصیلی داشته باشد"));
        Assert.Contains(e, x => x.Contains("نباید گروه تفصیلی داشته باشد"));
    }

    [Fact]
    public async Task LineAmountParameterMissingOrNotAmount_Reported()
    {
        var t = Simple();
        t.Lines[0].AmountParameterKey = "nope";
        Assert.Contains(await Validate(t), e => e.Contains("پارامتر مبلغ"));

        t = Simple();
        t.Parameters.Add(TextParam());
        t.Lines[0].AmountParameterKey = "note1";
        Assert.Contains(await Validate(t), e => e.Contains("پارامتر مبلغ"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1001)]
    public async Task PercentOutOfRange_Reported(double percent)
    {
        var t = Simple();
        t.Lines[0].Percent = (decimal)percent;
        Assert.Contains(await Validate(t), e => e.Contains("درصد"));
    }

    [Fact]
    public async Task MoreThanOneBalancingLine_Reported()
    {
        var t = Simple();
        t.Lines.Add(Line(LineSide.Debit, Acc1, balancing: true, sort: 3));
        t.Lines.Add(Line(LineSide.Credit, Acc2, balancing: true, sort: 4));
        Assert.Contains(await Validate(t), e => e.Contains("یک ردیف تراز"));
    }

    [Fact]
    public async Task PercentLineWithoutBalancingLine_Reported()
    {
        var t = Simple();
        t.Lines[0].Percent = 60; t.Lines[1].Percent = 60;
        Assert.Contains(await Validate(t), e => e.Contains("ردیف تراز‌کننده"));
    }

    [Fact]
    public async Task UnequalPercentSumsPerAmount_Reported()
    {
        var t = Simple();
        t.Lines.Add(Line(LineSide.Debit, Acc1, sort: 3)); // بدهکار ۲۰۰، بستانکار ۱۰۰
        Assert.Contains(await Validate(t), e => e.Contains("برابر نیست"));
    }

    [Fact]
    public async Task BalancingLineWithPartialPercents_IsAccepted()
    {
        var t = Simple();
        t.Lines[0].Percent = 70; t.Lines[1].Percent = 100;
        t.Lines.Add(Line(LineSide.Debit, Acc1, balancing: true, sort: 3));
        Assert.Empty(await Validate(t));
    }

    [Fact]
    public async Task DescriptionPlaceholderUnknown_Reported()
    {
        var t = Simple();
        t.VoucherDescriptionPattern = "دریافت {ghost}";
        Assert.Contains(await Validate(t), e => e.Contains("ghost"));

        t = Simple();
        t.Lines[0].DescriptionPattern = "x {ghost2}";
        Assert.Contains(await Validate(t), e => e.Contains("ghost2"));
    }

    [Fact]
    public async Task LineDetail_NeitherOrBothParameterAndFixed_Reported()
    {
        var t = Simple();
        t.Parameters.Add(DetailParam());
        t.Lines[0].Details = [new TemplateLineDetail { Level = 1 }];
        Assert.Contains(await Validate(t), e => e.Contains("دقیقاً یکی"));

        t = Simple();
        t.Parameters.Add(DetailParam());
        t.Lines[0].Details = [new TemplateLineDetail { Level = 1, ParameterKey = "person", FixedDetailId = Guid.NewGuid() }];
        Assert.Contains(await Validate(t), e => e.Contains("دقیقاً یکی"));
    }

    [Fact]
    public async Task LineDetail_DuplicateLevelOrNonDetailParameter_Reported()
    {
        var t = Simple();
        t.Parameters.Add(DetailParam());
        t.Lines[0].Details = [ParamDetail(1, "person"), ParamDetail(1, "person")];
        Assert.Contains(await Validate(t), e => e.Contains("سطح تفصیلی تکراری"));

        t = Simple();
        t.Lines[0].Details = [ParamDetail(1, "amount")];
        Assert.Contains(await Validate(t), e => e.Contains("تفصیلی نیست"));
    }

    [Fact]
    public async Task StructuralErrors_SkipDatabaseChecks()
    {
        var t = Simple();
        t.Title = "";
        await Validate(t);
        _accounts.Verify(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AccountMissingOrInactive_Reported()
    {
        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, SubsidiaryAccountInfo> { [Acc1] = Account(Acc1, "غیرفعال", active: false) });
        var e = await Validate(Simple());
        Assert.Contains(e, x => x.Contains("غیرفعال"));
        Assert.Contains(e, x => x.Contains("وجود ندارد"));
    }

    [Fact]
    public async Task DetailLevelNotDefinedOnAccount_Reported()
    {
        var t = Simple();
        t.Parameters.Add(DetailParam());
        t.Lines[0].Details = [ParamDetail(2, "person")];
        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, SubsidiaryAccountInfo>
            {
                [Acc1] = Account(Acc1, "الف", true, Level(1, false, Group1)), [Acc2] = Account(Acc2),
            });
        Assert.Contains(await Validate(t), e => e.Contains("سطح تفصیلی 2 ندارد"));
    }

    [Fact]
    public async Task DetailGroupMismatch_Reported()
    {
        var t = Simple();
        t.Parameters.Add(DetailParam(group: Group1));
        t.Lines[0].Details = [ParamDetail(1, "person")];
        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, SubsidiaryAccountInfo>
            {
                [Acc1] = Account(Acc1, "الف", true, Level(1, false, Guid.NewGuid())), [Acc2] = Account(Acc2),
            });
        Assert.Contains(await Validate(t), e => e.Contains("یکی نیست"));
    }

    [Fact]
    public async Task RequiredLevelNotMappedOrMappedToOptionalParameter_Reported()
    {
        var t = Simple();
        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, SubsidiaryAccountInfo>
            {
                [Acc1] = Account(Acc1, "الف", true, Level(1, true, Group1)), [Acc2] = Account(Acc2),
            });
        Assert.Contains(await Validate(t), e => e.Contains("الزامی است ولی در الگو تعیین نشده"));

        t = Simple();
        t.Parameters.Add(DetailParam(required: false));
        t.Lines[0].Details = [ParamDetail(1, "person")];
        Assert.Contains(await Validate(t), e => e.Contains("پارامتر اختیاری"));
    }

    [Fact]
    public async Task RequiredLevelMappedToRequiredParameter_Valid()
    {
        var t = Simple();
        t.Parameters.Add(DetailParam());
        t.Lines[0].Details = [ParamDetail(1, "person")];
        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, SubsidiaryAccountInfo>
            {
                [Acc1] = Account(Acc1, "الف", true, Level(1, true, Group1)), [Acc2] = Account(Acc2),
            });
        Assert.Empty(await Validate(t));
    }
}
