using Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;

namespace Accounting.Application.Tests.Vouchers.Commands.UpdateVoucherDetail;

public sealed class UpdateVoucherDetailCommandValidatorTests
{
    private readonly UpdateVoucherDetailCommandValidator _validator = new();

    private static UpdateVoucherDetailCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف اصلاحی",
        Radif: 2,
        Debtor: null,
        Creditor: 2500m,
        VahedCode: "0002",
        Year: "1404");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherDetailCommand.Id));
    }

    [Fact]
    public void Validate_DescriptionOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Description = new string('a', 201) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherDetailCommand.Description));
    }

    [Fact]
    public void Validate_DescriptionAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Description = new string('a', 200) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { VahedCode = "00001" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherDetailCommand.VahedCode));
    }

    [Fact]
    public void Validate_YearOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Year = "14050" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherDetailCommand.Year));
    }

    [Fact]
    public void Validate_AllOptionalFieldsNull_Passes()
    {
        var command = ValidCommand() with
        {
            AccountId = null,
            ReceiptId = null,
            CheckId = null,
            LowLevelCodeId = null,
            EtebarId = null,
            Description = null,
            Radif = null,
            Debtor = null,
            Creditor = null,
            VahedCode = null,
            Year = null,
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
