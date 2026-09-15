using Accounting.Application.Rabets.Commands.CreateRabet;

namespace Accounting.Application.Tests.Rabets.Commands.CreateRabet;

public sealed class CreateRabetCommandValidatorTests
{
    private readonly CreateRabetCommandValidator _validator = new();

    [Fact]
    public void Validate_BothIdsPresent_Passes()
    {
        var result = _validator.Validate(new CreateRabetCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_BothIdsNull_Passes()
    {
        // No surface-level rule exists for these optional FKs — see the validator's XML doc.
        var result = _validator.Validate(new CreateRabetCommand(null, null));

        Assert.True(result.IsValid);
    }
}
