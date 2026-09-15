using Accounting.Application.TafsilGroups.Commands.DeleteTafsilGroup;

namespace Accounting.Application.Tests.TafsilGroups.Commands.DeleteTafsilGroup;

public sealed class DeleteTafsilGroupCommandValidatorTests
{
    private readonly DeleteTafsilGroupCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteTafsilGroupCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteTafsilGroupCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteTafsilGroupCommand.Id));
    }
}
