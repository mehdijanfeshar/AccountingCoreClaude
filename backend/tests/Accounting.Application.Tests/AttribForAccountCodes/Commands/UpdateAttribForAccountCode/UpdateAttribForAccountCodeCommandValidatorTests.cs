using Accounting.Application.AttribForAccountCodes.Commands.UpdateAttribForAccountCode;

namespace Accounting.Application.Tests.AttribForAccountCodes.Commands.UpdateAttribForAccountCode;

public sealed class UpdateAttribForAccountCodeCommandValidatorTests
{
    private readonly UpdateAttribForAccountCodeCommandValidator _validator = new();

    private static UpdateAttribForAccountCodeCommand ValidCommand(Guid id) => new(
        Id: id,
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
}
