using Accounting.Application.RevolvingFunds.Commands.DeleteRevolvingFund;

namespace Accounting.Application.Tests.RevolvingFunds.Commands.DeleteRevolvingFund;

public sealed class DeleteRevolvingFundCommandValidatorTests
{
    private readonly DeleteRevolvingFundCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteRevolvingFundCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteRevolvingFundCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteRevolvingFundCommand.Id));
    }
}
