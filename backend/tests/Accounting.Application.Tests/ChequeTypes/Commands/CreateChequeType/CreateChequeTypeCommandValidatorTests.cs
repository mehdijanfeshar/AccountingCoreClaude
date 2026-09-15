using Accounting.Application.ChequeTypes.Commands.CreateChequeType;

namespace Accounting.Application.Tests.ChequeTypes.Commands.CreateChequeType;

public sealed class CreateChequeTypeCommandValidatorTests
{
    private readonly CreateChequeTypeCommandValidator _validator = new();

    private static CreateChequeTypeCommand ValidCommand() => new(
        ChequeTypeTitle: "Standard",
        ChequeWidth: 200,
        ChequeHeight: 90,
        ChequeImage: new byte[] { 1, 2, 3 },
        ChequeAdateFont: "Arial-10",
        ChequeAdateLeft: 10,
        ChequeAdateTop: 11,
        ChequeAdateWidth: 12,
        ChequeNdateFont: "Arial-11",
        ChequeNdateLeft: 20,
        ChequeNdateTop: 21,
        ChequeNdateWidth: 22,
        ChequeAamountFont: "Arial-12",
        ChequeAamountLeft: 30,
        ChequeAamountTop: 31,
        ChequeAamountWidth: 32,
        ChequeLamountFont: "Arial-13",
        ChequeLamountLeft: 40,
        ChequeLamountTop: 41,
        ChequeLamountWidth: 42,
        ChequeNamountFont: "Arial-14",
        ChequeNamountLeft: 50,
        ChequeNamountTop: 51,
        ChequeNamountWidth: 52,
        ChequeDescribe1Font: "Arial-15",
        ChequeDescribe1Left: 60,
        ChequeDescribe1Top: 61,
        ChequeDescribe1Width: 62,
        ChequeDescribe2Font: "Arial-16",
        ChequeDescribe2Left: 70,
        ChequeDescribe2Top: 71,
        ChequeDescribe2Width: 72,
        ChequeBreaklineFont: "Arial-17",
        ChequeBreaklineLeft: 80,
        ChequeBreaklineTop: 81,
        ChequeBreaklineWidth: 82,
        PrinterMargineTop: 5,
        PrinterMargineLeft: 6,
        PrinterType: "HP LaserJet",
        Year: "1403")
    {
        VahedCode = "0100",
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequeTypeCommand.Year));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14031" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequeTypeCommand.Year));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequeTypeCommand.VahedCode));
    }

    [Fact]
    public void Validate_ChequeTypeTitleTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ChequeTypeTitle = new string('a', 26) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequeTypeCommand.ChequeTypeTitle));
    }

    [Fact]
    public void Validate_ChequeAdateFontTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ChequeAdateFont = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequeTypeCommand.ChequeAdateFont));
    }

    [Fact]
    public void Validate_PrinterTypeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { PrinterType = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequeTypeCommand.PrinterType));
    }

    [Fact]
    public void Validate_AllOptionalFieldsNull_Passes()
    {
        var result = _validator.Validate(new CreateChequeTypeCommand(
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null,
            null,
            "1403")
        {
            VahedCode = "0100",
        });

        Assert.True(result.IsValid);
    }
}
