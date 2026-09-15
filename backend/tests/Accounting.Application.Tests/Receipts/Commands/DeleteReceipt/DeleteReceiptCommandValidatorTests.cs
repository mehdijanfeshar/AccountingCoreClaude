using Accounting.Application.Receipts.Commands.DeleteReceipt;

namespace Accounting.Application.Tests.Receipts.Commands.DeleteReceipt;

public sealed class DeleteReceiptCommandValidatorTests
{
    private readonly DeleteReceiptCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteReceiptCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteReceiptCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteReceiptCommand.Id));
    }
}
