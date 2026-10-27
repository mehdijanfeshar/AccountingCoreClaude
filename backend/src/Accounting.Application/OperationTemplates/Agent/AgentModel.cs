namespace Accounting.Application.OperationTemplates.Agent;

// ═══════════════════════ حسابیار فاز ۶۵ — مدل گفتگو با ابزار (ILlmProvider طرح agent-ux) ═══════════════════════
//
// قرارداد مستقل از سرویس: دستور سیستم + نوبت‌های گفتگو + فهرست ابزارها ⇒ متن یا درخواست صدا زدن ابزار.
// پیاده‌سازی Claude/Ollama/خاموش در Infrastructure. مدل هیچ ابزار نوشتنی ندارد (فقط خواندن و پیش‌نمایش).

public enum AgentTurnRole
{
    User,
    Assistant,

    /// <summary>نتیجهٔ یک ابزار برای مدل.</summary>
    Tool,
}

/// <param name="Id">شناسهٔ صدا زدن (Claude می‌دهد؛ برای Ollama خودمان می‌سازیم).</param>
/// <param name="ArgumentsJson">ورودی ابزار، شیء JSON.</param>
public sealed record AgentToolCall(string Id, string Name, string ArgumentsJson);

/// <param name="Text">متن کاربر/دستیار، یا نتیجهٔ ابزار (JSON) در نوبت <see cref="AgentTurnRole.Tool"/>.</param>
/// <param name="ToolCalls">فقط نوبت دستیار: ابزارهایی که مدل خواست.</param>
/// <param name="ToolCallId">فقط نوبت ابزار: پاسخ کدام صدا زدن است.</param>
public sealed record AgentTurn(
    AgentTurnRole Role,
    string? Text,
    IReadOnlyList<AgentToolCall>? ToolCalls = null,
    string? ToolCallId = null,
    string? ToolName = null);

/// <param name="InputSchemaJson">شِمای JSON ورودی ابزار.</param>
public sealed record AgentToolSpec(string Name, string Description, string InputSchemaJson);

public sealed record AgentModelRequest(string System, IReadOnlyList<AgentTurn> Turns, IReadOnlyList<AgentToolSpec> Tools);

/// <param name="Text">متن پاسخ (ممکن است کنار صدا زدن ابزار هم بیاید).</param>
/// <param name="ToolCalls">خالی = پاسخ نهایی این نوبت.</param>
public sealed record AgentModelReply(string? Text, IReadOnlyList<AgentToolCall> ToolCalls);

/// <summary>مدل گفتگو با ابزار (Claude، Ollama، یا خاموش) — پیاده‌سازی در Infrastructure.</summary>
public interface IAgentChatModel
{
    bool IsEnabled { get; }
    string ProviderName { get; }
    string? ModelName { get; }

    /// <summary>true = متن‌ها پیش از ارسال پوشانده می‌شوند (G5). Claude همیشه true.</summary>
    bool RequiresPiiMasking { get; }

    /// <summary>
    /// true = سرویس بیرون از سازمان است (Claude). سیاست داده (۲۰۲۶-۱۰-۰۶): نام طرف حساب و مانده به سرویس
    /// بیرونی نمی‌رود مگر با <see cref="AssistantAgentOptions.AllowDataToolsForExternal"/>.
    /// </summary>
    bool IsExternal { get; }

    Task<AgentModelReply> CompleteAsync(AgentModelRequest request, CancellationToken ct);
}

/// <summary>
/// تنظیمات دستیار گفتگو — بخش <c>Assistant:Agent</c> در appsettings (مدل و کلید همان <c>Assistant:Llm</c>).
/// <code>
/// "Assistant": { "Agent": { "MaxToolRounds": 6, "HistoryMessages": 12, "AllowDataToolsForExternal": false } }
/// </code>
/// </summary>
public sealed class AssistantAgentOptions
{
    public const string Section = "Assistant:Agent";

    /// <summary>حداکثر دور «صدا زدن ابزار ⇐ پاسخ» در یک پیام کاربر.</summary>
    public int MaxToolRounds { get; set; } = 6;

    /// <summary>چند پیام قبلی (کاربر/دستیار) به مدل فرستاده شود.</summary>
    public int HistoryMessages { get; set; } = 12;

    /// <summary>
    /// ابزارهای دادهٔ حساب (جستجوی طرف حساب با نام، مانده) برای سرویس بیرونی هم فعال باشد. پیش‌فرض خاموش —
    /// تصمیم صاحب پروژه لازم است (ریسک #۳۷). با خاموش بودن، نام طرف حساب را خود سیستم پیدا می‌کند.
    /// </summary>
    public bool AllowDataToolsForExternal { get; set; }

    /// <summary>
    /// گفتگو با حسابیار روشن باشد. با مدل کوچک داخلی (مثل qwen2.5:7b روی CPU) که فارسی و ابزار را خوب نمی‌فهمد
    /// خاموش کنید؛ «فهم جمله» در صفحهٔ ثبت سند جداست و روشن می‌ماند.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// JSON دستیار و سنجش: نویسه‌های فارسی بدون <c>\uXXXX</c>، تا پوشاندن PII (ارقام فارسی) روی متن واقعی کار کند،
/// مدل توکن کمتری بگیرد و ردپای ذخیره‌شده خوانا باشد.
/// </summary>
public static class AgentJson
{
    public static readonly System.Text.Json.JsonSerializerOptions Options =
        new(System.Text.Json.JsonSerializerDefaults.Web) { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
}
