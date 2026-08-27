using Accounting.Application.PreDescribs.Commands.CreatePreDescrib;

namespace Accounting.Application.Tests.PreDescribs.Commands.CreatePreDescrib;

public sealed class CreatePreDescribCommandValidatorTests
{
    private readonly CreatePreDescribCommandValidator _validator = new();

    private static CreatePreDescribCommand ValidCommand() => new(
        AccountId: null,
        Descrip: "توضیحات پیش‌فرض",
        VahedCode: "0001",
        FlagVoucher: false);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DescripOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Descrip = new string('a', 201) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePreDescribCommand.Descrip));
    }

    [Fact]
    public void Validate_DescripAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Descrip = new string('a', 200) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DescripNull_Passes()
    {
        var command = ValidCommand() with { Descrip = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { VahedCode = "00001" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePreDescribCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeAtMaxLength_Passes()
    {
        var command = ValidCommand() with { VahedCode = "0001" };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_VahedCodeNull_Passes()
    {
        var command = ValidCommand() with { VahedCode = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
