using Accounting.Application.ChequesIncorrents.Commands.CreateChequesIncorrent;

namespace Accounting.Application.Tests.ChequesIncorrents.Commands.CreateChequesIncorrent;

public sealed class CreateChequesIncorrentCommandValidatorTests
{
    private readonly CreateChequesIncorrentCommandValidator _validator = new();

    private static CreateChequesIncorrentCommand ValidCommand() => new(
        CheckId: Guid.NewGuid(),
        DocNum: "100001",
        DocDate: "13990101",
        CheqNo: "20000001",
        CheqDate: "13990102",
        PaperDesc: "بابت خرید کالا",
        PayTo: "شرکت الف",
        RecivDate: "13990110",
        AccountNumber: "1234567890123",
        Creditor: 500_000m,
        VahedCode: "0001",
        Year: "1399");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullOptionals_Passes()
    {
        var result = _validator.Validate(ValidCommand() with
        {
            CheckId = null,
            PaperDesc = null,
            PayTo = null,
            RecivDate = null,
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyDocNum_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DocNum = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.DocNum));
    }

    [Fact]
    public void Validate_DocNumTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DocNum = new string('1', 7) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.DocNum));
    }

    [Fact]
    public void Validate_EmptyDocDate_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DocDate = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.DocDate));
    }

    [Fact]
    public void Validate_DocDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DocDate = "139901011" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.DocDate));
    }

    [Fact]
    public void Validate_EmptyCheqNo_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheqNo = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.CheqNo));
    }

    [Fact]
    public void Validate_CheqNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheqNo = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.CheqNo));
    }

    [Fact]
    public void Validate_EmptyCheqDate_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheqDate = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.CheqDate));
    }

    [Fact]
    public void Validate_CheqDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheqDate = "139901021" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.CheqDate));
    }

    [Fact]
    public void Validate_PaperDescTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { PaperDesc = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.PaperDesc));
    }

    [Fact]
    public void Validate_PayToTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { PayTo = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.PayTo));
    }

    [Fact]
    public void Validate_RecivDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { RecivDate = "139901101" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.RecivDate));
    }

    [Fact]
    public void Validate_EmptyAccountNumber_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.AccountNumber));
    }

    [Fact]
    public void Validate_AccountNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = new string('1', 14) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.AccountNumber));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.Year));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "13990" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChequesIncorrentCommand.Year));
    }
}
