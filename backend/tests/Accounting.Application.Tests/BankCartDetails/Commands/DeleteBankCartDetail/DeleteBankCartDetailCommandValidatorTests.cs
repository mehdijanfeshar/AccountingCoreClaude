using Accounting.Application.BankCartDetails.Commands.DeleteBankCartDetail;

namespace Accounting.Application.Tests.BankCartDetails.Commands.DeleteBankCartDetail;

public sealed class DeleteBankCartDetailCommandValidatorTests
{
    private readonly DeleteBankCartDetailCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteBankCartDetailCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteBankCartDetailCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteBankCartDetailCommand.Id));
    }
}
