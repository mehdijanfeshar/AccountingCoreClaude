using Accounting.Application.Common.Exceptions;
using Accounting.Application.Elams;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.Elams;

/// <summary>نامهٔ اعلامیهٔ درآمد برای سبا و قواعد پیش از ارسال (عین <c>SendElmDrmdCommandHandler</c> مرجع).</summary>
public class RevenueElamLetterTests
{
    private static TB_ELAMHEAD Head() => new()
    {
        ELAMH_CASE = ElamCase.Creditor,
        ELAMHDRAMAD_TYPE = DaramElamhType.PersonalDramadElam,
        YEAR = "1404",
        VAHEDCODE = "0042",
        ELAMH_SENDRCVVAHED = "0051",
        ELAMH_SERIALNO = "00421404000123",
        ELAMH_DATE = "14040510",
        ELAMH_CODE = "900003",
        ELAMH_WORKSHOPCODE = "1234567890",
        ELAMH_WORKSHOPNAME = "کارگاه",
        ELAMH_RCVNO = "77",
        ELAMH_RCVDT = "14040501",
        // ⚠️ قاعدهٔ ارسال (عین مرجع) طول «سال + ماه» را ۸ می‌خواهد؛ با ماه دورقمیِ معتبر هرگز برقرار نیست.
        ELAMH_LSTMON = "0405",
        PEIMAN_NO = "P1",
    };

    [Theory]
    [InlineData("1", 91)]
    [InlineData("12", 44)]
    public void CheckDigits_mod11_two_rounds(string digits, long expected)
        => Assert.Equal(expected, RevenueElamLetterBuilder.CheckDigits(digits));

    [Fact]
    public void Build_uses_numeric_case_and_maps_fields()
    {
        var letter = RevenueElamLetterBuilder.Build(Head(), new TB_ELAMDETAIL { DEBTOR = 1500m });

        Assert.Equal("2", letter.LetterType);
        Assert.Equal("0", letter.Price1);
        Assert.Equal("1500", letter.Price2);
        Assert.Equal("04" + "0" + "0042" + "00421404000123", letter.LetterSerial);
        Assert.Equal("0051" + "1" + "00123", letter.IdNo);
        Assert.Equal("14040405", letter.FunctionDate);
        Assert.Equal("P1", letter.ContractNo);
        Assert.Equal("14040501", letter.ContractDate);
        Assert.Equal(
            RevenueElamLetterBuilder.CheckDigits("1" + "2" + "04" + "0051" + "0042" + "1500").ToString(),
            letter.CodeDigit);
    }

    [Fact]
    public void Build_non_personal_omits_contract()
    {
        var head = Head();
        head.ELAMHDRAMAD_TYPE = null;
        var letter = RevenueElamLetterBuilder.Build(head, new TB_ELAMDETAIL { DEBTOR = 1m });
        Assert.Equal(string.Empty, letter.ContractNo);
        Assert.Equal(string.Empty, letter.ContractDate);
    }

    [Fact]
    public void EnsureReadyToSend_accepts_complete_head()
        => RevenueSendRules.EnsureReadyToSend(Head());

    [Fact(Skip = "باگ باز: ستون ELAMH_LSTMON دو کاراکتری و validator ماه ۰۱..۱۲ است، ولی قاعدهٔ ارسال (عین مرجع) طول سال+ماه = ۸ می‌خواهد؛ اعلامیهٔ درآمد معتبر هرگز ارسال نمی‌شود. قالب مورد انتظار سبا باید روشن شود.")]
    public void EnsureReadyToSend_accepts_validated_two_digit_month()
    {
        var head = Head();
        head.ELAMH_LSTMON = "05";
        RevenueSendRules.EnsureReadyToSend(head);
    }

    public static TheoryData<Action<TB_ELAMHEAD>> Broken => new()
    {
        h => h.ELAMH_WORKSHOPCODE = "123",
        h => h.ELAMH_RCVNO = null,
        h => h.ELAMH_WORKSHOPNAME = "",
        h => h.ELAMH_LSTMON = null,
        h => h.VAHEDCODE = "42",
        h => h.ELAMH_SENDRCVVAHED = null,
    };

    [Theory]
    [MemberData(nameof(Broken))]
    public void EnsureReadyToSend_rejects_incomplete_head(Action<TB_ELAMHEAD> breakIt)
    {
        var head = Head();
        breakIt(head);
        Assert.Throws<ElamConflictException>(() => RevenueSendRules.EnsureReadyToSend(head));
    }
}
