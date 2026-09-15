using Accounting.Application.TafsilGroups.Commands.UpdateTafsilGroup;

namespace Accounting.Application.Tests.TafsilGroups.Commands.UpdateTafsilGroup;

public sealed class UpdateTafsilGroupCommandValidatorTests
{
    private readonly UpdateTafsilGroupCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new UpdateTafsilGroupCommand(Guid.NewGuid(), "001", "گروه تفصیلی یک", null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new UpdateTafsilGroupCommand(Guid.Empty, "001", "گروه تفصیلی یک", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsilGroupCommand.Id));
    }

    [Fact]
    public void Validate_TafsilGroupCodeTooLong_Fails()
    {
        var result = _validator.Validate(new UpdateTafsilGroupCommand(Guid.NewGuid(), "0001", "گروه تفصیلی یک", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsilGroupCommand.TafsilGroupCode));
    }

    [Fact]
    public void Validate_TafsilGroupNameTooLong_Fails()
    {
        var result = _validator.Validate(new UpdateTafsilGroupCommand(Guid.NewGuid(), "001", new string('a', 201), null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsilGroupCommand.TafsilGroupName));
    }
}
