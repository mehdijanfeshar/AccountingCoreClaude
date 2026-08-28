using Accounting.Application.WorkShops.Commands.UpdateWorkShop;

namespace Accounting.Application.Tests.WorkShops.Commands.UpdateWorkShop;

public sealed class UpdateWorkShopCommandValidatorTests
{
    private readonly UpdateWorkShopCommandValidator _validator = new();

    private static UpdateWorkShopCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        AccountCodeId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        WorkShopName: "کارگاه به‌روزشده",
        WorkShopCode: "WS002",
        VahedCode: "0002",
        IsActive: false,
        CheckFile: null);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWorkShopCommand.Id));
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountCodeId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWorkShopCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_NullBranchId_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { BranchId = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyWorkShopName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopName = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWorkShopCommand.WorkShopName));
    }

    [Fact]
    public void Validate_WorkShopCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopCode = new string('a', 11) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWorkShopCommand.WorkShopCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00002" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWorkShopCommand.VahedCode));
    }
}
