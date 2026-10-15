namespace Accounting.Application.Common.Interfaces;

/// <summary>یک اسکیمای سالانهٔ سیستم قدیم (هر واحد برای هر سال یک اسکیما، روی یکی از سرورها).</summary>
public sealed record LegacySourceDto(string Server, string Schema, int Year, bool Connected, string? Error, int Vouchers);

public sealed record LegacyFileDto(string FileName, string ContentType, byte[] Content);

/// <summary>نتیجهٔ بررسی یا اجرای انتقال؛ <c>Committed</c> فقط وقتی true است که واقعاً ثبت شده باشد.</summary>
public sealed record LegacyImportReportDto(
    bool Success,
    bool Committed,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Info);

/// <summary>سرور سیستم قدیم یک واحد (رمز هرگز برگردانده نمی‌شود).</summary>
public sealed record LegacyServerDto(string DataSource, string? Title, string? UserName, bool HasPassword);

/// <summary>اسکیمای یک سال؛ نام کاربر/رمز جدا فقط اگر با کاربر سرور فرق کند.</summary>
public sealed record LegacyYearDto(int Year, string Schema, string? UserName, bool HasPassword);

public sealed record LegacyConnectionsDto(LegacyServerDto? Server, IReadOnlyList<LegacyYearDto> Years);

/// <param name="DataSource">نام TNS، توصیف‌گر کامل یا <c>host:port/service</c>.</param>
/// <param name="Password">خالی = رمز ذخیره‌شده بماند.</param>
/// <param name="UserName">کاربر ورود جدا از اسکیما (معمولاً خالی: ورود با اسکیمای هر سال).</param>
/// <param name="CredentialToken">رمز رمزگشایی‌شده از فایل XML که سرور رمزگذاری کرده (برای مرورگر بی‌معنا).</param>
/// <param name="SchemaBase">نام اسکیما بدون سال (مثلاً <c>ACC9516</c>)؛ برای یافتن خودکار سال‌ها: اسکیمای هر سال = این + دو رقم سال.</param>
public sealed record LegacyServerInput(string DataSource, string? Title, string? UserName, string? Password, string? CredentialToken, string? SchemaBase = null);

public sealed record LegacyYearInput(int Year, string Schema, string? UserName, string? Password);

public sealed record LegacyConnectionsInput(LegacyServerInput Server, IReadOnlyList<LegacyYearInput> Years);

/// <summary>نتیجهٔ بررسی/ذخیره: <c>Saved</c> فقط وقتی همهٔ سال‌ها وصل شده باشند.</summary>
public sealed record LegacyConnectionsResultDto(bool Saved, IReadOnlyList<LegacySourceDto> Results);

/// <summary>سرور انتخاب‌شده از فایل XML سیستم قدیم با نام کاربر/رمز رمزگذاری‌شده‌اش.</summary>
public sealed record LegacyXmlServerInput(string Title, string DataSource, string? EncryptedUserName, string? EncryptedPassword);

/// <param name="Decrypted">false = کلید رمزگشایی تنظیم نشده یا ناسازگار است؛ کاربر باید نام کاربر و رمز را وارد کند.</param>
public sealed record LegacyXmlServerDto(bool Decrypted, string? UserName, string? CredentialToken, string? Message);

/// <summary>وضعیت انتقال یک واحد: کدینگ مشترک ساخته شده؟ کدینگ واحد پذیرفته شده؟</summary>
public sealed record LegacyImportStatusDto(bool ChartImported, DateTime? ChartImportedAt, bool UnitCodingImported, DateTime? UnitCodingImportedAt,
    DateTime? BankImportedAt = null);

/// <summary>اسناد منتقل‌شدهٔ یک سال مالی.</summary>
public sealed record LegacyVoucherYearDto(int Year, int Vouchers, int Lines, DateTime MigratedAt);

/// <summary>
/// انتقال از سیستم قدیم (خط‌کدینگ) به کدینگ شناور. اتصال به اسکیماهای قدیم، ساخت کاربرگ‌های تطبیق و ثبت
/// در دیتابیس جدید همه در Infrastructure است؛ کاربرگ پذیرفته‌شده روی سرور نگه داشته می‌شود تا انتقال اسناد
/// (مرحلهٔ بعد) از همان تطبیق استفاده کند.
/// </summary>
public interface ILegacyImportService
{
    /// <summary>اتصال دستی ذخیره‌شده؛ اگر نباشد، اسکیماها از سرورهای تنظیمات سامانه پیدا می‌شوند.</summary>
    Task<LegacyConnectionsDto> GetConnectionsAsync(string vahedCode, CancellationToken cancellationToken);

    /// <summary>
    /// همهٔ سال‌ها آزموده می‌شوند؛ فقط اگر همه وصل شوند و <paramref name="checkOnly"/> نباشد ذخیره می‌شود. «بررسی» با
    /// فهرست خالی سال‌ها = یافتن خودکار اسکیماهای سالانهٔ واحد روی همان سرور (فقط سال‌های یافت‌شده برمی‌گردند).
    /// </summary>
    Task<LegacyConnectionsResultDto> SaveConnectionsAsync(string vahedCode, LegacyConnectionsInput input, bool checkOnly, CancellationToken cancellationToken);

    /// <summary>رمزگشایی نام کاربر/رمز سرور انتخاب‌شده از فایل XML با کلید سیستم قدیم (تنظیمات سامانه).</summary>
    LegacyXmlServerDto ReadXmlServer(LegacyXmlServerInput input);

    Task<IReadOnlyList<LegacySourceDto>> FindSourcesAsync(string vahedCode, CancellationToken cancellationToken);

    Task<LegacyImportStatusDto> GetStatusAsync(string vahedCode, CancellationToken cancellationToken);

    /// <summary>کاربرگ کدینگ مشترک (گروه/کل/معین) از اسکیماهای واحدهای داده‌شده.</summary>
    Task<LegacyFileDto> BuildChartWorkbookAsync(IReadOnlyList<string> vahedCodes, CancellationToken cancellationToken);

    Task<LegacyImportReportDto> ImportChartAsync(byte[] workbook, bool commit, string userId, CancellationToken cancellationToken);

    /// <summary>کاربرگ کدینگ واحد: تفصیلی‌ها (با ستون کد ملی اشخاص)، فعالیت/مرکز هزینه، اسناد ناتراز.</summary>
    /// <summary>کاربرگ کدینگ واحد؛ با <paramref name="year"/> فقط از اسکیمای همان سال (همیشه از سیستم قدیم، نه فایل پذیرفته‌شده).</summary>
    Task<LegacyFileDto> BuildUnitWorkbookAsync(string vahedCode, int? year, CancellationToken cancellationToken);

    Task<LegacyImportReportDto> ImportUnitCodingAsync(string vahedCode, byte[] workbook, bool commit, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// انتقال اسناد یک سال مالی از سیستم قدیم؛ <paramref name="commit"/>=false فقط بررسی. انتقال دوبارهٔ همان سال اسنادِ
    /// انتقال قبلی را حذف و از نو منتقل می‌کند؛ اسناد دستی سیستم جدید دست نمی‌خورند.
    /// </summary>
    /// <summary>حساب‌های بانکی، نوع چک، دسته‌چک و برگ چک واحد از همهٔ سال‌ها (تکرارپذیر)؛ پیش از انتقال اسناد.</summary>
    Task<LegacyImportReportDto> ImportBankAsync(string vahedCode, bool commit, string userId, CancellationToken cancellationToken);

    Task<LegacyImportReportDto> ImportVouchersAsync(string vahedCode, int year, bool commit, string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LegacyVoucherYearDto>> GetVoucherStatusAsync(string vahedCode, CancellationToken cancellationToken);

    /// <summary>فهرست شمارهٔ سند جدید ← شمارهٔ سند قدیم یک سال منتقل‌شده.</summary>
    Task<LegacyFileDto> BuildVoucherMapAsync(string vahedCode, int year, CancellationToken cancellationToken);
}
