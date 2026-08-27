using Accounting.Application.WhiteLists.Commands.UpdateWhiteList;

namespace Accounting.Application.Tests.WhiteLists.Commands.UpdateWhiteList;

public sealed class UpdateWhiteListCommandValidatorTests
{
    private readonly UpdateWhiteListCommandValidator _validator = new();

    private static UpdateWhiteListCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        AccountCodeId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        VahedInfoId: Guid.NewGuid(),
        FromAuthorizedDate: "14040101",
        ToAuthorizedDate: "14041231",
        FromLimitationDate: "14040101",
        ToLimitationDate: "14041231");

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWhiteListCommand.Id));
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var command = ValidCommand() with { AccountCodeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWhiteListCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_FromAuthorizedDateOverMaxLength_Fails()
    {
        var command = ValidCommand() with { FromAuthorizedDate = "140401011" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWhiteListCommand.FromAuthorizedDate));
    }

    [Fact]
    public void Validate_ToAuthorizedDateAtMaxLength_Passes()
    {
        var command = ValidCommand() with { ToAuthorizedDate = "14041231" };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
