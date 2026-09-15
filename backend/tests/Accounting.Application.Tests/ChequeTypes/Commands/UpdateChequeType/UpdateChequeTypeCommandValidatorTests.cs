using Accounting.Application.ChequeTypes.Commands.UpdateChequeType;

namespace Accounting.Application.Tests.ChequeTypes.Commands.UpdateChequeType;

public sealed class UpdateChequeTypeCommandValidatorTests
{
    private readonly UpdateChequeTypeCommandValidator _validator = new();

    private static UpdateChequeTypeCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        ChequeTypeTitle: "Updated",
        ChequeWidth: 210,
        ChequeHeight: 95,
        ChequeImage: new byte[] { 9, 9, 9 },
        ChequeAdateFont: "Tahoma-10",
        ChequeAdateLeft: 15,
        ChequeAdateTop: 16,
        ChequeAdateWidth: 17,
        ChequeNdateFont: "Tahoma-11",
        ChequeNdateLeft: 25,
        ChequeNdateTop: 26,
        ChequeNdateWidth: 27,
        ChequeAamountFont: "Tahoma-12",
        ChequeAamountLeft: 35,
        ChequeAamountTop: 36,
        ChequeAamountWidth: 37,
        ChequeLamountFont: "Tahoma-13",
        ChequeLamountLeft: 45,
        ChequeLamountTop: 46,
        ChequeLamountWidth: 47,
        ChequeNamountFont: "Tahoma-14",
        ChequeNamountLeft: 55,
        ChequeNamountTop: 56,
        ChequeNamountWidth: 57,
        ChequeDescribe1Font: "Tahoma-15",
        ChequeDescribe1Left: 65,
        ChequeDescribe1Top: 66,
        ChequeDescribe1Width: 67,
        ChequeDescribe2Font: "Tahoma-16",
        ChequeDescribe2Left: 75,
        ChequeDescribe2Top: 76,
        ChequeDescribe2Width: 77,
        ChequeBreaklineFont: "Tahoma-17",
        ChequeBreaklineLeft: 85,
        ChequeBreaklineTop: 86,
        ChequeBreaklineWidth: 87,
        PrinterMargineTop: 7,
        PrinterMargineLeft: 8,
        PrinterType: "Epson",
        Year: "1404")
    {
        VahedCode = "0200",
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequeTypeCommand.Id));
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequeTypeCommand.Year));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequeTypeCommand.VahedCode));
    }
}
