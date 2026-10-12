using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// پنل لایسنس توسعه‌دهنده — فقط روی سیستم خودتان اجرا شود، نه سرور مشتری (کلید خصوصی اینجاست).
//   dotnet run --project backend/tools/Accounting.LicenseStudio
// پوشهٔ داده (private.pem + registry.json): متغیر LICENSE_STUDIO_DIR، پیش‌فرض D:\AiProj\LicenseKeys.
// فقط روی localhost گوش می‌دهد؛ هر درخواست باید هدر X-Studio داشته باشد (جلوی فرم‌های سایت‌های دیگر).

var dataDir = Environment.GetEnvironmentVariable("LICENSE_STUDIO_DIR") ?? @"D:\AiProj\LicenseKeys";
var keyPath = Path.Combine(dataDir, "private.pem");
var registryPath = Path.Combine(dataDir, "registry.json");
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never };
var gate = new object();

Registry LoadRegistry() =>
    File.Exists(registryPath) ? JsonSerializer.Deserialize<Registry>(File.ReadAllText(registryPath), json)! : new Registry();

void SaveRegistry(Registry r)
{
    Directory.CreateDirectory(dataDir);
    File.WriteAllText(registryPath + ".tmp", JsonSerializer.Serialize(r, json), new UTF8Encoding(false));
    File.Move(registryPath + ".tmp", registryPath, overwrite: true);
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("LICENSE_STUDIO_URL") ?? "http://127.0.0.1:5300");
var app = builder.Build();

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api") && !ctx.Request.Headers.ContainsKey("X-Studio"))
    {
        ctx.Response.StatusCode = 403;
        return;
    }

    await next();
});

app.MapGet("/", () => Results.File(Path.Combine(AppContext.BaseDirectory, "index.html"), "text/html; charset=utf-8"));

app.MapGet("/api/state", () =>
{
    lock (gate)
    {
        return Results.Json(new { keyOk = File.Exists(keyPath), dir = dataDir, customers = LoadRegistry().Customers }, json);
    }
});

app.MapPost("/api/customers", (CustomerInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest("نام مشتری خالی است.");
    lock (gate)
    {
        var r = LoadRegistry();
        var c = new Customer { Id = Guid.NewGuid().ToString("N"), Name = input.Name.Trim(), Notes = input.Notes?.Trim() };
        r.Customers.Add(c);
        SaveRegistry(r);
        return Results.Json(c, json);
    }
});

app.MapPost("/api/customers/{id}/update", (string id, CustomerInput input) =>
{
    lock (gate)
    {
        var r = LoadRegistry();
        var c = r.Customers.FirstOrDefault(x => x.Id == id);
        if (c is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest("نام مشتری خالی است.");
        c.Name = input.Name.Trim();
        c.Notes = input.Notes?.Trim();
        SaveRegistry(r);
        return Results.Json(c, json);
    }
});

app.MapPost("/api/customers/{id}/delete", (string id) =>
{
    lock (gate)
    {
        var r = LoadRegistry();
        r.Customers.RemoveAll(x => x.Id == id);
        SaveRegistry(r);
        return Results.Ok();
    }
});

app.MapPost("/api/customers/{id}/licenses", (string id, IssueInput input) =>
{
    if (!File.Exists(keyPath)) return Results.BadRequest($"کلید خصوصی پیدا نشد: {keyPath}");

    var hasUnits = input.Units is { Count: > 0 };
    if (!hasUnits && !IsDate(input.ExpiresAt)) return Results.BadRequest("تاریخ انقضا نامعتبر است.");
    if (hasUnits && input.Units!.Any(u => string.IsNullOrWhiteSpace(u.Code) || !IsDate(u.ExpiresAt)))
        return Results.BadRequest("کد یا تاریخ یکی از واحدها نامعتبر است.");
    if (hasUnits && input.Units!.Select(u => u.Code.Trim()).Distinct().Count() != input.Units!.Count)
        return Results.BadRequest("کد واحد تکراری است.");
    if (input.GraceDays is < 0 or > 365) return Results.BadRequest("مهلت ارفاقی باید بین ۰ تا ۳۶۵ روز باشد.");

    lock (gate)
    {
        var r = LoadRegistry();
        var c = r.Customers.FirstOrDefault(x => x.Id == id);
        if (c is null) return Results.NotFound();

        var issuedAt = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            customer = c.Name,
            issuedAt,
            expiresAt = hasUnits ? null : input.ExpiresAt,
            graceDays = input.GraceDays,
            units = hasUnits ? input.Units!.Select(u => new { code = u.Code.Trim(), expiresAt = u.ExpiresAt }).ToArray() : null,
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(keyPath));
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var file = JsonSerializer.Serialize(new { payload = Convert.ToBase64String(payload), signature = Convert.ToBase64String(signature) });

        var lic = new IssuedLicense
        {
            Id = Guid.NewGuid().ToString("N"),
            IssuedAt = issuedAt,
            ExpiresAt = hasUnits ? null : input.ExpiresAt,
            GraceDays = input.GraceDays,
            Units = hasUnits ? input.Units!.Select(u => new UnitLicense { Code = u.Code.Trim(), ExpiresAt = u.ExpiresAt }).ToList() : null,
            Note = input.Note?.Trim(),
            File = file,
        };
        c.Licenses.Add(lic);
        SaveRegistry(r);
        return Results.Json(lic, json);
    }
});

app.MapGet("/api/licenses/{id}/file", (string id) =>
{
    lock (gate)
    {
        foreach (var c in LoadRegistry().Customers)
        {
            var lic = c.Licenses.FirstOrDefault(l => l.Id == id);
            if (lic is not null)
            {
                return Results.File(Encoding.UTF8.GetBytes(lic.File), "application/octet-stream", "license.lic");
            }
        }

        return Results.NotFound();
    }
});

Console.WriteLine($"پنل لایسنس: {builder.WebHost.GetSetting("urls") ?? "http://127.0.0.1:5300"}  — پوشهٔ داده: {dataDir}");
app.Run();

static bool IsDate(string? s) =>
    DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

record CustomerInput(string Name, string? Notes);
record UnitInput(string Code, string ExpiresAt);
record IssueInput(string? ExpiresAt, int GraceDays, List<UnitInput>? Units, string? Note);

class Registry { public List<Customer> Customers { get; set; } = []; }

class Customer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Notes { get; set; }
    public List<IssuedLicense> Licenses { get; set; } = [];
}

class UnitLicense
{
    public string Code { get; set; } = "";
    public string ExpiresAt { get; set; } = "";
}

class IssuedLicense
{
    public string Id { get; set; } = "";
    public string IssuedAt { get; set; } = "";
    public string? ExpiresAt { get; set; }
    public int GraceDays { get; set; }
    public List<UnitLicense>? Units { get; set; }
    public string? Note { get; set; }
    public string File { get; set; } = "";
}
