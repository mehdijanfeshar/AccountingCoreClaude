using Accounting.Application.ChequeTypes.Commands.DeleteChequeType;

namespace Accounting.Application.Tests.ChequeTypes.Commands.DeleteChequeType;

public sealed class DeleteChequeTypeCommandValidatorTests
{
    private readonly DeleteChequeTypeCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteChequeTypeCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteChequeTypeCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteChequeTypeCommand.Id));
    }
}
