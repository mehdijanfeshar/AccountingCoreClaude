using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure.Legacy;
using Oracle.ManagedDataAccess.Client;

namespace Accounting.Infrastructure.LegacyImport;

/// <summary>
/// کاربرگ‌های پذیرفته‌شده روی سرور: <c>chart.xlsx</c> (کدینگ مشترک) و <c>unit-{vahed}.xlsx</c> (کدینگ واحد، با کد
/// نهایی تفصیلی‌ها). انتقال اسناد از همین فایل‌ها تطبیق را می‌خواند؛ جدول تازه‌ای (و DDL) لازم نیست.
/// </summary>
internal sealed class LegacyImportStorage(LegacyImportOptions options)
{
    private string Dir
    {
        get
        {
            var d = Path.IsPathRooted(options.StoragePath) ? options.StoragePath : Path.Combine(AppContext.BaseDirectory, options.StoragePath);
            Directory.CreateDirectory(d);
            return d;
        }
    }

    public string ChartPath => Path.Combine(Dir, "chart.xlsx");
    public string UnitPath(string vahed) => Path.Combine(Dir, $"unit-{vahed}.xlsx");
    public string ConnectionsPath(string vahed) => Path.Combine(Dir, $"connections-{vahed}.json");
    public string VouchersPath(string vahed, int year) => Path.Combine(Dir, $"vouchers-{vahed}-{year}.json");
    public string BankPath(string vahed) => Path.Combine(Dir, $"bank-{vahed}.json");
    public string DecisionsPath(string vahed, int year) => Path.Combine(Dir, $"decisions-{vahed}-{year}.json");
    public IEnumerable<string> VoucherFiles(string vahed) => Directory.EnumerateFiles(Dir, $"vouchers-{vahed}-*.json");

    public DateTime? When(string path) => File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;
    public byte[]? Read(string path) => File.Exists(path) ? File.ReadAllBytes(path) : null;
    public void Save(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);
}

internal sealed class LegacyImportService(
    LegacyDbContext db,
    LegacySourceLocator locator,
    LegacyImportStorage storage,
    LegacyImportOptions options,
    ISecretProtector protector,
    LegacyXmlCredentials xmlCredentials,
    IMemoryCache cache) : ILegacyImportService
{
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// دادهٔ خوانده‌شدهٔ سیستم قدیم برای «بررسی»‌های پشت‌سرهم (هر بررسی دوباره پس از تصمیم کاربر) چند دقیقه نگه داشته
    /// می‌شود؛ تغییر اتصال‌ها کلید را عوض می‌کند. ثبت نهایی همیشه دادهٔ تازه می‌خواند.
    /// </summary>
    private async Task<LegacyUnit> LoadUnitAsync(string vahed, bool fresh, CancellationToken ct)
    {
        var key = $"legacy-unit:{vahed}:{storage.When(storage.ConnectionsPath(vahed))?.Ticks}";
        if (!fresh && cache.TryGetValue(key, out LegacyUnit? hit) && hit != null) return hit;
        var unit = await locator.LoadUnitAsync(vahed, ManualSources(vahed), ct);
        cache.Set(key, unit, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(15) });
        return unit;
    }

    public async Task<IReadOnlyList<LegacySourceDto>> FindSourcesAsync(string vahedCode, CancellationToken ct)
    {
        var manual = ManualSources(vahedCode);
        return (manual.Count > 0 ? await locator.ProbeAsync(manual, vahedCode, ct) : await locator.DiscoverAsync(vahedCode, ct)).Report;
    }

    // ---------- اتصال دستی: یک سرور برای واحد + اسکیمای هر سال (رمزها با ISecretProtector؛ هرگز برگردانده نمی‌شوند) ----------
    private sealed record StoredServer(string DataSource, string? Title, string? UserName, string? ProtectedPassword);
    private sealed record StoredYear(int Year, string Schema, string? UserName, string? ProtectedPassword);
    private sealed record StoredConnections(StoredServer? Server, List<StoredYear> Years);
    private sealed record XmlCredential(string UserName, string Password);

    private StoredConnections ReadConnections(string vahed)
    {
        var json = storage.Read(storage.ConnectionsPath(vahed));
        if (json == null) return new(null, new());
        try { return JsonSerializer.Deserialize<StoredConnections>(json) ?? new(null, new()); }
        catch (JsonException) { return new(null, new()); } // قالب قدیمی فایل
    }

    private string? Plain(string? protectedValue) => protectedValue is null ? null : protector.Unprotect(protectedValue);

    private LegacySourceLocator.Source ToSource(StoredServer server, StoredYear y)
    {
        var user = string.IsNullOrWhiteSpace(y.UserName) ? server.UserName ?? y.Schema : y.UserName!;
        var password = (string.IsNullOrWhiteSpace(y.UserName) ? null : Plain(y.ProtectedPassword)) ?? Plain(server.ProtectedPassword) ?? "";
        return new LegacySourceLocator.Source(server.Title ?? server.DataSource, y.Schema.ToUpperInvariant(), y.Year,
            locator.ConnectionString(server.DataSource, user, password), !user.Equals(y.Schema, StringComparison.OrdinalIgnoreCase));
    }

    private List<LegacySourceLocator.Source> ManualSources(string vahed)
    {
        var c = ReadConnections(vahed);
        return c.Server == null ? new() : c.Years.OrderBy(y => y.Year).Select(y => ToSource(c.Server, y)).ToList();
    }

    public Task<LegacyConnectionsDto> GetConnectionsAsync(string vahedCode, CancellationToken ct)
    {
        var c = ReadConnections(vahedCode);
        var server = c.Server == null ? null
            : new LegacyServerDto(c.Server.DataSource, c.Server.Title, c.Server.UserName, Plain(c.Server.ProtectedPassword) != null);
        return Task.FromResult(new LegacyConnectionsDto(server, c.Years.OrderBy(y => y.Year)
            .Select(y => new LegacyYearDto(y.Year, y.Schema, y.UserName, Plain(y.ProtectedPassword) != null)).ToList()));
    }

    public async Task<LegacyConnectionsResultDto> SaveConnectionsAsync(string vahedCode, LegacyConnectionsInput input, bool checkOnly, CancellationToken ct)
    {
        var old = ReadConnections(vahedCode);
        var s = input.Server;

        // رمز سرور: ورودی تازه ← رمز رمزگشایی‌شده از XML ← رمز ذخیره‌شده. نام کاربر XML همان نام اسکیما (بدون سال)
        // است و مرورگر اسکیمای هر سال را از آن می‌سازد؛ ورود با اسکیمای هر سال است، نه با آن.
        var serverUser = string.IsNullOrWhiteSpace(s.UserName) ? null : s.UserName.Trim();
        var serverPassword = string.IsNullOrEmpty(s.Password) ? null : protector.Protect(s.Password);
        if (!string.IsNullOrEmpty(s.CredentialToken) && Plain(s.CredentialToken) is { } tokenJson
            && JsonSerializer.Deserialize<XmlCredential>(tokenJson) is { } xml)
            serverPassword ??= protector.Protect(xml.Password);
        serverPassword ??= old.Server?.ProtectedPassword;
        var server = new StoredServer(s.DataSource.Trim(), string.IsNullOrWhiteSpace(s.Title) ? null : s.Title.Trim(), serverUser, serverPassword);

        var oldYears = old.Years.ToDictionary(y => y.Year);
        var years = input.Years.OrderBy(y => y.Year).Select(y =>
        {
            var user = string.IsNullOrWhiteSpace(y.UserName) ? null : y.UserName.Trim();
            var pw = string.IsNullOrEmpty(y.Password) ? (user != null ? oldYears.GetValueOrDefault(y.Year)?.ProtectedPassword : null) : protector.Protect(y.Password);
            return new StoredYear(y.Year, y.Schema.Trim().ToUpperInvariant(), user, pw);
        }).ToList();

        if (checkOnly && years.Count == 0)
            return new LegacyConnectionsResultDto(false, await DiscoverYearsAsync(vahedCode, server, s.SchemaBase, ct));

        var results = new List<LegacySourceDto>();
        foreach (var y in years)
        {
            if (server.UserName == null && y.UserName == null && Plain(server.ProtectedPassword) == null && Plain(y.ProtectedPassword) == null)
            {
                results.Add(new LegacySourceDto(server.Title ?? server.DataSource, y.Schema, y.Year, false, "نام کاربر و رمز تعیین نشده", 0));
                continue;
            }
            results.Add(await locator.ProbeOneAsync(ToSource(server, y), vahedCode, ct));
        }

        var ok = results.Count > 0 && results.All(r => r.Connected);
        if (ok && !checkOnly)
            storage.Save(storage.ConnectionsPath(vahedCode), JsonSerializer.SerializeToUtf8Bytes(new StoredConnections(server, years)));
        return new LegacyConnectionsResultDto(ok && !checkOnly, results);
    }

    /// <summary>
    /// اسکیماهای سالانهٔ واحد روی سرور داده‌شده. با کاربر سرور: یک ورود و فهرست ALL_USERS؛ بدون آن (ورود با نام
    /// اسکیما): هر سال یک تلاش تا نخستین ورود موفق، سپس ALL_USERS — تا ورود ناموفق روی یک کاربر تکرار نشود.
    /// </summary>
    private async Task<List<LegacySourceDto>> DiscoverYearsAsync(string vahed, StoredServer server, string? schemaBase, CancellationToken ct)
    {
        var name = server.Title ?? server.DataSource;
        LegacySourceDto Fail(string error) => new(name, "", 0, false, error, 0);
        if (Plain(server.ProtectedPassword) == null)
            return [Fail("رمز تعیین نشده")];

        var prefix = string.IsNullOrWhiteSpace(schemaBase) ? null : schemaBase.Trim().ToUpperInvariant();
        var candidates = Enumerable.Range(options.FromYear, Math.Max(0, options.ToYear - options.FromYear + 1))
            .Select(y => new StoredYear(y, prefix != null ? prefix + (y % 100).ToString("00")
                : options.SchemaPattern.Replace("{vahed}", vahed).Replace("{yy}", (y % 100).ToString("00")).ToUpperInvariant(), null, null))
            .ToList();
        HashSet<string>? existing = null;
        foreach (var y in server.UserName != null ? candidates.Take(1) : candidates)
        {
            var source = ToSource(server, y);
            try
            {
                await using var cn = new OracleConnection(source.Connection);
                await cn.OpenAsync(ct);
                existing = (await OldYear.ListAsync(cn, "select username from all_users", r => r.GetString(0), ct))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                break;
            }
            catch (OracleException ex) when (ex.Number == 1017 && server.UserName == null) { } // این سال اسکیما ندارد
            catch (OracleException)
            {
                var dto = await locator.ProbeOneAsync(source with { SetSchema = false }, vahed, ct); // پیام خطای فارسی
                return [dto with { Year = 0, Schema = "" }];
            }
        }

        var results = new List<LegacySourceDto>();
        if (existing != null)
            foreach (var y in candidates.Where(c => existing.Contains(c.Schema)))
                results.Add(await locator.ProbeOneAsync(ToSource(server, y), vahed, ct));
        return results.Count > 0 ? results : [Fail($"هیچ اسکیمای سالانه‌ای برای واحد {vahed} روی این سرور پیدا نشد")];
    }

    public LegacyXmlServerDto ReadXmlServer(LegacyXmlServerInput input)
    {
        if (!xmlCredentials.Configured)
            return new(false, null, null, "کلید رمزگشایی فایل XML سیستم قدیم در تنظیمات سامانه نیست؛ رمز را دستی وارد کنید.");
        // نام کاربر هر سال از کد واحد و سال ساخته می‌شود (ACC + واحد + yy)؛ از فایل فقط رمز لازم است
        var password = xmlCredentials.Decrypt(input.EncryptedPassword);
        if (password == null)
            return new(false, null, null, "رمز این سرور از فایل خوانده نشد؛ آن را وارد کنید.");
        var user = xmlCredentials.Decrypt(input.EncryptedUserName)?.Trim();
        var token = protector.Protect(JsonSerializer.Serialize(new XmlCredential(user ?? "", password)));
        return new(true, user, token, null);
    }

    public Task<LegacyImportStatusDto> GetStatusAsync(string vahedCode, CancellationToken ct)
    {
        var chart = storage.When(storage.ChartPath);
        var unit = storage.When(storage.UnitPath(vahedCode));
        return Task.FromResult(new LegacyImportStatusDto(chart != null, chart, unit != null, unit, storage.When(storage.BankPath(vahedCode))));
    }

    public async Task<LegacyFileDto> BuildChartWorkbookAsync(IReadOnlyList<string> vahedCodes, CancellationToken ct)
    {
        var units = new List<LegacyUnit>();
        foreach (var v in vahedCodes.Distinct()) units.Add(await locator.LoadUnitAsync(v, ManualSources(v), ct));
        var bytes = ChartWorkbook.Build(units, await ChartWorkbook.LoadTreeAsync(db, ct));
        return new LegacyFileDto($"legacy-chart-{string.Join("-", vahedCodes)}.xlsx", Xlsx, bytes);
    }

    public Task<LegacyImportReportDto> ImportChartAsync(byte[] workbook, bool commit, string userId, CancellationToken ct)
        => Guard(async report =>
        {
            var importer = new ChartImporter(db, ChartMapping.Read(workbook), userId, report);
            if (!importer.Validate()) return false;
            return await InTransactionAsync(commit, report, () => importer.RunAsync(ct), () => storage.Save(storage.ChartPath, workbook), ct);
        });

    public async Task<LegacyFileDto> BuildUnitWorkbookAsync(string vahedCode, int? year, CancellationToken ct)
    {
        if (year is { } y)
        {
            var sources = ManualSources(vahedCode).Where(s => s.Year == y).ToList();
            if (sources.Count == 0)
                throw new Accounting.Application.Common.Exceptions.BusinessRuleException($"سال {y} در اتصال‌های ذخیره‌شدهٔ این واحد نیست؛ اول اتصال‌ها را ذخیره کنید.");
            var one = LegacyUnitWorkbook.Build(await locator.LoadUnitAsync(vahedCode, sources, ct));
            return new LegacyFileDto($"legacy-unit-{vahedCode}-{y}.xlsx", Xlsx, one);
        }

        // اگر کدینگ واحد پذیرفته شده، همان فایل (با کدهای نهایی) را برمی‌گرداند
        var stored = storage.Read(storage.UnitPath(vahedCode));
        var bytes = stored ?? LegacyUnitWorkbook.Build(await locator.LoadUnitAsync(vahedCode, ManualSources(vahedCode), ct));
        return new LegacyFileDto($"legacy-unit-{vahedCode}.xlsx", Xlsx, bytes);
    }

    public Task<LegacyImportReportDto> ImportUnitCodingAsync(string vahedCode, byte[] workbook, bool commit, string userId, CancellationToken ct)
        => Guard(async report =>
        {
            var chartBytes = storage.Read(storage.ChartPath);
            if (chartBytes == null)
            {
                report.Errors.Add("کدینگ مشترک (گروه/کل/معین) هنوز توسط مدیر ستاد منتقل نشده است.");
                return false;
            }
            if (storage.When(storage.UnitPath(vahedCode)) != null)
            {
                report.Errors.Add($"کدینگ واحد {vahedCode} قبلاً منتقل شده است.");
                return false;
            }
            var unit = await locator.LoadUnitAsync(vahedCode, ManualSources(vahedCode), ct);
            report.Info.Add("سال‌های سیستم قدیم: " + string.Join("، ", unit.Years.Select(y => $"{y.Year} ({y.Schema})")));
            var importer = new LegacyUnitCodingImporter(db, unit, ChartMapping.Read(chartBytes), UnitMapping.Read(workbook), options, userId, report);
            if (!await importer.ValidateAsync(ct)) return false;
            return await InTransactionAsync(commit, report, () => importer.RunAsync(ct),
                () => storage.Save(storage.UnitPath(vahedCode), UnitMapping.WriteFinalCodes(workbook, importer.TafsiliCodes, importer.LowCodes)), ct);
        });

    // ---------- اطلاعات بانکی ----------
    public Task<LegacyImportReportDto> ImportBankAsync(string vahedCode, bool commit, string userId, CancellationToken ct)
        => Guard(async report =>
        {
            var chartBytes = storage.Read(storage.ChartPath);
            if (chartBytes == null)
            {
                report.Errors.Add("کدینگ مشترک (گروه/کل/معین) هنوز منتقل نشده است.");
                return false;
            }
            var unit = await locator.LoadUnitAsync(vahedCode, ManualSources(vahedCode), ct);
            var unitBytes = storage.Read(storage.UnitPath(vahedCode));
            var chart = ChartMapping.Read(chartBytes);
            var importer = new LegacyBankImporter(db, unit, chart, unitBytes == null ? null : UnitMapping.Read(unitBytes), userId, report);
            try
            {
                return await InTransactionAsync(commit, report, async () => { await EnsureMoeinsAsync(unit, chart, userId, report, ct); if (report.Ok) await importer.RunAsync(ct); },
                    () => storage.Save(storage.BankPath(vahedCode), JsonSerializer.SerializeToUtf8Bytes(new { At = DateTime.UtcNow, report.Info })), ct);
            }
            catch (DbUpdateException ex)
            {
                db.ChangeTracker.Clear();
                report.Errors.Add(DbError(ex));
                return false;
            }
            finally { db.BulkAuditLabel = null; }
        });

    // ---------- انتقال اسناد ----------
    public Task<LegacyImportReportDto> ImportVouchersAsync(string vahedCode, int year, bool commit, string userId, CancellationToken ct)
        => Guard(async report =>
        {
            var chartBytes = storage.Read(storage.ChartPath);
            var unitBytes = storage.Read(storage.UnitPath(vahedCode));
            if (chartBytes == null || unitBytes == null)
            {
                report.Errors.Add(chartBytes == null
                    ? "کدینگ مشترک (گروه/کل/معین) هنوز منتقل نشده است."
                    : $"کدینگ واحد {vahedCode} هنوز ثبت نهایی نشده است؛ اول کاربرگ کدینگ واحد را بارگذاری و ثبت کنید.");
                return false;
            }
            var unit = await LoadUnitAsync(vahedCode, fresh: commit, ct);
            var previous = LegacyVoucherImporter.Read(storage.Read(storage.VouchersPath(vahedCode, year)));
            var chart = ChartMapping.Read(chartBytes);
            var book = new IssueBook(IssueBook.Read(storage.Read(storage.DecisionsPath(vahedCode, year))));
            report.Issues = book;
            var importer = new LegacyVoucherImporter(db, unit, year, chart, UnitMapping.Read(unitBytes),
                previous, userId, report, book);
            try
            {
                var done = await InTransactionAsync(commit, report, async () => { await EnsureMoeinsAsync(unit, chart, userId, report, ct); if (report.Ok) await importer.RunAsync(ct); },
                    () => storage.Save(storage.VouchersPath(vahedCode, year), JsonSerializer.SerializeToUtf8Bytes(importer.Result!)), ct);
                if (!report.Ok && report.Errors.Count == 0)
                    report.Info.Add($"{book.Open} مورد هنوز تصمیم یا اصلاح می‌خواهد؛ تا همهٔ بخش‌ها بدون مشکل نشوند چیزی ثبت نمی‌شود.");
                return done;
            }
            catch (DbUpdateException ex)
            {
                db.ChangeTracker.Clear();
                report.Errors.Add(DbError(ex));
                return false;
            }
            catch (OracleException ex) when (ex.Number == 2292)
            {
                db.ChangeTracker.Clear();
                report.Errors.Add($"اسناد انتقال قبلی سال {year} در جای دیگری (مثل اعلامیه، دریافت/پرداخت یا خزانه) استفاده شده‌اند و "
                                  + "حذف و انتقال دوباره ممکن نیست؛ اول آن ارجاع‌ها را بردارید.");
                return false;
            }
            finally { db.BulkAuditLabel = null; }
        });

    public Task SaveVoucherDecisionsAsync(string vahedCode, int year, IReadOnlyList<LegacyDecisionInput> decisions, string userId, CancellationToken ct)
    {
        var path = storage.DecisionsPath(vahedCode, year);
        var all = IssueBook.Read(storage.Read(path));
        foreach (var d in decisions)
        {
            if (string.IsNullOrWhiteSpace(d.Option)) all.Remove(d.IssueId);
            else all[d.IssueId] = new IssueDecision(d.Option.Trim(), string.IsNullOrWhiteSpace(d.Value) ? null : d.Value.Trim(), userId, DateTime.UtcNow);
        }
        storage.Save(path, JsonSerializer.SerializeToUtf8Bytes(all));
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<LegacyLookupDto>> LookupAsync(string kind, string? search, CancellationToken ct)
    {
        var q = search?.Trim() ?? "";
        if (kind == "moein")
            return await db.TB_ACCOUNTCODEs.AsNoTracking()
                .Where(a => a.TYPECODE == Accounting.Domain.ValueObjects.TypeCodes.Moin && a.ISDELETED != true && a.ACCCODE != null)
                .Where(a => q == "" || a.ACCCODE!.StartsWith(q) || a.ACCCODENAME!.Contains(q))
                .OrderBy(a => a.ACCCODE).Take(50).Select(a => new LegacyLookupDto(a.ACCCODE!.Trim(), a.ACCCODENAME ?? "")).ToListAsync(ct);
        if (kind == "tafsili")
            return await db.TB_TAFSILIs.AsNoTracking()
                .Where(t => t.TAFSILI_CODE != null)
                .Where(t => q == "" || t.TAFSILI_CODE!.StartsWith(q) || t.TAFSILI_NAME!.Contains(q))
                .OrderBy(t => t.TAFSILI_CODE).Take(50).Select(t => new LegacyLookupDto(t.TAFSILI_CODE!.Trim(), t.TAFSILI_NAME ?? "")).ToListAsync(ct);
        return [];
    }

    public Task<IReadOnlyList<LegacyVoucherYearDto>> GetVoucherStatusAsync(string vahedCode, CancellationToken ct)
    {
        IReadOnlyList<LegacyVoucherYearDto> list = storage.VoucherFiles(vahedCode)
            .Select(f => LegacyVoucherImporter.Read(File.ReadAllBytes(f)))
            .Where(m => m != null)
            .Select(m => new LegacyVoucherYearDto(m!.Year, m.Vouchers.Count, m.Vouchers.Sum(v => v.Lines), m.At))
            .OrderBy(x => x.Year).ToList();
        return Task.FromResult(list);
    }

    public Task<LegacyFileDto> BuildVoucherMapAsync(string vahedCode, int year, CancellationToken ct)
    {
        var m = LegacyVoucherImporter.Read(storage.Read(storage.VouchersPath(vahedCode, year)))
            ?? throw new Accounting.Application.Common.Exceptions.BusinessRuleException($"اسناد سال {year} هنوز منتقل نشده است.");
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("اسناد منتقل‌شده");
        ws.RightToLeft = true;
        string[] head = ["شمارهٔ سند جدید", "تاریخ", "شمارهٔ سند قدیم", "اسکیمای قدیم", "کلید قدیم (سال:شناسه)", "تعداد ردیف"];
        for (var c = 0; c < head.Length; c++) ws.Cell(1, c + 1).Value = head[c];
        var row = 2;
        foreach (var v in m.Vouchers)
        {
            ws.Cell(row, 1).SetValue(v.NewNo); ws.Cell(row, 2).SetValue(LegacyText.Date8(v.Date)); ws.Cell(row, 3).SetValue(v.OldNo);
            ws.Cell(row, 4).SetValue(v.OldSchema); ws.Cell(row, 5).SetValue(v.OldKey); ws.Cell(row, 6).Value = v.Lines;
            row++;
        }
        ws.Row(1).Style.Font.Bold = true;
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return Task.FromResult(new LegacyFileDto($"legacy-vouchers-{vahedCode}-{year}.xlsx", Xlsx, ms.ToArray()));
    }

    /// <summary>
    /// معین‌هایی که دادهٔ قدیم واحد (سند، اعلامیه، ویژگی، کارگاه، حساب بانکی) به آن‌ها ارجاع دارد و در کاربرگ مشترکِ
    /// ثبت‌شده کد جدید دارند ولی در درخت جدید نیستند، با همان سازندهٔ کدینگ مشترک و همان تصمیم‌های کاربرگ ساخته می‌شوند.
    /// </summary>
    private async Task EnsureMoeinsAsync(LegacyUnit unit, ChartMapping chart, string userId, ImportReport report, CancellationToken ct)
    {
        var existing = (await db.TB_ACCOUNTCODEs.AsNoTracking().Where(a => a.TYPECODE == Accounting.Domain.ValueObjects.TypeCodes.Moin && a.ISDELETED != true)
            .Select(a => a.ACCCODE).ToListAsync(ct)).Where(c => c != null).Select(c => c!.Trim()).ToHashSet();
        var missing = unit.Years.SelectMany(y => y.UsedMoeins()).Distinct()
            .Where(k => chart.Moeins.TryGetValue(k, out var mm) && mm.Final != "" && !existing.Contains(mm.Final)).ToHashSet();
        if (missing.Count == 0) return;

        var names = string.Join("، ", missing.OrderBy(k => k.Kol).ThenBy(k => k.Moi).Select(k => $"{k.Kol}/{k.Moi}→{chart.Moeins[k].Final}"));
        var builder = new ChartImporter(db, chart.WithRequired(missing), userId, report);
        if (!builder.Validate())
        {
            report.Errors.Add($"معین‌های {names} در دادهٔ قدیم استفاده شده‌اند ولی ردیفشان در کاربرگ کدینگ مشترک کامل نیست؛ " +
                              "ستاد باید کاربرگ مشترک را اصلاح و دوباره ثبت کند.");
            return;
        }
        await builder.RunAsync(ct);
        report.Info.Add($"معین‌های جاافتادهٔ کدینگ مشترک طبق همان کاربرگ ساخته شد: {names}");
    }

    /// <summary>خطای ذخیره با نام جدول(های) درگیر؛ مثلاً «ذخیره در جدول TB_ELAMHEAD ممکن نشد: ORA-…».</summary>
    private static string DbError(DbUpdateException ex)
    {
        var tables = ex.Entries.Select(e => e.Metadata.GetTableName()).Where(t => t != null).Distinct().ToList();
        var msg = (ex.InnerException?.Message ?? ex.Message).Split('\n')[0];
        var sections = tables.Select(Sections.OfTable).Where(s => s != null)
            .Select(s => Sections.Voucher.First(x => x.Key == s).Title).Distinct().ToList();
        var where = sections.Count > 0 ? $"بخش «{string.Join("، ", sections)}» — " : "";
        return tables.Count > 0
            ? $"{where}ذخیره در جدول {string.Join("، ", tables)} ممکن نشد: {msg}"
            : $"ذخیره در پایگاه داده ممکن نشد: {msg}";
    }

    /// <summary>بدون commit همه‌چیز در تراکنش اجرا و برگردانده می‌شود تا خطاهای دیتابیس هم در «بررسی» دیده شوند.</summary>
    private async Task<bool> InTransactionAsync(bool commit, ImportReport report, Func<Task> run, Action onCommitted, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await run();
        if (report.Ok && commit)
        {
            await tx.CommitAsync(ct);
            onCommitted();
            report.Info.Add("✔ ثبت شد.");
            return true;
        }
        await tx.RollbackAsync(ct);
        db.ChangeTracker.Clear();
        if (report.Ok) report.Info.Add("بررسی بدون خطا بود؛ هیچ تغییری ثبت نشد. برای ثبت، «ثبت نهایی» را بزنید.");
        return false;
    }

    /// <summary>خطای قابل انتظار (فایل نادرست، اتصال قدیم) به پیام گزارش تبدیل می‌شود، نه ۵۰۰.</summary>
    private static async Task<LegacyImportReportDto> Guard(Func<ImportReport, Task<bool>> body)
    {
        var report = new ImportReport();
        bool committed;
        try { committed = await body(report); }
        catch (InvalidDataException ex) { report.Errors.Add(ex.Message); committed = false; }
        catch (InvalidOperationException ex) when (ex.Message.Contains("اسکیما")) { report.Errors.Add(ex.Message); committed = false; }
        catch (OracleException ex) when (ex.Number is 1017 or 12154 or 12514 or 12541 or 12170)
        {
            report.Errors.Add($"اتصال به سیستم قدیم ممکن نشد (ORA-{ex.Number:00000})."); committed = false;
        }
        return report.ToDto(committed);
    }
}
