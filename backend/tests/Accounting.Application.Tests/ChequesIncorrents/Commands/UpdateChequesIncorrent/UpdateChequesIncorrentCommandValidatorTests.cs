using Accounting.Application.ChequesIncorrents.Commands.UpdateChequesIncorrent;

namespace Accounting.Application.Tests.ChequesIncorrents.Commands.UpdateChequesIncorrent;

public sealed class UpdateChequesIncorrentCommandValidatorTests
{
    private readonly UpdateChequesIncorrentCommandValidator _validator = new();

    private static UpdateChequesIncorrentCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        CheckId: Guid.NewGuid(),
        DocNum: "200002",
        DocDate: "13990201",
        CheqNo: "30000002",
        CheqDate: "13990202",
        PaperDesc: "بابت خرید کالای دیگر",
        PayTo: "شرکت ب",
        RecivDate: "13990210",
        AccountNumber: "3210987654321",
        Creditor: 750_000m,
        VahedCode: "0002",
        Year: "1400");

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.Id));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.DocNum));
    }

    [Fact]
    public void Validate_DocNumTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DocNum = new string('2', 7) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.DocNum));
    }

    [Fact]
    public void Validate_EmptyDocDate_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DocDate = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.DocDate));
    }

    [Fact]
    public void Validate_DocDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DocDate = "139902011" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.DocDate));
    }

    [Fact]
    public void Validate_EmptyCheqNo_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheqNo = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.CheqNo));
    }

    [Fact]
    public void Validate_CheqNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheqNo = new string('2', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.CheqNo));
    }

    [Fact]
    public void Validate_EmptyCheqDate_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheqDate = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.CheqDate));
    }

    [Fact]
    public void Validate_CheqDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheqDate = "139902021" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.CheqDate));
    }

    [Fact]
    public void Validate_PaperDescTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { PaperDesc = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.PaperDesc));
    }

    [Fact]
    public void Validate_PayToTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { PayTo = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.PayTo));
    }

    [Fact]
    public void Validate_RecivDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { RecivDate = "139902101" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.RecivDate));
    }

    [Fact]
    public void Validate_EmptyAccountNumber_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.AccountNumber));
    }

    [Fact]
    public void Validate_AccountNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = new string('2', 14) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.AccountNumber));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00002" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.Year));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14000" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateChequesIncorrentCommand.Year));
    }
}
