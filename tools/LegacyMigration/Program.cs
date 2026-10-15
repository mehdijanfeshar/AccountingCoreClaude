using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// میزبان خط فرمان برای همان ILegacyImportService که صفحهٔ «انتقال از سیستم قدیم» استفاده می‌کند (ستاد/پشتیبانی).
// اتصال‌ها و رمزها فقط از متغیر محیطی: MIGRATION_NEW_CONN (دیتابیس جدید)، MIGRATION_OLD_PASSWORD (اسکیماهای قدیم).

const string Usage = """
    LegacyMigration sources        --vahed 9516
    LegacyMigration chart-workbook --units 9516[,1155] --out chart.xlsx
    LegacyMigration chart-import   --excel chart.xlsx [--commit]
    LegacyMigration unit-workbook  --vahed 9516 --out unit-9516.xlsx
    LegacyMigration unit-import    --vahed 9516 --excel unit-9516.xlsx [--commit]
    گزینه‌های مشترک: --server host:port/service یا نام TNS (چند بار)  --schema AICENTRALACCOUNT  --user LEGACYMIG
    """;

Console.OutputEncoding = System.Text.Encoding.UTF8;
if (args.Length == 0) { Console.Error.WriteLine(Usage); return 2; }

var opt = new Dictionary<string, string>();
var servers = new List<string>();
var commit = false;
for (var i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--commit": commit = true; break;
        case "--server": servers.Add(args[++i]); break;
        default: opt[args[i].TrimStart('-')] = args[++i]; break;
    }
}
string Opt(string k, string? def = null) => opt.TryGetValue(k, out var v) ? v : def ?? throw new ArgumentException($"--{k} لازم است.\n{Usage}");

var settings = new Dictionary<string, string?>
{
    ["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("MIGRATION_NEW_CONN"),
    ["Database:Schema"] = Opt("schema", "AICENTRALACCOUNT"),
    ["Audit:Enabled"] = "false",
    ["LegacyImport:Password"] = Environment.GetEnvironmentVariable("MIGRATION_OLD_PASSWORD"),
};
for (var i = 0; i < servers.Count; i++)
{
    settings[$"LegacyImport:Servers:{i}:Name"] = $"server{i + 1}";
    settings[$"LegacyImport:Servers:{i}:DataSource"] = servers[i];
}
var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
var services = new ServiceCollection().AddLogging().AddInfrastructure(config)
    .AddSingleton<ISecretProtector, NoSecretStore>().BuildServiceProvider();
using var scope = services.CreateScope();
var svc = scope.ServiceProvider.GetRequiredService<ILegacyImportService>();
var user = Opt("user", "LEGACYMIG");

void Print(LegacyImportReportDto r)
{
    foreach (var s in r.Info) Console.WriteLine(s);
    foreach (var s in r.Warnings) Console.WriteLine("⚠ " + s);
    foreach (var s in r.Errors) Console.WriteLine("✖ " + s);
    Console.WriteLine(r.Committed ? "== ثبت شد" : r.Success ? "== بررسی موفق (ثبت نشد)" : $"== {r.Errors.Count} خطا");
}

switch (args[0])
{
    case "sources":
        foreach (var s in await svc.FindSourcesAsync(Opt("vahed"), default))
            Console.WriteLine($"{s.Server}\t{s.Schema}\t{s.Year}\t{(s.Connected ? $"OK ({s.Vouchers} سند)" : s.Error)}");
        break;
    case "chart-workbook":
    {
        var f = await svc.BuildChartWorkbookAsync(Opt("units").Split(','), default);
        await File.WriteAllBytesAsync(Opt("out"), f.Content);
        Console.WriteLine($"{Opt("out")} ({f.Content.Length:N0} بایت)");
        break;
    }
    case "chart-import":
        Print(await svc.ImportChartAsync(await File.ReadAllBytesAsync(Opt("excel")), commit, user, default));
        break;
    case "unit-workbook":
    {
        var f = await svc.BuildUnitWorkbookAsync(Opt("vahed"), default);
        await File.WriteAllBytesAsync(Opt("out"), f.Content);
        Console.WriteLine($"{Opt("out")} ({f.Content.Length:N0} بایت)");
        break;
    }
    case "unit-import":
        Print(await svc.ImportUnitCodingAsync(Opt("vahed"), await File.ReadAllBytesAsync(Opt("excel")), commit, user, default));
        break;
    default:
        Console.Error.WriteLine(Usage);
        return 2;
}
return 0;

/// <summary>خط فرمان اتصال دستی ذخیره نمی‌کند (رمز از MIGRATION_OLD_PASSWORD)؛ فقط اتصال‌های ذخیره‌شده را نمی‌خواند.</summary>
internal sealed class NoSecretStore : ISecretProtector
{
    public string Protect(string plain) => throw new NotSupportedException("در خط فرمان اتصال دستی ذخیره نمی‌شود.");
    public string? Unprotect(string protectedValue) => null;
}
