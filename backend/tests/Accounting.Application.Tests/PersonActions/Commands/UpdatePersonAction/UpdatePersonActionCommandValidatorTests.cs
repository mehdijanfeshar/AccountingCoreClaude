using Accounting.Application.PersonActions.Commands.UpdatePersonAction;

namespace Accounting.Application.Tests.PersonActions.Commands.UpdatePersonAction;

public sealed class UpdatePersonActionCommandValidatorTests
{
    private readonly UpdatePersonActionCommandValidator _validator = new();

    private static UpdatePersonActionCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
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
    public void Validate_EmptyId_Fails()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePersonActionCommand.Id));
    }

    [Fact]
    public void Validate_UserNameOverMaxLength_Fails()
    {
        var command = ValidCommand() with { UserName = new string('a', 31) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePersonActionCommand.UserName));
    }

    [Fact]
    public void Validate_EmptyUserId_Fails()
    {
        var command = ValidCommand() with { UserId = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePersonActionCommand.UserId));
    }

    [Fact]
    public void Validate_UserIdOverMaxLength_Fails()
    {
        var command = ValidCommand() with { UserId = "01234567890" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePersonActionCommand.UserId));
    }

    [Fact]
    public void Validate_FromDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { FromDate = "140301011" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePersonActionCommand.FromDate));
    }

    [Fact]
    public void Validate_ToDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { ToDate = "140312311" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePersonActionCommand.ToDate));
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { VahedCode = "01000" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePersonActionCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeAtMaxLength_Passes()
    {
        var command = ValidCommand() with { VahedCode = "0100" };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
