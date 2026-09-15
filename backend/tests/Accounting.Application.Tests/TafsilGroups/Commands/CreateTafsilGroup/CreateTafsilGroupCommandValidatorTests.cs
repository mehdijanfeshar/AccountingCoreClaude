using Accounting.Application.TafsilGroups.Commands.CreateTafsilGroup;

namespace Accounting.Application.Tests.TafsilGroups.Commands.CreateTafsilGroup;

public sealed class CreateTafsilGroupCommandValidatorTests
{
    private readonly CreateTafsilGroupCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new CreateTafsilGroupCommand("001", "گروه تفصیلی یک", true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullPersonType_Passes()
    {
        var result = _validator.Validate(new CreateTafsilGroupCommand("001", "گروه تفصیلی یک", null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTafsilGroupCode_Fails()
    {
        var result = _validator.Validate(new CreateTafsilGroupCommand("", "گروه تفصیلی یک", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsilGroupCommand.TafsilGroupCode));
    }

    [Fact]
    public void Validate_TafsilGroupCodeTooLong_Fails()
    {
        var result = _validator.Validate(new CreateTafsilGroupCommand("0001", "گروه تفصیلی یک", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsilGroupCommand.TafsilGroupCode));
    }

    [Fact]
    public void Validate_EmptyTafsilGroupName_Fails()
    {
        var result = _validator.Validate(new CreateTafsilGroupCommand("001", "", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsilGroupCommand.TafsilGroupName));
    }

    [Fact]
    public void Validate_TafsilGroupNameTooLong_Fails()
    {
        var result = _validator.Validate(new CreateTafsilGroupCommand("001", new string('a', 201), null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsilGroupCommand.TafsilGroupName));
    }

    [Fact]
    public void Validate_TafsilGroupNameAtMaxLength_Passes()
    {
        var result = _validator.Validate(new CreateTafsilGroupCommand("001", new string('a', 200), null));

        Assert.True(result.IsValid);
    }
}
