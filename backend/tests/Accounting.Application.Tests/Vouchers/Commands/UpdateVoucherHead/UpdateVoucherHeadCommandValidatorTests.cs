using Accounting.Application.Vouchers.Commands.UpdateVoucherHead;

namespace Accounting.Application.Tests.Vouchers.Commands.UpdateVoucherHead;

public sealed class UpdateVoucherHeadCommandValidatorTests
{
    private readonly UpdateVoucherHeadCommandValidator _validator = new();

    private static UpdateVoucherHeadCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        DocNum: "000001",
        DateDoc: "14030101",
        DocLife: null,
        HeadDesc: "سند افتتاحیه",
        Apendix: null,
        SystemTypeId: null,
        FlagState: null,
        VahedCode: "0001",
        Year: "1403",
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
    public void Validate_EmptyId_Fails()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.Id));
    }

    [Fact]
    public void Validate_ParentHeadIdEqualsOwnId_Fails()
    {
        var id = Guid.NewGuid();
        var command = ValidCommand() with { Id = id, ParentHeadId = id };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ParentHeadId");
    }

    [Fact]
    public void Validate_ParentHeadIdDifferentFromOwnId_Passes()
    {
        var command = ValidCommand() with { Id = Guid.NewGuid(), ParentHeadId = Guid.NewGuid() };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyDocNum_Fails()
    {
        var command = ValidCommand() with { DocNum = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.DocNum));
    }

    [Fact]
    public void Validate_DocNumOverMaxLength_Fails()
    {
        var command = ValidCommand() with { DocNum = "0000001" }; // 7 chars, max is 6

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.DocNum));
    }

    [Fact]
    public void Validate_DocNumAtMaxLength_Passes()
    {
        var command = ValidCommand() with { DocNum = "000001" }; // 6 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyDateDoc_Fails()
    {
        var command = ValidCommand() with { DateDoc = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.DateDoc));
    }

    [Fact]
    public void Validate_DateDocOverMaxLength_Fails()
    {
        var command = ValidCommand() with { DateDoc = "140301011" }; // 9 chars, max is 8

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.DateDoc));
    }

    [Fact]
    public void Validate_DateDocAtMaxLength_Passes()
    {
        var command = ValidCommand() with { DateDoc = "14030101" }; // 8 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var command = ValidCommand() with { VahedCode = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { VahedCode = "00001" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeAtMaxLength_Passes()
    {
        var command = ValidCommand() with { VahedCode = "0001" }; // 4 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var command = ValidCommand() with { Year = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.Year));
    }

    [Fact]
    public void Validate_YearOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Year = "14031" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.Year));
    }

    [Fact]
    public void Validate_YearAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Year = "1403" }; // 4 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_HeadDescOverMaxLength_Fails()
    {
        var command = ValidCommand() with { HeadDesc = new string('a', 251) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.HeadDesc));
    }

    [Fact]
    public void Validate_HeadDescAtMaxLength_Passes()
    {
        var command = ValidCommand() with { HeadDesc = new string('a', 250) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.Apendix));
    }

    [Fact]
    public void Validate_ApendixAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Apendix = new string('a', 800) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_SndVahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { SndVahedCode = "00001" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.SndVahedCode));
    }

    [Fact]
    public void Validate_SndVahedCodeAtMaxLength_Passes()
    {
        var command = ValidCommand() with { SndVahedCode = "0001" }; // 4 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AttachFileNameOverMaxLength_Fails()
    {
        var command = ValidCommand() with { AttachFileName = new string('a', 101) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.AttachFileName));
    }

    [Fact]
    public void Validate_AttachFileNameAtMaxLength_Passes()
    {
        var command = ValidCommand() with { AttachFileName = new string('a', 100) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AtfNumOverMaxLength_Fails()
    {
        var command = ValidCommand() with { AtfNum = new string('a', 16) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVoucherHeadCommand.AtfNum));
    }

    [Fact]
    public void Validate_AtfNumAtMaxLength_Passes()
    {
        var command = ValidCommand() with { AtfNum = new string('a', 15) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
