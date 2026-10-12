using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Accounting.Api.Licensing;

public enum LicenseState
{
    /// <summary>تا تاریخ انقضا — همه‌چیز عادی.</summary>
    Valid,
    /// <summary>پس از انقضا، در مهلت ارفاقی — کار عادی با هشدار.</summary>
    Grace,
    /// <summary>پس از مهلت (یا لایسنس نامعتبر/نبود/واحد بی‌مجوز) — فقط خواندنی.</summary>
    ReadOnly,
}

public sealed record LicenseStatus(
    LicenseState State,
    string? Customer,
    DateOnly? ExpiresAt,
    int? DaysLeft,
    int? GraceDaysLeft,
    string? Problem);

/// <summary>یک واحد در لایسنس به‌تفکیک واحد، با وضعیت امروزش.</summary>
public sealed record LicenseUnitStatus(string Code, LicenseStatus Status);

/// <summary>
/// لایسنس آفلاین امضاشده (RSA-SHA256). فایل را «پنل لایسنس توسعه‌دهنده» (<c>Accounting.LicenseStudio</c>)
/// با کلید خصوصی صادرکننده می‌سازد؛ این‌جا فقط با کلید عمومیِ تعبیه‌شده در کد راستی‌آزمایی می‌شود (نه از
/// کانفیگ، تا مشتری با عوض‌کردن آن لایسنس جعلی نپذیراند). فایل هر دقیقه دوباره خوانده می‌شود، پس تمدید بدون
/// ری‌استارت است.
/// <para>
/// دو حالت: <b>کل نصب</b> (یک <c>expiresAt</c>)، یا <b>به‌تفکیک واحد</b> (فهرست <c>units</c> با تاریخ هر
/// واحد؛ واحدی که در فهرست نیست مجاز نیست). واحدِ هر درخواست همان واحدی است که کاربر در آن کار می‌کند
/// (هدر <c>X-Vahed-Code</c> یا واحد خودش).
/// </para>
/// <para>
/// برای جلوگیری از عقب‌بردن ساعت سرور، بیشترین تاریخ دیده‌شده کنار فایل لایسنس (<c>.lastseen</c>)
/// نگه داشته می‌شود و «امروز» هرگز از آن کوچک‌تر حساب نمی‌شود.
/// </para>
/// </summary>
public sealed class LicenseService
{
    // کلید عمومی صادرکننده. کلید خصوصی بیرون از ریپو نگه‌داری می‌شود.
    private const string PublicKeyBase64 =
        "MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEA26VEUwZ1gnNO1iuCyNumw+Xnys7qTf/TdMDZBWGl8BrXmHkqcxyidLp1uQSX3SZHrOFmPHtQUHhsfheTPRVuiIPqh570iCLsZu/jOIRehJRXITd2TQxe0QBw5OODOS66Np8oGsxnUhXCe953s+mJdlUPNaXnJU6e7eLbem7Gn0rdTKEM3UjtNqfWNwKlj7TOeHNAWqegisrDHeVFLVWxeXoyyPAsA7hdUIyzhAs+EwVoshes52sLn8EyuAMoGs26fGYfbDNP67jz2abyDD4FWyXTidAr/KFDMhJM8fzod7IrpHoz1Z+A4lK0XMlGTlUDnTKyWa1CL5XGoHco0y2sm7L9co97ZWOEaN1IU+VyCk01n2CZ5FdMczd1XqKW7txwTBFXHfMnbeqxse5BIESW2yYyzTc4QVa4qH/ANOi2DEBf3o7X4ZhyYpvF560Tg7QNwvH0NSv/oGzwul//+//vQUqBQD9rofm22CRqB1E9kfJ8H3PPUYlMh6JugxTvQLIFAgMBAAE=";

    private static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(1);

    private readonly IConfiguration _configuration;
    private readonly string _path;
    private readonly ILogger<LicenseService> _logger;
    private readonly object _gate = new();
    private Loaded? _cached;
    private DateTime _cachedAtUtc;

    public LicenseService(IConfiguration configuration, IWebHostEnvironment env, ILogger<LicenseService> logger)
    {
        _configuration = configuration;
        var configured = configuration["License:Path"];
        _path = Path.IsPathRooted(configured ?? "")
            ? configured!
            : Path.Combine(env.ContentRootPath, string.IsNullOrWhiteSpace(configured) ? "license.lic" : configured);
        _logger = logger;
    }

    // هر بار از کانفیگ خوانده می‌شود تا تغییر appsettings بدون ری‌استارت اعمال شود.
    public bool Enforce => _configuration.GetValue("License:Enforce", false);

    /// <summary>true ⇒ لایسنس به‌تفکیک واحد است.</summary>
    public bool IsPerUnit => Load().Units is not null;

    /// <summary>
    /// وضعیت برای واحد <paramref name="unitCode"/>. در حالت کل‌نصب، واحد نادیده گرفته می‌شود. در حالت
    /// به‌تفکیک واحد: واحدِ ناشناخته (<c>null</c>) = بهترین وضعیت میان واحدها (برای صفحهٔ ورود)، و واحد
    /// غیرموجود در فهرست = فقط‌خواندنی.
    /// </summary>
    public LicenseStatus GetStatus(string? unitCode = null)
    {
        // بدون Enforce هم فایل (اگر بارگذاری شده) خوانده و نشان داده می‌شود؛ فقط هیچ‌چیز بسته نمی‌شود.
        if (!Enforce && !File.Exists(_path))
        {
            return new LicenseStatus(LicenseState.Valid, null, null, null, null, null);
        }

        var loaded = Load();
        if (loaded.Problem is not null)
        {
            return new LicenseStatus(LicenseState.ReadOnly, null, null, null, null, loaded.Problem);
        }

        var today = TodayGuarded();
        if (loaded.Units is null)
        {
            return Compute(loaded.Customer, loaded.ExpiresAt!.Value, loaded.GraceDays, today);
        }

        if (string.IsNullOrWhiteSpace(unitCode))
        {
            return loaded.Units
                .Select(u => Compute(loaded.Customer, u.ExpiresAt, loaded.GraceDays, today))
                .OrderBy(s => s.State)
                .ThenByDescending(s => s.ExpiresAt)
                .FirstOrDefault()
                ?? new LicenseStatus(LicenseState.ReadOnly, loaded.Customer, null, null, null, "لایسنس هیچ واحدی ندارد.");
        }

        var unit = loaded.Units.FirstOrDefault(u => string.Equals(u.Code, unitCode, StringComparison.Ordinal));
        return unit is null
            ? new LicenseStatus(LicenseState.ReadOnly, loaded.Customer, null, null, null, "برای این واحد لایسنس صادر نشده است.")
            : Compute(loaded.Customer, unit.ExpiresAt, loaded.GraceDays, today);
    }

    /// <summary>وضعیت همهٔ واحدهای لایسنس (فقط در حالت به‌تفکیک واحد؛ وگرنه خالی).</summary>
    public IReadOnlyList<LicenseUnitStatus> GetUnitStatuses()
    {
        var loaded = Load();
        if (loaded.Problem is not null || loaded.Units is null)
        {
            return [];
        }

        var today = TodayGuarded();
        return loaded.Units
            .Select(u => new LicenseUnitStatus(u.Code, Compute(loaded.Customer, u.ExpiresAt, loaded.GraceDays, today)))
            .ToList();
    }

    /// <summary>
    /// فایل لایسنس تازه را راستی‌آزمایی می‌کند و فقط اگر امضایش درست باشد جای فایل فعلی می‌نشاند.
    /// <c>false</c> یعنی فایل نامعتبر بود و چیزی ذخیره نشد.
    /// </summary>
    public bool Install(string fileText)
    {
        if (Parse(fileText).Problem is not null)
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, fileText);
        lock (_gate)
        {
            _cached = null;
        }

        return true;
    }

    private Loaded Load()
    {
        lock (_gate)
        {
            if (_cached is null || DateTime.UtcNow - _cachedAtUtc > RefreshEvery)
            {
                _cached = ReadFile();
                _cachedAtUtc = DateTime.UtcNow;
            }

            return _cached;
        }
    }

    private Loaded ReadFile()
    {
        if (!File.Exists(_path))
        {
            return Loaded.Bad("فایل لایسنس یافت نشد.");
        }

        try
        {
            return Parse(File.ReadAllText(_path));
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "License file {Path} could not be read.", _path);
            return Loaded.Bad("فایل لایسنس خراب است.");
        }
    }

    private Loaded Parse(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var payloadBytes = Convert.FromBase64String(doc.RootElement.GetProperty("payload").GetString()!);
            var signature = Convert.FromBase64String(doc.RootElement.GetProperty("signature").GetString()!);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKeyBase64), out _);
            if (!rsa.VerifyData(payloadBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                return Loaded.Bad("امضای لایسنس معتبر نیست.");
            }

            var p = JsonSerializer.Deserialize<Payload>(payloadBytes, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            if (p.Units is { Count: > 0 })
            {
                var units = p.Units.Select(u => new UnitEntry(u.Code, ParseDate(u.ExpiresAt))).ToList();
                return new Loaded(p.Customer, null, p.GraceDays, units, null);
            }

            return new Loaded(p.Customer, ParseDate(p.ExpiresAt!), p.GraceDays, null, null);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or KeyNotFoundException
            or CryptographicException or InvalidOperationException or ArgumentException or NullReferenceException)
        {
            _logger.LogWarning(ex, "License file {Path} could not be read.", _path);
            return Loaded.Bad("فایل لایسنس خراب است.");
        }
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static LicenseStatus Compute(string customer, DateOnly expires, int graceDays, DateOnly today)
    {
        var daysLeft = expires.DayNumber - today.DayNumber;
        if (daysLeft >= 0)
        {
            return new LicenseStatus(LicenseState.Valid, customer, expires, daysLeft, null, null);
        }

        var graceLeft = graceDays + daysLeft;
        return graceLeft >= 0
            ? new LicenseStatus(LicenseState.Grace, customer, expires, daysLeft, graceLeft, null)
            : new LicenseStatus(LicenseState.ReadOnly, customer, expires, daysLeft, 0, "لایسنس منقضی شده است.");
    }

    /// <summary>امروز، به‌شرط آنکه ساعت سرور عقب نرفته باشد (بیشترین تاریخ دیده‌شده را نگه می‌دارد).</summary>
    private DateOnly TodayGuarded()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stamp = _path + ".lastseen";
        try
        {
            if (File.Exists(stamp)
                && DateOnly.TryParseExact(File.ReadAllText(stamp).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var seen)
                && seen > today)
            {
                return seen;
            }

            File.WriteAllText(stamp, today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        catch (IOException)
        {
            // نوشتن ممکن نیست؛ همان تاریخ سیستم.
        }
        catch (UnauthorizedAccessException)
        {
        }

        return today;
    }

    private sealed record UnitEntry(string Code, DateOnly ExpiresAt);

    private sealed record Loaded(string Customer, DateOnly? ExpiresAt, int GraceDays, List<UnitEntry>? Units, string? Problem)
    {
        public static Loaded Bad(string problem) => new("", null, 0, null, problem);
    }

    private sealed record PayloadUnit(string Code, string ExpiresAt);

    private sealed record Payload(string Customer, string IssuedAt, string? ExpiresAt, int GraceDays, List<PayloadUnit>? Units);
}
