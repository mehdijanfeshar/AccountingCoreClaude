using Accounting.Application.Tafsilis.Commands.DeleteTafsili;

namespace Accounting.Application.Tests.Tafsilis.Commands.DeleteTafsili;

public sealed class DeleteTafsiliCommandValidatorTests
{
    private readonly DeleteTafsiliCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteTafsiliCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteTafsiliCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteTafsiliCommand.Id));
    }
}
