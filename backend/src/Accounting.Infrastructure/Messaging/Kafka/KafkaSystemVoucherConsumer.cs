using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Vouchers.Inbox;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accounting.Infrastructure.Messaging.Kafka;

/// <summary>بخش <c>Kafka</c> تنظیمات — همان کلیدهای پروژهٔ قدیم. بدون <c>BootstrapServers</c> سرویس ثبت نمی‌شود.</summary>
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; } = true;
    public string? BootstrapServers { get; set; }
    public string Topic { get; set; } = "TmpVouchersTopic";
    public string? DLQTopic { get; set; }
    public string? ProcessedTopic { get; set; }
    public string GroupId { get; set; } = "FinancialServiceGroup";
    /// <summary>فعلاً استفاده نمی‌شود (مصرف‌کنندهٔ DLQ نداریم؛ DLQ دستی بررسی می‌شود).</summary>
    public string? DLQGroupId { get; set; }
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
    /// <summary>نادیده گرفته می‌شود: commit همیشه دستی و پس از ذخیره/ارسال به DLQ است.</summary>
    public bool EnableAutoCommit { get; set; }
    /// <summary>مکث پیش از تلاش دوباره وقتی خطای زیرساختی (مثلاً قطع دیتابیس) رخ داده است.</summary>
    public int RetryDelaySeconds { get; set; } = 15;

    public string DlqTopicName => string.IsNullOrWhiteSpace(DLQTopic) ? $"{Topic}-DLQ" : DLQTopic;
    public string ProcessedTopicName => string.IsNullOrWhiteSpace(ProcessedTopic) ? $"{Topic}-Processed" : ProcessedTopic;
    public bool IsActive => Enabled && !string.IsNullOrWhiteSpace(BootstrapServers);
}

/// <summary>
/// اسناد سایر سیستم‌ها را از تاپیک کافکا می‌خواند و در صندوق «دریافت از سایر سیستم‌ها» می‌گذارد
/// (<see cref="ISystemVoucherIngestService"/>). نتیجه: ثبت یا تکراری → تاپیک Processed؛ پیام خراب یا
/// ردشده → DLQ. در هر دو حالت offset commit می‌شود. خطای زیرساختی (دیتابیس/شبکه) commit نمی‌شود و همان
/// پیام پس از مکث دوباره خوانده می‌شود تا هیچ سندی گم نشود (برخلاف پروژهٔ قدیم که آن را هم به DLQ می‌فرستاد).
/// </summary>
public sealed class KafkaSystemVoucherConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new LenientStringConverter() },
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaSystemVoucherConsumer> _logger;

    public KafkaSystemVoucherConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<KafkaSystemVoucherConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    // Consume() مسدودکننده است؛ روی نخ جدا تا راه‌اندازی برنامه معطل نماند.
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Factory.StartNew(() => RunAsync(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = _options.AutoOffsetReset,
            EnableAutoCommit = false,
        };
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig)
            .SetErrorHandler((_, e) => _logger.LogWarning("Kafka consumer error: {Reason}", e.Reason))
            .Build();
        using var producer = new ProducerBuilder<Null, string>(producerConfig)
            .SetErrorHandler((_, e) => _logger.LogWarning("Kafka producer error: {Reason}", e.Reason))
            .Build();

        consumer.Subscribe(_options.Topic);
        _logger.LogInformation("Kafka consumer started. Topic={Topic}, DLQ={Dlq}, Processed={Processed}",
            _options.Topic, _options.DlqTopicName, _options.ProcessedTopicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, string>? cr;
                try
                {
                    cr = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    // مثلاً تاپیک هنوز ساخته نشده — بدون مکث لاگ پر می‌شود.
                    _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                if (cr?.Message is null)
                    continue;

                try
                {
                    await HandleAsync(producer, cr, stoppingToken);
                    consumer.Commit(cr);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // خطای زیرساختی: commit نکن، به همین offset برگرد و بعد از مکث دوباره تلاش کن.
                    _logger.LogError(ex, "Transient error on {Topic}[{Partition}]@{Offset}; retrying in {Delay}s.",
                        cr.Topic, cr.Partition.Value, cr.Offset.Value, _options.RetryDelaySeconds);
                    consumer.Seek(cr.TopicPartitionOffset);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.RetryDelaySeconds)), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            consumer.Close();
            producer.Flush(TimeSpan.FromSeconds(5));
        }
    }

    private async Task HandleAsync(IProducer<Null, string> producer, ConsumeResult<Ignore, string> cr, CancellationToken ct)
    {
        var value = cr.Message.Value;

        SystemVoucherMessage? message;
        try
        {
            message = string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Deserialize<SystemVoucherMessage>(value, JsonOptions);
        }
        catch (JsonException ex)
        {
            await ToDlqAsync(producer, cr, [$"پیام JSON معتبر نیست: {ex.Message}"], ex, ct);
            return;
        }

        if (message is null)
        {
            await ToDlqAsync(producer, cr, ["پیام خالی است."], null, ct);
            return;
        }

        SystemVoucherIngestResult result;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ingest = scope.ServiceProvider.GetRequiredService<ISystemVoucherIngestService>();
            result = await ingest.IngestAsync(message, ct);
        }
        catch (Exception ex) when (ex is IPublicProblemException or DuplicateKeyException or ForeignKeyViolationException)
        {
            // خطای ناشی از خود داده (نه زیرساخت) — تکرار فایده ندارد.
            await ToDlqAsync(producer, cr, [ex.Message], ex, ct);
            return;
        }

        if (result.Status == SystemVoucherIngestStatus.Rejected)
        {
            await ToDlqAsync(producer, cr, result.Errors, null, ct);
            return;
        }

        var status = result.Status == SystemVoucherIngestStatus.Duplicate ? "duplicate" : "success";
        var payload = JsonSerializer.Serialize(new
        {
            Status = status,
            VoucherId = result.TmpVoucherId,
            message.SourceId,
            message.VahedCode,
            OriginalMessage = value,
            Timestamp = DateTime.UtcNow,
        });
        await producer.ProduceAsync(_options.ProcessedTopicName,
            new Message<Null, string> { Value = payload, Headers = BuildHeaders(cr, status) }, ct);

        _logger.LogInformation("System voucher {Status} (TmpVoucherId={Id}, SourceId={SourceId}, Vahed={Vahed}).",
            status, result.TmpVoucherId, message.SourceId, message.VahedCode);
    }

    private async Task ToDlqAsync(IProducer<Null, string> producer, ConsumeResult<Ignore, string> cr,
        IReadOnlyList<string> errors, Exception? ex, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            Error = string.Join(" | ", errors),
            Errors = errors,
            OriginalMessage = cr.Message.Value,
            cr.Topic,
            Partition = cr.Partition.Value,
            Offset = cr.Offset.Value,
            Timestamp = DateTime.UtcNow,
        });
        await producer.ProduceAsync(_options.DlqTopicName,
            new Message<Null, string> { Value = payload, Headers = BuildHeaders(cr, "error", ex) }, ct);

        _logger.LogWarning("System voucher sent to DLQ {Dlq} (offset={Offset}): {Errors}",
            _options.DlqTopicName, cr.Offset.Value, string.Join(" | ", errors));
    }

    private static Headers BuildHeaders(ConsumeResult<Ignore, string> cr, string status, Exception? ex = null)
    {
        var h = new Headers
        {
            new Header("status", Encoding.UTF8.GetBytes(status)),
            new Header("original-topic", Encoding.UTF8.GetBytes(cr.Topic)),
            new Header("original-partition", BitConverter.GetBytes(cr.Partition.Value)),
            new Header("original-offset", BitConverter.GetBytes(cr.Offset.Value)),
            new Header("original-ts-ms", BitConverter.GetBytes(cr.Message.Timestamp.UnixTimestampMs)),
        };
        if (ex != null)
            h.Add("error-type", Encoding.UTF8.GetBytes(ex.GetType().Name));
        return h;
    }

    /// <summary>سیستم‌های مبدأ کدها/سال را گاهی عدد می‌فرستند؛ هر دو را به رشته بپذیر.</summary>
    private sealed class LenientStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan),
                JsonTokenType.True => "1",
                JsonTokenType.False => "0",
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token {reader.TokenType} for a string field."),
            };

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
    }
}
