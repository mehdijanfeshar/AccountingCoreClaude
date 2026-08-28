using Accounting.Application.AttribForAccountCodes.Commands.CreateAttribForAccountCode;

namespace Accounting.Application.Tests.AttribForAccountCodes.Commands.CreateAttribForAccountCode;

public sealed class CreateAttribForAccountCodeCommandValidatorTests
{
    private readonly CreateAttribForAccountCodeCommandValidator _validator = new();

    private static CreateAttribForAccountCodeCommand ValidCommand() => new(
        AccountCodeId: Guid.NewGuid(),
        AttribBoxNo: true,
        Flag: false,
        LenAtr: 4,
        AttribSum: true,
        ControlId: null,
        VahedCode: "0001",
        Year: "1404");

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
}
