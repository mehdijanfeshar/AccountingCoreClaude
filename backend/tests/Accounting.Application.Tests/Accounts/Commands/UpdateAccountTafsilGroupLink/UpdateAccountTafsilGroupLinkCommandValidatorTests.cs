using Accounting.Application.Accounts.Commands.UpdateAccountTafsilGroupLink;

namespace Accounting.Application.Tests.Accounts.Commands.UpdateAccountTafsilGroupLink;

public sealed class UpdateAccountTafsilGroupLinkCommandValidatorTests
{
    private readonly UpdateAccountTafsilGroupLinkCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(
            new UpdateAccountTafsilGroupLinkCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var result = _validator.Validate(
            new UpdateAccountTafsilGroupLinkCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountTafsilGroupLinkCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_EmptyLinkId_Fails()
    {
        var result = _validator.Validate(
            new UpdateAccountTafsilGroupLinkCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountTafsilGroupLinkCommand.LinkId));
    }
}
