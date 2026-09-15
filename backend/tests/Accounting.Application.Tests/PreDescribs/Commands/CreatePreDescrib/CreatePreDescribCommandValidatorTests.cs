using Accounting.Application.PreDescribs.Commands.CreatePreDescrib;

namespace Accounting.Application.Tests.PreDescribs.Commands.CreatePreDescrib;

public sealed class CreatePreDescribCommandValidatorTests
{
    private readonly CreatePreDescribCommandValidator _validator = new();

    private static CreatePreDescribCommand ValidCommand() => new(
        AccountId: null,
        Descrip: "توضیحات پیش‌فرض",
        FlagVoucher: false)
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
    public void Validate_EmptyVahedCode_Fails()
    {
        // VahedCode is now always server-assigned by VahedScopeBehavior before this validator
        // runs, so it can never legitimately be empty — NotEmpty is the correct second belt even
        // though the underlying VAHEDCODE column is nullable at the Legacy schema level.
        var command = ValidCommand() with { VahedCode = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePreDescribCommand.VahedCode));
    }
}
