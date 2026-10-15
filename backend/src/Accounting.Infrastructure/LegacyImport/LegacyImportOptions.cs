namespace Accounting.Infrastructure.LegacyImport;

/// <summary>
/// پیکربندی <c>LegacyImport</c>. سیستم قدیم برای هر واحد و هر سال یک اسکیما دارد (مثلاً <c>ACC951600</c> = واحد
/// ۹۵۱۶ سال ۱۴۰۰) و اسکیماها روی چند سرور پخش‌اند. رمزها فقط از User Secrets/محیط می‌آیند.
/// </summary>
public sealed class LegacyImportOptions
{
    public const string Section = "LegacyImport";

    public List<LegacyServerOptions> Servers { get; set; } = new();

    /// <summary>رمز پیش‌فرض اسکیماهای قدیم (هر سرور می‌تواند رمز خودش را داشته باشد).</summary>
    public string Password { get; set; } = "";

    /// <summary>نام کاربر/اسکیما؛ <c>{vahed}</c> = کد واحد، <c>{yy}</c> = دو رقم آخر سال.</summary>
    public string SchemaPattern { get; set; } = "ACC{vahed}{yy}";

    public int FromYear { get; set; } = 1390;
    public int ToYear { get; set; } = 1405;

    /// <summary>پوشهٔ نگهداری کاربرگ‌های پذیرفته‌شده (برای مرحلهٔ انتقال اسناد).</summary>
    public string StoragePath { get; set; } = "App_Data/legacy-import";

    /// <summary>دستهٔ واحد برای تفصیلی‌های واحد: 1=بیمه‌ای، 2=درمانی.</summary>
    public short TafsiliVahedType { get; set; } = 2;

    public string ActivityGroupName { get; set; } = "فعالیت";
    public string CostCenterGroupName { get; set; } = "مرکز هزینه";
    public int ConnectTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// کلید خصوصی RSA سیستم قدیم (PEM یا XML) برای خواندن نام کاربر/رمز رمزگذاری‌شده در فایل XML سرورها. فقط از
    /// User Secrets/محیط؛ اگر خالی باشد، کاربر نام کاربر و رمز را دستی وارد می‌کند.
    /// </summary>
    public string XmlPrivateKey { get; set; } = "";

    /// <summary>کلید XOR برنامهٔ قدیم که پس از RSA روی نام کاربر/رمز اعمال می‌شود — فقط از تنظیمات/User Secrets.</summary>
    public string XmlXorKey { get; set; } = "";
}

/// <param name="DataSource">نام TNS یا آدرس EZConnect (<c>host:port/service</c>).</param>
public sealed class LegacyServerOptions
{
    public string Name { get; set; } = "";
    public string DataSource { get; set; } = "";
    public string? Password { get; set; }
}
