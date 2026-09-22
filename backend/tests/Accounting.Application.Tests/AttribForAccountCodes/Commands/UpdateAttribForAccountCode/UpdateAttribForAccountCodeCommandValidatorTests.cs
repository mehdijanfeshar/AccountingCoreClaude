using Accounting.Application.AttribForAccountCodes.Commands.UpdateAttribForAccountCode;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.AttribForAccountCodes.Commands.UpdateAttribForAccountCode;

public sealed class UpdateAttribForAccountCodeCommandValidatorTests
{
    private readonly UpdateAttribForAccountCodeCommandValidator _validator = new();

    private static UpdateAttribForAccountCodeCommand ValidCommand(Guid id) => new(
        Id: id,
        AccountCodeId: Guid.NewGuid(),
        AttribBoxNo: 3,
        Flag: AttribFlag.Date,
        LenAtr: 4,
        AttribSum: AttribSum.Summable,
        ControlId: null,
        Year: "1404")
    {
        VahedCode = "0001",
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(ValidCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAttribForAccountCodeCommand.Id));
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var command = ValidCommand(Guid.NewGuid()) with { AccountCodeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAttribForAccountCodeCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var command = ValidCommand(Guid.NewGuid()) with { VahedCode = "00001" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAttribForAccountCodeCommand.VahedCode));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var command = ValidCommand(Guid.NewGuid()) with { Year = "14040" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAttribForAccountCodeCommand.Year));
    }

    [Fact]
    public void Validate_AttribBoxNoOutOfRange_Fails()
    {
        var command = ValidCommand(Guid.NewGuid()) with { AttribBoxNo = 10 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAttribForAccountCodeCommand.AttribBoxNo));
    }

    [Fact]
    public void Validate_InvalidFlagEnumValue_Fails()
    {
        var command = ValidCommand(Guid.NewGuid()) with { Flag = (AttribFlag)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAttribForAccountCodeCommand.Flag));
    }

    [Fact]
    public void Validate_InvalidAttribSumEnumValue_Fails()
    {
        var command = ValidCommand(Guid.NewGuid()) with { AttribSum = (AttribSum)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAttribForAccountCodeCommand.AttribSum));
    }

    [Fact]
    public void Validate_InvalidControlIdEnumValue_Fails()
    {
        var command = ValidCommand(Guid.NewGuid()) with { ControlId = (AttribControl)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAttribForAccountCodeCommand.ControlId));
    }

    [Fact]
    public void Validate_ControlIdNull_Passes()
    {
        var command = ValidCommand(Guid.NewGuid()) with { ControlId = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
