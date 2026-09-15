using Accounting.Application.Accounts.Commands.LinkAccountCodeToTafsilGroup;

namespace Accounting.Application.Tests.Accounts.Commands.LinkAccountCodeToTafsilGroup;

public sealed class LinkAccountCodeToTafsilGroupCommandValidatorTests
{
    private readonly LinkAccountCodeToTafsilGroupCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new LinkAccountCodeToTafsilGroupCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var result = _validator.Validate(new LinkAccountCodeToTafsilGroupCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LinkAccountCodeToTafsilGroupCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_EmptyLevelId_Fails()
    {
        var result = _validator.Validate(new LinkAccountCodeToTafsilGroupCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LinkAccountCodeToTafsilGroupCommand.LevelId));
    }

    [Fact]
    public void Validate_EmptyTafsilGroupId_Fails()
    {
        var result = _validator.Validate(new LinkAccountCodeToTafsilGroupCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LinkAccountCodeToTafsilGroupCommand.TafsilGroupId));
    }
}
