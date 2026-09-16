using Accounting.Application.AttribForAccountCodes.Commands.CreateAttribForAccountCode;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.AttribForAccountCodes.Commands.CreateAttribForAccountCode;

public sealed class CreateAttribForAccountCodeCommandValidatorTests
{
    private readonly CreateAttribForAccountCodeCommandValidator _validator = new();

    private static CreateAttribForAccountCodeCommand ValidCommand() => new(
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
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var command = ValidCommand() with { AccountCodeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAttribForAccountCodeCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var command = ValidCommand() with { VahedCode = "" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAttribForAccountCodeCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var command = ValidCommand() with { VahedCode = "00001" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAttribForAccountCodeCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var command = ValidCommand() with { Year = "" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAttribForAccountCodeCommand.Year));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var command = ValidCommand() with { Year = "14040" };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAttribForAccountCodeCommand.Year));
    }

    [Fact]
    public void Validate_AttribBoxNoOutOfRange_Fails()
    {
        var command = ValidCommand() with { AttribBoxNo = 10 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAttribForAccountCodeCommand.AttribBoxNo));
    }

    [Fact]
    public void Validate_InvalidFlagEnumValue_Fails()
    {
        var command = ValidCommand() with { Flag = (AttribFlag)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAttribForAccountCodeCommand.Flag));
    }

    [Fact]
    public void Validate_InvalidAttribSumEnumValue_Fails()
    {
        var command = ValidCommand() with { AttribSum = (AttribSum)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAttribForAccountCodeCommand.AttribSum));
    }

    [Fact]
    public void Validate_InvalidControlIdEnumValue_Fails()
    {
        var command = ValidCommand() with { ControlId = (AttribControl)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAttribForAccountCodeCommand.ControlId));
    }

    [Fact]
    public void Validate_ControlIdNull_Passes()
    {
        var command = ValidCommand() with { ControlId = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
