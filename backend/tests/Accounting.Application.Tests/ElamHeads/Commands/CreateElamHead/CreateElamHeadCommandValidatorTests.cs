using Accounting.Application.ElamHeads.Commands.CreateElamHead;

namespace Accounting.Application.Tests.ElamHeads.Commands.CreateElamHead;

public sealed class CreateElamHeadCommandValidatorTests
{
    private readonly CreateElamHeadCommandValidator _validator = new();

    private static CreateElamHeadCommand ValidCommand() => new(
        VoucherHeadId: Guid.NewGuid(),
        SerialNo: "SER-001",
        Code: "COD-01",
        DabirNo: "DABIR-001",
        DabirDate: "14040101",
        PrintNo: 7,
        Case: true,
        SerialNoInput: "INP-01",
        WebStat: 2,
        Date: "14040102",
        Desc: "شرح اعلاميه تستی",
        WorkShopId: Guid.NewGuid(),
        RcvNo: "RCV-001",
        RcvDt: "14040103",
        LstMon: "07",
        PayNo: "PAY-00001",
        DramadType: false,
        PeimanNo: "PEIMAN-01",
        WorkShopCode: "WS-001",
        WorkShopName: "کارگاه تستی",
        SendRcvVahed: "0009",
        ElamYear: "04",
        Year: "1404",
        ElamSenderId: Guid.NewGuid())
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
    public void Validate_AllNullableFieldsNull_Passes()
    {
        // VahedCode is the sole exception to "every field is nullable" (see validator XML doc)
        // and must still be set, since VahedCode itself is never allowed to be empty.
        var command = new CreateElamHeadCommand(
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
            Year: null,
            ElamSenderId: null)
        {
            VahedCode = "0001",
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.VahedCode));
    }

    [Fact]
    public void Validate_SerialNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SerialNo = new string('a', 15) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.SerialNo));
    }

    [Fact]
    public void Validate_SerialNoAtMaxLength_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { SerialNo = new string('a', 14) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Code = new string('a', 7) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.Code));
    }

    [Fact]
    public void Validate_DabirNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DabirNo = new string('a', 11) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.DabirNo));
    }

    [Fact]
    public void Validate_DabirDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DabirDate = new string('a', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.DabirDate));
    }

    [Fact]
    public void Validate_SerialNoInputTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SerialNoInput = new string('a', 7) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.SerialNoInput));
    }

    [Fact]
    public void Validate_DateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Date = new string('a', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.Date));
    }

    [Fact]
    public void Validate_DescTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Desc = new string('a', 301) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.Desc));
    }

    [Fact]
    public void Validate_RcvNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { RcvNo = new string('a', 15) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.RcvNo));
    }

    [Fact]
    public void Validate_RcvDtTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { RcvDt = new string('a', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.RcvDt));
    }

    [Fact]
    public void Validate_LstMonTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { LstMon = "123" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.LstMon));
    }

    [Fact]
    public void Validate_PayNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { PayNo = new string('a', 16) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.PayNo));
    }

    [Fact]
    public void Validate_PeimanNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { PeimanNo = new string('a', 13) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.PeimanNo));
    }

    [Fact]
    public void Validate_WorkShopCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopCode = new string('a', 11) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.WorkShopCode));
    }

    [Fact]
    public void Validate_WorkShopNameTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { WorkShopName = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.WorkShopName));
    }

    [Fact]
    public void Validate_SendRcvVahedTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SendRcvVahed = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.SendRcvVahed));
    }

    [Fact]
    public void Validate_ElamYearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ElamYear = "123" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.ElamYear));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.VahedCode));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14045" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateElamHeadCommand.Year));
    }
}
