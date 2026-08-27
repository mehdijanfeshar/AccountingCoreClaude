using Accounting.Application.BillLogs.Commands.CreateBillLog;

namespace Accounting.Application.Tests.BillLogs.Commands.CreateBillLog;

public sealed class CreateBillLogCommandValidatorTests
{
    private readonly CreateBillLogCommandValidator _validator = new();

    private static CreateBillLogCommand ValidCommand() => new(
        LogDesc: "invoice processed",
        LogDate: "14030101",
        VahedCode: "0100",
        Year: "1403");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LogDescOverMaxLength_Fails()
    {
        var command = ValidCommand() with { LogDesc = new string('a', 1001) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBillLogCommand.LogDesc));
    }

    [Fact]
    public void Validate_LogDescAtMaxLength_Passes()
    {
        var command = ValidCommand() with { LogDesc = new string('a', 1000) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LogDescNull_Passes()
    {
        var command = ValidCommand() with { LogDesc = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LogDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { LogDate = "140301011" }; // 9 chars, max is 8

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBillLogCommand.LogDate));
    }

    [Fact]
    public void Validate_LogDateAtMaxLength_Passes()
    {
        var command = ValidCommand() with { LogDate = "14030101" }; // 8 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var command = ValidCommand() with { VahedCode = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBillLogCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { VahedCode = "01000" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBillLogCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeAtMaxLength_Passes()
    {
        var command = ValidCommand() with { VahedCode = "0100" }; // 4 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var command = ValidCommand() with { Year = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBillLogCommand.Year));
    }

    [Fact]
    public void Validate_YearOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Year = "14035" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBillLogCommand.Year));
    }

    [Fact]
    public void Validate_YearAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Year = "1403" }; // 4 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
