using Accounting.Application.RevolvingFunds.Commands.UpdateRevolvingFund;

namespace Accounting.Application.Tests.RevolvingFunds.Commands.UpdateRevolvingFund;

public sealed class UpdateRevolvingFundCommandValidatorTests
{
    private readonly UpdateRevolvingFundCommandValidator _validator = new();

    private static UpdateRevolvingFundCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        Code: "02",
        Name: "تنخواه به‌روزشده",
        Description: "توضیحات",
        DefaultAmount: 2000000m,
        AccountCodeId: Guid.NewGuid(),
        VahedCode: "0002",
        Year: "1405");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRevolvingFundCommand.Id));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRevolvingFundCommand.Code));
    }

    [Fact]
    public void Validate_CodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Code = "abc" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRevolvingFundCommand.Code));
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Name = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRevolvingFundCommand.Name));
    }

    [Fact]
    public void Validate_NameTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Name = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRevolvingFundCommand.Name));
    }

    [Fact]
    public void Validate_DescriptionTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Description = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRevolvingFundCommand.Description));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00002" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRevolvingFundCommand.VahedCode));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14055" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRevolvingFundCommand.Year));
    }
}
