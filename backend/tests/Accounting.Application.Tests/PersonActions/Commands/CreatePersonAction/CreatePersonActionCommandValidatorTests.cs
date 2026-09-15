using Accounting.Application.PersonActions.Commands.CreatePersonAction;

namespace Accounting.Application.Tests.PersonActions.Commands.CreatePersonAction;

public sealed class CreatePersonActionCommandValidatorTests
{
    private readonly CreatePersonActionCommandValidator _validator = new();

    private static CreatePersonActionCommand ValidCommand() => new(
        UserName: "John Doe",
        UserId: "jdoe",
        FromDate: "14030101",
        ToDate: "14031231",
        Status: true,
        OperatorRole: true,
        VahedCode: "0100");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_UserNameOverMaxLength_Fails()
    {
        var command = ValidCommand() with { UserName = new string('a', 31) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePersonActionCommand.UserName));
    }

    [Fact]
    public void Validate_UserNameAtMaxLength_Passes()
    {
        var command = ValidCommand() with { UserName = new string('a', 30) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyUserId_Fails()
    {
        var command = ValidCommand() with { UserId = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePersonActionCommand.UserId));
    }

    [Fact]
    public void Validate_UserIdOverMaxLength_Fails()
    {
        var command = ValidCommand() with { UserId = "01234567890" }; // 11 chars, max is 10

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePersonActionCommand.UserId));
    }

    [Fact]
    public void Validate_UserIdAtMaxLength_Passes()
    {
        var command = ValidCommand() with { UserId = "0123456789" }; // 10 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_FromDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { FromDate = "140301011" }; // 9 chars, max is 8

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePersonActionCommand.FromDate));
    }

    [Fact]
    public void Validate_ToDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { ToDate = "140312311" }; // 9 chars, max is 8

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePersonActionCommand.ToDate));
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { VahedCode = "01000" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePersonActionCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeAtMaxLength_Passes()
    {
        var command = ValidCommand() with { VahedCode = "0100" }; // 4 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_VahedCodeNull_Passes()
    {
        var command = ValidCommand() with { VahedCode = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
