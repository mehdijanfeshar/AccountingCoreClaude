using Accounting.Application.BillLogs.Commands.UpdateBillLog;

namespace Accounting.Application.Tests.BillLogs.Commands.UpdateBillLog;

public sealed class UpdateBillLogCommandValidatorTests
{
    private readonly UpdateBillLogCommandValidator _validator = new();

    private static UpdateBillLogCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        LogDesc: "desc",
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
    public void Validate_EmptyId_Fails()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBillLogCommand.Id));
    }

    [Fact]
    public void Validate_LogDescOverMaxLength_Fails()
    {
        var command = ValidCommand() with { LogDesc = new string('a', 1001) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBillLogCommand.LogDesc));
    }

    [Fact]
    public void Validate_LogDescAtMaxLength_Passes()
    {
        var command = ValidCommand() with { LogDesc = new string('a', 1000) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LogDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { LogDate = "140301011" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBillLogCommand.LogDate));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var command = ValidCommand() with { VahedCode = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBillLogCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { VahedCode = "01000" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBillLogCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var command = ValidCommand() with { Year = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBillLogCommand.Year));
    }

    [Fact]
    public void Validate_YearOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Year = "14035" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBillLogCommand.Year));
    }

    [Fact]
    public void Validate_YearAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Year = "1403" };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
