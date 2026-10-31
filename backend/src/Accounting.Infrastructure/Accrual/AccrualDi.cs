using Accounting.Application.Accrual;
using Accounting.Application.Commands.Vouchers.ReverseVoucher;
using Accounting.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Accrual;

/// <summary>بخش <c>Accrual:Job</c>. پیش‌فرض خاموش: تا DDL 090 اجرا نشده، کار روزانه نباید هر بار ORA-00942 بدهد.</summary>
public sealed class AccrualJobOptions
{
    public const string Section = "Accrual:Job";

    public bool Enabled { get; set; }

    /// <summary>فاصلهٔ بررسی سطرهای سررسیدشده (دقیقه). کوتاه‌تر از روز تا ری‌استارت app pool روی IIS روزی را جا نیندازد.</summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>حداکثر سطر در هر دور؛ هر سطر تراکنش جدای خودش را دارد.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>ثبت‌کنندهٔ سندهای کار روزانه (ADDUSERID، حداکثر ۱۰ نویسه) — تصمیم صاحب پروژه: کاربر سیستمی ثابت.</summary>
    public string SystemUserId { get; set; } = "SYSTEM";
}

public static class AccrualDi
{
    /// <summary>حسابداری تعهدی (DDL 090). به AddOperationTemplates (IVoucherWriter، موتور الگو) وابسته است.</summary>
    public static IServiceCollection AddAccrual(this IServiceCollection s, IConfiguration configuration)
    {
        s.AddScoped<IAccrualScheduleRepository, AccrualScheduleRepository>();
        s.AddScoped<AccrualSchedulePlanner>();
        s.AddScoped<AccrualScheduleRunner>();
        s.AddScoped<VoucherReverser>();
        s.AddScoped<SystemActorContext>();

        var options = configuration.GetSection(AccrualJobOptions.Section).Get<AccrualJobOptions>() ?? new AccrualJobOptions();
        if (options.SystemUserId.Length is 0 or > 10)
            throw new InvalidOperationException("Accrual:Job:SystemUserId باید ۱ تا ۱۰ نویسه باشد (ستون ADDUSERID).");
        s.AddSingleton(options);
        if (options.Enabled) s.AddHostedService<AccrualScheduleWorker>();
        return s;
    }
}

/// <summary>
/// کار روزانه: سطرهای باز با سررسید تا امروز را برمی‌دارد و هر کدام را در scope و تراکنش جدا با
/// <see cref="AccrualScheduleRunner"/> و <c>SKIP LOCKED</c> اجرا می‌کند. خطای یک سطر بقیه را متوقف نمی‌کند (فقط لاگ)؛
/// سطر باز می‌ماند تا دور بعد یا اجرای دستی.
/// </summary>
public sealed class AccrualScheduleWorker : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopes;
    private readonly AccrualJobOptions _options;
    private readonly ILogger<AccrualScheduleWorker> _logger;

    public AccrualScheduleWorker(IServiceScopeFactory scopes, AccrualJobOptions options, ILogger<AccrualScheduleWorker> logger)
    { _scopes = scopes; _options = options; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, stoppingToken);
            var period = TimeSpan.FromMinutes(Math.Clamp(_options.IntervalMinutes, 1, 24 * 60));
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);
                await Task.Delay(period, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public async Task<(int Ran, int Failed)> RunOnceAsync(CancellationToken ct)
    {
        IReadOnlyList<Guid> ids;
        try
        {
            using var scope = _scopes.CreateScope();
            ids = await scope.ServiceProvider.GetRequiredService<IAccrualScheduleRepository>()
                .ListDueIdsAsync(AccrualDates.Today(), Math.Clamp(_options.BatchSize, 1, 500), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Accrual job: could not list due schedule lines");
            return (0, 0);
        }

        int ran = 0, failed = 0;
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // scope تازه برای هر سطر: DbContext سطر شکست‌خورده به سطر بعد نمی‌رسد.
                using var scope = _scopes.CreateScope();
                scope.ServiceProvider.GetRequiredService<SystemActorContext>().UserId = _options.SystemUserId;
                var r = await scope.ServiceProvider.GetRequiredService<AccrualScheduleRunner>().RunAsync(id, vahedCode: null, skipLocked: true, ct);
                if (r.Outcome == AccrualRunOutcome.Ran) ran++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _logger.LogWarning(ex, "Accrual job: schedule line {Id} failed; it stays open", id);
            }
        }

        if (ids.Count > 0)
            _logger.LogInformation("Accrual job: {Ran} vouchers created, {Failed} failed, {Total} due lines checked", ran, failed, ids.Count);
        return (ran, failed);
    }
}
