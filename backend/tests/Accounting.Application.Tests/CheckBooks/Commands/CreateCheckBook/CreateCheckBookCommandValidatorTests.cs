using Accounting.Application.CheckBooks.Commands.CreateCheckBook;

namespace Accounting.Application.Tests.CheckBooks.Commands.CreateCheckBook;

public sealed class CreateCheckBookCommandValidatorTests
{
    private readonly CreateCheckBookCommandValidator _validator = new();

    private static CreateCheckBookCommand ValidCommand() => new(
        AccountId: Guid.NewGuid(),
        CheckBookTitle: "دسته چک اول",
        CheckBookDate: "13990101",
        FromCheckNumber: "100000",
        ToCheckNumber: "100050",
        CheckTypeId: Guid.NewGuid(),
        VahedCode: "0001",
        CheckBookType: true,
        Serial: "SER0001");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullOptionals_Passes()
    {
        var result = _validator.Validate(ValidCommand() with
        {
            CheckBookTitle = null,
            CheckTypeId = null,
            CheckBookType = null,
            Serial = null,
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.AccountId));
    }

    [Fact]
    public void Validate_CheckBookTitleTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheckBookTitle = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.CheckBookTitle));
    }

    [Fact]
    public void Validate_EmptyCheckBookDate_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheckBookDate = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.CheckBookDate));
    }

    [Fact]
    public void Validate_CheckBookDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheckBookDate = "139901011" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.CheckBookDate));
    }

    [Fact]
    public void Validate_EmptyFromCheckNumber_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { FromCheckNumber = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.FromCheckNumber));
    }

    [Fact]
    public void Validate_FromCheckNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { FromCheckNumber = new string('1', 15) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.FromCheckNumber));
    }

    [Fact]
    public void Validate_EmptyToCheckNumber_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ToCheckNumber = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.ToCheckNumber));
    }

    [Fact]
    public void Validate_ToCheckNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ToCheckNumber = new string('1', 15) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.ToCheckNumber));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.VahedCode));
    }

    [Fact]
    public void Validate_SerialTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Serial = new string('a', 21) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCheckBookCommand.Serial));
    }
}
