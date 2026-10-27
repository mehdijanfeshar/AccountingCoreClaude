using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Accounting.Application.OperationTemplates.Agent;
using Microsoft.Extensions.Options;

namespace Accounting.Infrastructure.OperationTemplates.Agent;

// حسابیار فاز ۶۵ — مدل گفتگو با ابزار. همان تنظیمات Assistant:Llm (سرویس، کلید، مدل، آدرس، پراکسی).

/// <summary>هوش مصنوعی خاموش — صفحهٔ گفتگو پیام «خاموش است» نشان می‌دهد.</summary>
public sealed class DisabledAgentChatModel : IAgentChatModel
{
    public bool IsEnabled => false;
    public string ProviderName => "None";
    public string? ModelName => null;
    public bool RequiresPiiMasking => false;
    public bool IsExternal => false;
    public Task<AgentModelReply> CompleteAsync(AgentModelRequest request, CancellationToken ct) =>
        throw new InvalidOperationException("هوش مصنوعی حسابیار خاموش است.");
}

/// <summary>Claude (Anthropic Messages API) با ابزار: tool_use در پاسخ، tool_result در پیام بعدی کاربر.</summary>
public sealed class ClaudeAgentChatModel : IAgentChatModel
{
    private readonly IHttpClientFactory _http;
    private readonly AssistantLlmOptions _options;

    public ClaudeAgentChatModel(IHttpClientFactory http, IOptions<AssistantLlmOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public bool IsEnabled => true;
    public string ProviderName => "Claude";
    public string? ModelName => string.IsNullOrWhiteSpace(_options.Model) ? "claude-sonnet-5-5" : _options.Model;
    public bool RequiresPiiMasking => true; // سرویس بیرونی: پوشاندن اجباری
    public bool IsExternal => true;

    public async Task<AgentModelReply> CompleteAsync(AgentModelRequest request, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"] = ModelName,
            ["max_tokens"] = 1500,
            ["system"] = request.System,
            ["messages"] = BuildMessages(request.Turns),
            ["tools"] = new JsonArray(request.Tools.Select(t => (JsonNode)new JsonObject
            {
                ["name"] = t.Name,
                ["description"] = t.Description,
                ["input_schema"] = JsonNode.Parse(t.InputSchemaJson),
            }).ToArray()),
        };

        var baseUrl = (string.IsNullOrWhiteSpace(_options.BaseUrl) ? "https://api.anthropic.com" : _options.BaseUrl).TrimEnd('/');
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        message.Headers.Add("x-api-key", _options.ApiKey);
        message.Headers.Add("anthropic-version", "2023-06-01");
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.CreateClient(AssistantLlmDi.HttpClientName).SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return ParseReply(doc.RootElement);
    }

    /// <summary>نوبت‌ها ⇒ پیام‌های Claude. نتیجه‌های پشت سر هم ابزار در یک پیام «user» جمع می‌شوند.</summary>
    public static JsonArray BuildMessages(IReadOnlyList<AgentTurn> turns)
    {
        var messages = new JsonArray();
        JsonArray? pendingResults = null;
        foreach (var turn in turns)
        {
            if (turn.Role == AgentTurnRole.Tool)
            {
                pendingResults ??= [];
                pendingResults.Add(new JsonObject
                {
                    ["type"] = "tool_result",
                    ["tool_use_id"] = turn.ToolCallId,
                    ["content"] = turn.Text ?? "",
                });
                continue;
            }
            if (pendingResults is not null)
            {
                messages.Add(new JsonObject { ["role"] = "user", ["content"] = pendingResults });
                pendingResults = null;
            }
            if (turn.Role == AgentTurnRole.User)
            {
                messages.Add(new JsonObject { ["role"] = "user", ["content"] = turn.Text ?? "" });
                continue;
            }
            var content = new JsonArray();
            if (!string.IsNullOrWhiteSpace(turn.Text))
                content.Add(new JsonObject { ["type"] = "text", ["text"] = turn.Text });
            foreach (var call in turn.ToolCalls ?? [])
                content.Add(new JsonObject
                {
                    ["type"] = "tool_use",
                    ["id"] = call.Id,
                    ["name"] = call.Name,
                    ["input"] = JsonNode.Parse(string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson),
                });
            if (content.Count == 0) content.Add(new JsonObject { ["type"] = "text", ["text"] = "…" });
            messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = content });
        }
        if (pendingResults is not null)
            messages.Add(new JsonObject { ["role"] = "user", ["content"] = pendingResults });
        return messages;
    }

    public static AgentModelReply ParseReply(JsonElement root)
    {
        var text = new StringBuilder();
        var calls = new List<AgentToolCall>();
        foreach (var block in root.GetProperty("content").EnumerateArray())
        {
            var type = block.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (type == "text" && block.TryGetProperty("text", out var tx))
                text.Append(tx.GetString());
            else if (type == "tool_use")
                calls.Add(new AgentToolCall(block.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N"),
                    block.GetProperty("name").GetString() ?? "", block.TryGetProperty("input", out var input) ? input.GetRawText() : "{}"));
        }
        return new AgentModelReply(text.Length == 0 ? null : text.ToString(), calls);
    }
}

/// <summary>
/// مدل داخلی سازمان با Ollama (<c>/api/chat</c> با <c>tools</c>). صدا زدن ابزار در <c>message.tool_calls</c>؛
/// Ollama شناسه نمی‌دهد، پس شناسه را خودمان می‌سازیم. دما صفر.
/// </summary>
public sealed class OllamaAgentChatModel : IAgentChatModel
{
    private readonly IHttpClientFactory _http;
    private readonly AssistantLlmOptions _options;

    public OllamaAgentChatModel(IHttpClientFactory http, IOptions<AssistantLlmOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public bool IsEnabled => true;
    public string ProviderName => "Ollama";
    public string? ModelName => string.IsNullOrWhiteSpace(_options.Model) ? "qwen2.5:7b" : _options.Model;
    public bool RequiresPiiMasking => _options.MaskPii;
    public bool IsExternal => false;

    public async Task<AgentModelReply> CompleteAsync(AgentModelRequest request, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"] = ModelName,
            ["stream"] = false,
            ["keep_alive"] = string.IsNullOrWhiteSpace(_options.KeepAlive) ? "30m" : _options.KeepAlive,
            ["options"] = new JsonObject { ["temperature"] = 0 },
            ["messages"] = BuildMessages(request.System, request.Turns),
            ["tools"] = new JsonArray(request.Tools.Select(t => (JsonNode)new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description,
                    ["parameters"] = JsonNode.Parse(t.InputSchemaJson),
                },
            }).ToArray()),
        };

        if (_options.Think is { } think)
            body["think"] = think;

        var baseUrl = (string.IsNullOrWhiteSpace(_options.BaseUrl) ? "http://localhost:11434" : _options.BaseUrl).TrimEnd('/');
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await _http.CreateClient(AssistantLlmDi.HttpClientName).PostAsync($"{baseUrl}/api/chat", content, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return ParseReply(doc.RootElement);
    }

    public static JsonArray BuildMessages(string system, IReadOnlyList<AgentTurn> turns)
    {
        var messages = new JsonArray(new JsonObject { ["role"] = "system", ["content"] = system });
        foreach (var turn in turns)
        {
            switch (turn.Role)
            {
                case AgentTurnRole.User:
                    messages.Add(new JsonObject { ["role"] = "user", ["content"] = turn.Text ?? "" });
                    break;
                case AgentTurnRole.Tool:
                    messages.Add(new JsonObject { ["role"] = "tool", ["content"] = turn.Text ?? "", ["tool_name"] = turn.ToolName });
                    break;
                default:
                    var m = new JsonObject { ["role"] = "assistant", ["content"] = turn.Text ?? "" };
                    if (turn.ToolCalls is { Count: > 0 } calls)
                        m["tool_calls"] = new JsonArray(calls.Select(c => (JsonNode)new JsonObject
                        {
                            ["function"] = new JsonObject
                            {
                                ["name"] = c.Name,
                                ["arguments"] = JsonNode.Parse(string.IsNullOrWhiteSpace(c.ArgumentsJson) ? "{}" : c.ArgumentsJson),
                            },
                        }).ToArray());
                    messages.Add(m);
                    break;
            }
        }
        return messages;
    }

    public static AgentModelReply ParseReply(JsonElement root)
    {
        var message = root.GetProperty("message");
        var text = message.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
        var calls = new List<AgentToolCall>();
        if (message.TryGetProperty("tool_calls", out var tc) && tc.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in tc.EnumerateArray())
            {
                if (!call.TryGetProperty("function", out var fn)) continue;
                var name = fn.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;
                var args = fn.TryGetProperty("arguments", out var a)
                    ? a.ValueKind == JsonValueKind.String ? a.GetString() ?? "{}" : a.GetRawText()
                    : "{}";
                calls.Add(new AgentToolCall("call_" + Guid.NewGuid().ToString("N")[..12], name, args));
            }
        }
        return new AgentModelReply(string.IsNullOrWhiteSpace(text) ? null : text, calls);
    }
}
