using Accounting.Application.RevolvingFunds.Commands.CreateRevolvingFund;

namespace Accounting.Application.Tests.RevolvingFunds.Commands.CreateRevolvingFund;

public sealed class CreateRevolvingFundCommandValidatorTests
{
    private readonly CreateRevolvingFundCommandValidator _validator = new();

    private static CreateRevolvingFundCommand ValidCommand() => new(
        Code: "01",
        Name: "تنخواه شماره یک",
        Description: "توضیحات",
        DefaultAmount: 1000000m,
        AccountCodeId: Guid.NewGuid(),
        VahedCode: "0001",
        Year: "1404");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullOptionalFields_Passes()
    {
        var result = _validator.Validate(ValidCommand() with
        {
            Description = null,
            DefaultAmount = null,
            AccountCodeId = null,
            VahedCode = null,
            Year = null,
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Code = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRevolvingFundCommand.Code));
    }

    [Fact]
    public void Validate_CodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Code = "abc" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRevolvingFundCommand.Code));
    }

    [Fact]
    public void Validate_CodeAtMaxLength_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { Code = "99" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Name = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRevolvingFundCommand.Name));
    }

    [Fact]
    public void Validate_NameTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Name = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRevolvingFundCommand.Name));
    }

    [Fact]
    public void Validate_DescriptionTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Description = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRevolvingFundCommand.Description));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRevolvingFundCommand.VahedCode));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14045" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRevolvingFundCommand.Year));
    }
}
