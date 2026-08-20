using Accounting.Application.Vouchers.Commands.DeleteVoucherDetail;

namespace Accounting.Application.Tests.Vouchers.Commands.DeleteVoucherDetail;

public sealed class DeleteVoucherDetailCommandValidatorTests
{
    private readonly DeleteVoucherDetailCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteVoucherDetailCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteVoucherDetailCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteVoucherDetailCommand.Id));
    }
}
