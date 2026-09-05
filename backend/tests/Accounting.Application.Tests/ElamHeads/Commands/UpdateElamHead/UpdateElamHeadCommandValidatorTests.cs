using Accounting.Application.ElamHeads.Commands.UpdateElamHead;

namespace Accounting.Application.Tests.ElamHeads.Commands.UpdateElamHead;

public sealed class UpdateElamHeadCommandValidatorTests
{
    private readonly UpdateElamHeadCommandValidator _validator = new();

    private static UpdateElamHeadCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        VoucherHeadId: Guid.NewGuid(),
        SerialNo: "SER-002",
        Code: "COD-02",
        DabirNo: "DABIR-002",
        DabirDate: "14040201",
        PrintNo: 9,
        Case: false,
        SerialNoInput: "INP-02",
        WebStat: 1,
        Date: "14040202",
        Desc: "شرح به‌روزشده",
        WorkShopId: Guid.NewGuid(),
        RcvNo: "RCV-002",
        RcvDt: "14040203",
        LstMon: "08",
        PayNo: "PAY-00002",
        DramadType: true,
        PeimanNo: "PEIMAN-02",
        WorkShopCode: "WS-002",
        WorkShopName: "کارگاه به‌روزشده",
        SendRcvVahed: "0010",
        ElamYear: "05",
        VahedCode: "0002",
        Year: "1405",
        ElamSenderId: Guid.NewGuid());

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.Id));
    }

    [Fact]
    public void Validate_AllOptionalFieldsNull_Passes()
    {
        var command = new UpdateElamHeadCommand(
            Id: Guid.NewGuid(),
            VoucherHeadId: null,
            SerialNo: null,
            Code: null,
            DabirNo: null,
            DabirDate: null,
            PrintNo: null,
            Case: null,
            SerialNoInput: null,
            WebStat: null,
            Date: null,
            Desc: null,
            WorkShopId: null,
            RcvNo: null,
            RcvDt: null,
            LstMon: null,
            PayNo: null,
            DramadType: null,
            PeimanNo: null,
            WorkShopCode: null,
            WorkShopName: null,
            SendRcvVahed: null,
            ElamYear: null,
            VahedCode: null,
            Year: null,
            ElamSenderId: null);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_SerialNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SerialNo = new string('a', 15) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.SerialNo));
    }

    [Fact]
    public void Validate_CodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Code = new string('a', 7) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.Code));
    }

    [Fact]
    public void Validate_DabirNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DabirNo = new string('a', 11) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.DabirNo));
    }

    [Fact]
    public void Validate_DabirDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DabirDate = new string('a', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.DabirDate));
    }

    [Fact]
    public void Validate_SerialNoInputTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SerialNoInput = new string('a', 7) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.SerialNoInput));
    }

    [Fact]
    public void Validate_DateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Date = new string('a', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.Date));
    }

    [Fact]
    public void Validate_DescTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Desc = new string('a', 301) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.Desc));
    }

    [Fact]
    public void Validate_RcvNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { RcvNo = new string('a', 15) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.RcvNo));
    }

    [Fact]
    public void Validate_RcvDtTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { RcvDt = new string('a', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.RcvDt));
    }

    [Fact]
    public void Validate_LstMonTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { LstMon = "123" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.LstMon));
    }

    [Fact]
    public void Validate_PayNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { PayNo = new string('a', 16) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.PayNo));
    }

    [Fact]
    public void Validate_PeimanNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { PeimanNo = new string('a', 13) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.PeimanNo));
    }

    [Fact]
    public void Validate_WorkShopCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopCode = new string('a', 11) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.WorkShopCode));
    }

    [Fact]
    public void Validate_WorkShopNameTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopName = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.WorkShopName));
    }

    [Fact]
    public void Validate_SendRcvVahedTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SendRcvVahed = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.SendRcvVahed));
    }

    [Fact]
    public void Validate_ElamYearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ElamYear = "123" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.ElamYear));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00002" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.VahedCode));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14055" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateElamHeadCommand.Year));
    }
}
