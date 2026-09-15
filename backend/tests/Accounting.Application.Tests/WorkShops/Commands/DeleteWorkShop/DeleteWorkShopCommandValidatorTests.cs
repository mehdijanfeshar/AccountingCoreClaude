using Accounting.Application.WorkShops.Commands.DeleteWorkShop;

namespace Accounting.Application.Tests.WorkShops.Commands.DeleteWorkShop;

public sealed class DeleteWorkShopCommandValidatorTests
{
    private readonly DeleteWorkShopCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteWorkShopCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteWorkShopCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteWorkShopCommand.Id));
    }
}
