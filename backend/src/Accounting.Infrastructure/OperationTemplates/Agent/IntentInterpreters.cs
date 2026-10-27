using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Accounting.Application.OperationTemplates.Agent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Accounting.Infrastructure.OperationTemplates.Agent;

/// <summary>
/// تنظیمات هوش مصنوعی حسابیار — بخش <c>Assistant:Llm</c> در appsettings. پیش‌فرض خاموش است و حسابیار با
/// روش قاعده‌ای کار می‌کند. کلید API فقط در تنظیمات سرور (یا متغیر محیطی <c>Assistant__Llm__ApiKey</c>)،
/// هرگز در کد یا مخزن.
/// <code>
/// "Assistant": { "Llm": {
///   "Provider": "Claude",               // None | Claude | Ollama
///   "ApiKey": "...",                    // فقط Claude
///   "Model": "claude-sonnet-5-5",       // Ollama مثلاً "qwen2.5:7b"
///   "BaseUrl": null,                    // Claude: https://api.anthropic.com · Ollama: http://localhost:11434
///   "ProxyUrl": null,                   // اگر خروج به اینترنت از پراکسی است
///   "TimeoutSeconds": 20,
///   "MaskPii": false,              // فقط Ollama؛ Claude همیشه پوشانده می‌شود
///   "TrustAnswers": true,           // false = فقط انتخاب الگو از مدل
///   "KeepAlive": "30m" } }          // فقط Ollama
/// </code>
/// </summary>
public sealed class AssistantLlmOptions
{
    public const string Section = "Assistant:Llm";

    public string Provider { get; set; } = "None";
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public string? BaseUrl { get; set; }
    public string? ProxyUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// پوشاندن دادهٔ شخصی پیش از ارسال (G5). برای Claude همیشه روشن است (این گزینه نادیده گرفته می‌شود)؛
    /// برای Ollama داخلی پیش‌فرض خاموش.
    /// </summary>
    public bool MaskPii { get; set; }

    /// <summary>
    /// false = از «فهم جمله» فقط الگو پذیرفته می‌شود؛ مبلغ/تاریخ/طرف حساب را روش قاعده‌ای برمی‌دارد (مدل کوچک داخلی).
    /// </summary>
    public bool TrustAnswers { get; set; } = true;

    /// <summary>false = صفحهٔ ثبت سند بدون هوش مصنوعی (روش قاعده‌ای)؛ گفتگو و سنجش همین مدل را به کار می‌برند.</summary>
    public bool InterpretEnabled { get; set; } = true;

    /// <summary>فقط Ollama: مدت ماندن مدل در حافظه پس از آخرین درخواست (مثل «30m»)؛ بارگذاری دوباره روی CPU ده‌ها ثانیه است.</summary>
    public string KeepAlive { get; set; } = "30m";

    /// <summary>
    /// فقط Ollama: «فکر کردن» مدل‌های استدلالی (مثل qwen3). false = بی‌فکر و چند برابر سریع‌تر روی CPU؛
    /// خالی = اصلاً فرستاده نمی‌شود (مدل‌هایی مثل qwen2.5 که این قابلیت را ندارند).
    /// </summary>
    public bool? Think { get; set; }
}

public static class AssistantLlmDi
{
    public const string HttpClientName = "assistant-llm";

    public static IServiceCollection AddAssistantLlm(this IServiceCollection s, IConfiguration configuration)
    {
        s.Configure<AssistantLlmOptions>(configuration.GetSection(AssistantLlmOptions.Section));
        var options = configuration.GetSection(AssistantLlmOptions.Section).Get<AssistantLlmOptions>() ?? new AssistantLlmOptions();

        s.AddHttpClient(HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120)))
            .ConfigurePrimaryHttpMessageHandler(() => string.IsNullOrWhiteSpace(options.ProxyUrl)
                ? new HttpClientHandler()
                : new HttpClientHandler { Proxy = new WebProxy(options.ProxyUrl), UseProxy = true });

        switch (options.Provider.Trim().ToLowerInvariant())
        {
            case "claude" when !string.IsNullOrWhiteSpace(options.ApiKey):
                s.AddScoped<IIntentInterpreter, ClaudeIntentInterpreter>();
                s.AddScoped<IAgentChatModel, ClaudeAgentChatModel>();
                break;
            case "ollama":
                s.AddScoped<IIntentInterpreter, OllamaIntentInterpreter>();
                s.AddScoped<IAgentChatModel, OllamaAgentChatModel>();
                break;
            default:
                s.AddSingleton<IIntentInterpreter, DisabledIntentInterpreter>();
                s.AddSingleton<IAgentChatModel, DisabledAgentChatModel>();
                break;
        }

        // فاز ۶۵ — دستیار گفتگو: تنظیمات Assistant:Agent
        s.AddSingleton(configuration.GetSection(AssistantAgentOptions.Section).Get<AssistantAgentOptions>() ?? new AssistantAgentOptions());
        return s;
    }
}

/// <summary>هوش مصنوعی خاموش (پیش‌فرض یا Claude بدون کلید) — فرانت با روش قاعده‌ای ادامه می‌دهد.</summary>
public sealed class DisabledIntentInterpreter : IIntentInterpreter
{
    public bool IsEnabled => false;
    public string ProviderName => "None";
    public bool RequiresPiiMasking => false;
    public Task<string> CompleteJsonAsync(IntentPrompt prompt, CancellationToken ct) =>
        throw new InvalidOperationException("هوش مصنوعی حسابیار خاموش است.");
}

/// <summary>
/// Claude (Anthropic Messages API). خروجی ساخت‌یافته با «ابزار اجباری»: مدل باید ابزار <c>fill_operation</c>
/// را با ورودی مطابق شِما صدا بزند، پس همیشه JSON معتبر برمی‌گردد.
/// </summary>
public sealed class ClaudeIntentInterpreter : IIntentInterpreter
{
    private const string ToolName = "fill_operation";
    private readonly IHttpClientFactory _http;
    private readonly AssistantLlmOptions _options;

    public ClaudeIntentInterpreter(IHttpClientFactory http, IOptions<AssistantLlmOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public bool IsEnabled => true;
    public string ProviderName => "Claude";
    public string? ModelName => string.IsNullOrWhiteSpace(_options.Model) ? "claude-sonnet-5-5" : _options.Model;
    public bool RequiresPiiMasking => true; // ارسال به سرویس بیرونی: پوشاندن اجباری، قابل خاموش شدن نیست
    public bool TrustAnswers => _options.TrustAnswers;
    public bool InterpretEnabled => _options.InterpretEnabled;

    public async Task<string> CompleteJsonAsync(IntentPrompt prompt, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"] = string.IsNullOrWhiteSpace(_options.Model) ? "claude-sonnet-5-5" : _options.Model,
            ["max_tokens"] = 1024,
            ["system"] = prompt.System,
            ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = prompt.User }),
            ["tools"] = new JsonArray(new JsonObject
            {
                ["name"] = ToolName,
                ["description"] = "Return the chosen operation template and the answers extracted from the sentence.",
                ["input_schema"] = JsonNode.Parse(prompt.JsonSchema),
            }),
            ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = ToolName },
        };

        var baseUrl = (string.IsNullOrWhiteSpace(_options.BaseUrl) ? "https://api.anthropic.com" : _options.BaseUrl).TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.CreateClient(AssistantLlmDi.HttpClientName).SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type) && type.GetString() == "tool_use" && block.TryGetProperty("input", out var input))
                return input.GetRawText();
        }
        throw new InvalidOperationException("Claude ابزار خروجی را صدا نزد.");
    }
}

/// <summary>
/// مدل داخلی روی سرور سازمان با Ollama (<c>/api/chat</c>). خروجی با <c>format</c> = شِمای JSON محدود می‌شود؛
/// دما صفر برای پاسخ پایدار.
/// </summary>
public sealed class OllamaIntentInterpreter : IIntentInterpreter
{
    private readonly IHttpClientFactory _http;
    private readonly AssistantLlmOptions _options;

    public OllamaIntentInterpreter(IHttpClientFactory http, IOptions<AssistantLlmOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public bool IsEnabled => true;
    public string ProviderName => "Ollama";
    public string? ModelName => string.IsNullOrWhiteSpace(_options.Model) ? "qwen2.5:7b" : _options.Model;
    public bool RequiresPiiMasking => _options.MaskPii;
    public bool TrustAnswers => _options.TrustAnswers;
    public bool InterpretEnabled => _options.InterpretEnabled;

    public async Task<string> CompleteJsonAsync(IntentPrompt prompt, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"] = string.IsNullOrWhiteSpace(_options.Model) ? "qwen2.5:7b" : _options.Model,
            ["stream"] = false,
            ["keep_alive"] = string.IsNullOrWhiteSpace(_options.KeepAlive) ? "30m" : _options.KeepAlive,
            ["format"] = JsonNode.Parse(prompt.JsonSchema),
            ["options"] = new JsonObject { ["temperature"] = 0 },
            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = prompt.System },
                new JsonObject { ["role"] = "user", ["content"] = prompt.User }),
        };

        if (_options.Think is { } think)
            body["think"] = think;

        var baseUrl = (string.IsNullOrWhiteSpace(_options.BaseUrl) ? "http://localhost:11434" : _options.BaseUrl).TrimEnd('/');
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await _http.CreateClient(AssistantLlmDi.HttpClientName).PostAsync($"{baseUrl}/api/chat", content, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString()
            ?? throw new InvalidOperationException("پاسخ Ollama خالی بود.");
    }
}
