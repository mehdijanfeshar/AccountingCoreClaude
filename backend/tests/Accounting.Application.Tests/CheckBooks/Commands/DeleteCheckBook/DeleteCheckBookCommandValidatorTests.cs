using Accounting.Application.CheckBooks.Commands.DeleteCheckBook;

namespace Accounting.Application.Tests.CheckBooks.Commands.DeleteCheckBook;

public sealed class DeleteCheckBookCommandValidatorTests
{
    private readonly DeleteCheckBookCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteCheckBookCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteCheckBookCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteCheckBookCommand.Id));
    }
}
