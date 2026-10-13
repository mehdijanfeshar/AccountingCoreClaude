using Accounting.Application.FinancialStatements.Periods;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.FinancialStatements.Periods;

public sealed class TransitionFsPeriodCommandValidatorTests
{
    private readonly TransitionFsPeriodCommandValidator _sut = new();

    private static TransitionFsPeriodCommand Cmd(string unit = "101", string year = "1404", FsPeriodAction action = FsPeriodAction.Close, string? reason = null)
        => new(unit, year, action, reason);

    [Fact]
    public void Valid_Close_WithoutReason_Passes() => Assert.True(_sut.Validate(Cmd()).IsValid);

    [Theory]
    [InlineData("1404")]
    [InlineData("1399")]
    [InlineData("1310")]
    public void ValidYears_Pass(string year) => Assert.True(_sut.Validate(Cmd(year: year)).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("2025")]
    [InlineData("140")]
    [InlineData("14040")]
    [InlineData("14a4")]
    public void InvalidYears_Fail(string year) => Assert.False(_sut.Validate(Cmd(year: year)).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    public void InvalidUnit_Fails(string unit) => Assert.False(_sut.Validate(Cmd(unit: unit)).IsValid);

    [Theory]
    [InlineData(FsPeriodAction.RequestReopen)]
    [InlineData(FsPeriodAction.Reopen)]
    [InlineData(FsPeriodAction.RejectReopen)]
    public void ActionsNeedingReason_FailWithoutReason(FsPeriodAction action)
    {
        Assert.False(_sut.Validate(Cmd(action: action)).IsValid);
        Assert.False(_sut.Validate(Cmd(action: action, reason: "  ")).IsValid);
        Assert.True(_sut.Validate(Cmd(action: action, reason: "دلیل")).IsValid);
    }

    [Theory]
    [InlineData(FsPeriodAction.Close)]
    [InlineData(FsPeriodAction.Lock)]
    [InlineData(FsPeriodAction.ApproveReopen)]
    public void OtherActions_DoNotNeedReason(FsPeriodAction action) => Assert.True(_sut.Validate(Cmd(action: action)).IsValid);

    [Fact]
    public void UndefinedAction_Fails() => Assert.False(_sut.Validate(Cmd(action: (FsPeriodAction)99)).IsValid);

    [Fact]
    public void ReasonLongerThan1000_Fails() => Assert.False(_sut.Validate(Cmd(reason: new string('x', 1001))).IsValid);
}
