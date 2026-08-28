using Accounting.Application.WorkShops.Commands.CreateWorkShop;

namespace Accounting.Application.Tests.WorkShops.Commands.CreateWorkShop;

public sealed class CreateWorkShopCommandValidatorTests
{
    private readonly CreateWorkShopCommandValidator _validator = new();

    private static CreateWorkShopCommand ValidCommand() => new(
        AccountCodeId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        WorkShopName: "کارگاه شماره یک",
        WorkShopCode: "WS001",
        VahedCode: "0001",
        IsActive: true,
        CheckFile: null);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullBranchId_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { BranchId = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountCodeId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkShopCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_EmptyWorkShopName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopName = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkShopCommand.WorkShopName));
    }

    [Fact]
    public void Validate_WorkShopNameTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopName = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkShopCommand.WorkShopName));
    }

    [Fact]
    public void Validate_EmptyWorkShopCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkShopCommand.WorkShopCode));
    }

    [Fact]
    public void Validate_WorkShopCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopCode = new string('a', 11) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkShopCommand.WorkShopCode));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkShopCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkShopCommand.VahedCode));
    }
}
