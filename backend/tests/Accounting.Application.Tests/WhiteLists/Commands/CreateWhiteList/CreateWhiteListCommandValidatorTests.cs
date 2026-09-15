using Accounting.Application.WhiteLists.Commands.CreateWhiteList;

namespace Accounting.Application.Tests.WhiteLists.Commands.CreateWhiteList;

public sealed class CreateWhiteListCommandValidatorTests
{
    private readonly CreateWhiteListCommandValidator _validator = new();

    private static CreateWhiteListCommand ValidCommand() => new(
        AccountCodeId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        VahedInfoId: Guid.NewGuid(),
        FromAuthorizedDate: "14030101",
        ToAuthorizedDate: "14031231",
        FromLimitationDate: "14030101",
        ToLimitationDate: "14031231");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var command = ValidCommand() with { AccountCodeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWhiteListCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_FromAuthorizedDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { FromAuthorizedDate = "140301011" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWhiteListCommand.FromAuthorizedDate));
    }

    [Fact]
    public void Validate_FromAuthorizedDateAtMaxLength_Passes()
    {
        var command = ValidCommand() with { FromAuthorizedDate = "14030101" };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ToAuthorizedDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { ToAuthorizedDate = "140312311" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWhiteListCommand.ToAuthorizedDate));
    }

    [Fact]
    public void Validate_FromLimitationDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { FromLimitationDate = "140301011" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWhiteListCommand.FromLimitationDate));
    }

    [Fact]
    public void Validate_ToLimitationDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { ToLimitationDate = "140312311" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWhiteListCommand.ToLimitationDate));
    }

    [Fact]
    public void Validate_AllDateFieldsNull_Passes()
    {
        var command = ValidCommand() with
        {
            FromAuthorizedDate = null,
            ToAuthorizedDate = null,
            FromLimitationDate = null,
            ToLimitationDate = null,
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
