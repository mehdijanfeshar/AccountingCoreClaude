using Accounting.Application.Vouchers.Commands.CreateVoucherHead;

namespace Accounting.Application.Tests.Vouchers.Commands.CreateVoucherHead;

public sealed class CreateVoucherHeadCommandValidatorTests
{
    private readonly CreateVoucherHeadCommandValidator _validator = new();

    private static CreateVoucherHeadCommand ValidCommand() => new(
        DocNum: "000001",
        DateDoc: "14050101",
        DocLife: true,
        HeadDesc: "سند افتتاحیه",
        Apendix: null,
        SystemTypeId: null,
        FlagState: null,
        VahedCode: "0001",
        Year: "1405",
        IsAutomatic: false,
        SndVahedCode: null,
        ParentHeadId: null,
        AttachFileName: null,
        AtfNum: null);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyDocNum_Fails()
    {
        var command = ValidCommand() with { DocNum = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.DocNum));
    }

    [Fact]
    public void Validate_DocNumOverMaxLength_Fails()
    {
        var command = ValidCommand() with { DocNum = "0000001" }; // 7 chars, max is 6

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.DocNum));
    }

    [Fact]
    public void Validate_EmptyDateDoc_Fails()
    {
        var command = ValidCommand() with { DateDoc = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.DateDoc));
    }

    [Fact]
    public void Validate_DateDocOverMaxLength_Fails()
    {
        var command = ValidCommand() with { DateDoc = "140501011" }; // 9 chars, max is 8

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.DateDoc));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var command = ValidCommand() with { VahedCode = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { VahedCode = "00001" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var command = ValidCommand() with { Year = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.Year));
    }

    [Fact]
    public void Validate_YearOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Year = "14050" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.Year));
    }

    [Fact]
    public void Validate_HeadDescOverMaxLength_Fails()
    {
        var command = ValidCommand() with { HeadDesc = new string('a', 251) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.HeadDesc));
    }

    [Fact]
    public void Validate_HeadDescNull_Passes()
    {
        var command = ValidCommand() with { HeadDesc = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ApendixOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Apendix = new string('a', 801) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.Apendix));
    }

    [Fact]
    public void Validate_SndVahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { SndVahedCode = "00001" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.SndVahedCode));
    }

    [Fact]
    public void Validate_AttachFileNameOverMaxLength_Fails()
    {
        var command = ValidCommand() with { AttachFileName = new string('a', 101) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.AttachFileName));
    }

    [Fact]
    public void Validate_AtfNumOverMaxLength_Fails()
    {
        var command = ValidCommand() with { AtfNum = new string('a', 16) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadCommand.AtfNum));
    }
}
