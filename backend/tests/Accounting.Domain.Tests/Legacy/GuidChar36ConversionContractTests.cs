namespace Accounting.Domain.Tests.Legacy;

/// <summary>
/// این تست به‌طور مستقیم <c>Accounting.Infrastructure.Legacy.GuidToChar36Converter</c> را فراخوانی
/// نمی‌کند، چون آن کلاس به <c>Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter&lt;,&gt;</c>
/// وابسته است و افزودن آن وابستگی به این پروژهٔ تست (Accounting.Domain.Tests) به‌معنای کشیدن EF Core
/// به یک پروژهٔ تستی است که قرار است روی Domain خالص (بدون وابستگی خارجی) بماند.
///
/// به‌جای آن، همان دو delegate «write» و «read» که body واقعی converter را تشکیل می‌دهند اینجا
/// عیناً بازتولید شده‌اند (<see cref="Write"/> و <see cref="Read"/>) تا این تست به‌عنوان
/// «contract pin» عمل کند: اگر رفتار Guid.ToString("D") یا Guid.ParseExact روی یک runtime جدید
/// تغییر کند، این تست قرمز می‌شود پیش از اینکه به تولید برسد.
///
/// اگر GuidToChar36Converter در آینده تغییر کند، این دو متد باید هم‌زمان به‌روزرسانی شوند.
/// </summary>
public class GuidChar36ConversionContractTests
{
    // عیناً از GuidToChar36Converter.Instance کپی شده — رجوع کنید به
    // backend/src/Accounting.Infrastructure/Legacy/GuidToChar36Converter.cs
    private static string Write(Guid g) => g.ToString("D").ToLowerInvariant();

    private static Guid Read(string s) => Guid.ParseExact(s.Trim(), "D");

    [Fact]
    public void ToStringD_OnThisRuntime_IsLowercase()
    {
        // فرضِ بنیادین کل این refactor: Guid.ToString("D") روی این runtime خودش
        // lowercase تولید می‌کند (بدون نیاز به ToLowerInvariant). اگر این فرض یک روز
        // نقض شود، ToLowerInvariant در Write بازهم درست کار می‌کند، اما این تست
        // صراحتاً مستند می‌کند که در حال حاضر آن safety-net واقعاً لازم نیست —
        // صرفاً محافظ در برابر تغییر رفتار runtime/culture است.
        var g = Guid.NewGuid();

        var formatted = g.ToString("D");

        Assert.Equal(formatted.ToLowerInvariant(), formatted);
        Assert.DoesNotContain(formatted, c => char.IsUpper(c));
    }

    [Fact]
    public void Write_ProducesCanonical36CharLowercaseDashedFormat()
    {
        var g = Guid.NewGuid();

        var s = Write(g);

        Assert.Equal(36, s.Length);
        Assert.Equal(s.ToLowerInvariant(), s);
        Assert.Matches(
            "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            s);
    }

    [Theory]
    [InlineData("3fa85f64-5717-4562-b3fc-2c963f66afa6")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")]
    public void RoundTrip_CanonicalLowercaseDashedString_IsPreservedExactly(string canonical)
    {
        // این خودِ contract ای است که کل refactor به آن متکی است:
        // مقدار canonical که از Oracle خوانده می‌شود، پس از Read → Write باید
        // بایت‌به‌بایت با ورودی یکسان بماند (idempotent روی داده‌ای که از قبل canonical است).
        var read = Read(canonical);
        var writtenBack = Write(read);

        Assert.Equal(canonical, writtenBack);
    }

    [Fact]
    public void RoundTrip_RandomGuids_AlwaysPreservesCanonicalForm()
    {
        for (var i = 0; i < 1000; i++)
        {
            var original = Guid.NewGuid();

            var written = Write(original);
            var readBack = Read(written);
            var writtenAgain = Write(readBack);

            Assert.Equal(original, readBack);
            Assert.Equal(written, writtenAgain);
        }
    }

    [Theory]
    [InlineData("3FA85F64-5717-4562-B3FC-2C963F66AFA6")] // uppercase dashed — پارس می‌شود، اما نوشتنِ دوباره باید lowercase تولید کند
    [InlineData("  3fa85f64-5717-4562-b3fc-2c963f66afa6  ")] // فضای خالی اطراف — Trim در Read هندل می‌کند
    public void Read_ToleratesCaseAndSurroundingWhitespace_ButWriteNormalizesToLowercase(string input)
    {
        var guid = Read(input);
        var writtenBack = Write(guid);

        Assert.Equal(writtenBack.ToLowerInvariant(), writtenBack);
        Assert.Equal(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), guid);
    }

    [Fact]
    public void Read_RejectsDashlessUppercaseNFormat_LikeOracleSysGuid()
    {
        // این تست دقیقاً همان ریسکِ شناخته‌شده‌ای را که در گزارش QA مستند شده مستند می‌کند:
        // ستون‌هایی که با sys_guid() پر می‌شوند مقدار RAW(16) تولید می‌کنند که در یک ستون
        // CHAR(36) به‌صورت رشتهٔ ۳۲ کاراکتریِ بدون خط‌تیره و UPPERCASE («N» format) ذخیره
        // می‌شود. ParseExact با فرمت "D" این را عمداً رد می‌کند (fail loudly) به‌جای اینکه
        // با Guid.Parse معمولی به‌طور خاموش آن را بپذیرد و در نوشتنِ بعدی بازفرمت‌دهی کند.
        const string nFormatValue = "3FA85F6457174562B3FC2C963F66AFA6"; // 32-char dashless UPPERCASE — فرمت واقعی sys_guid() در CHAR(36)

        Assert.Throws<FormatException>(() => Read(nFormatValue));

        // در تضاد با آن، Guid.Parse معمولی (بدون ParseExact) این‌ها را بی‌سروصدا قبول می‌کند —
        // دقیقاً همان رفتار «lenient» ای که در converter عمداً انتخاب نشده است.
        Assert.True(Guid.TryParse(nFormatValue, out _));
    }

    [Fact]
    public void Read_RejectsLegacyDefaultZeroString_LikeAccountCodeParentIdDefault()
    {
        // ریسک شناخته‌شدهٔ دیگر: TB_ACCOUNTCODE.PARENTID مقدار DEFAULT '0' دارد که
        // یک GUID معتبر نیست. اگر این مقدار واقعاً در دیتابیس درج شود، خواندن آن ردیف
        // با یک FormatException شکست می‌خورد (fail loudly) به‌جای تولید GUID نامعتبر/گمراه‌کننده.
        Assert.Throws<FormatException>(() => Read("0"));
    }
}
