using Accounting.Application.Accounts.Commands.UnlinkAccountCodeFromTafsilGroup;

namespace Accounting.Application.Tests.Accounts.Commands.UnlinkAccountCodeFromTafsilGroup;

public sealed class UnlinkAccountCodeFromTafsilGroupCommandValidatorTests
{
    private readonly UnlinkAccountCodeFromTafsilGroupCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new UnlinkAccountCodeFromTafsilGroupCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var result = _validator.Validate(new UnlinkAccountCodeFromTafsilGroupCommand(Guid.Empty, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UnlinkAccountCodeFromTafsilGroupCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_EmptyLinkId_Fails()
    {
        var result = _validator.Validate(new UnlinkAccountCodeFromTafsilGroupCommand(Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UnlinkAccountCodeFromTafsilGroupCommand.LinkId));
    }
}
