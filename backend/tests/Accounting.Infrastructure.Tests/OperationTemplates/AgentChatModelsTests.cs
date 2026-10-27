using System.Text.Json;
using Accounting.Application.OperationTemplates.Agent;
using Accounting.Infrastructure.OperationTemplates.Agent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure.Tests.OperationTemplates;

public sealed class AgentChatModelsTests
{
    private static readonly IReadOnlyList<AgentTurn> Turns =
    [
        new(AgentTurnRole.User, "صد ریال گرفتم"),
        new(AgentTurnRole.Assistant, "بررسی می‌کنم", [new AgentToolCall("t1", "preview_voucher", """{"template_code":"A"}"""), new AgentToolCall("t2", "search_counterparty", "")]),
        new(AgentTurnRole.Tool, """{"success":true}""", ToolCallId: "t1", ToolName: "preview_voucher"),
        new(AgentTurnRole.Tool, """{"items":[]}""", ToolCallId: "t2", ToolName: "search_counterparty"),
        new(AgentTurnRole.Assistant, "آماده است"),
        new(AgentTurnRole.User, "ممنون"),
    ];

    [Fact]
    public void Claude_GroupsConsecutiveToolResultsIntoOneUserMessage()
    {
        var m = ClaudeAgentChatModel.BuildMessages(Turns);

        Assert.Equal(["user", "assistant", "user", "assistant", "user"], m.Select(x => x!["role"]!.GetValue<string>()));
        var calls = m[1]!["content"]!.AsArray();
        Assert.Equal("text", calls[0]!["type"]!.GetValue<string>());
        Assert.Equal("tool_use", calls[1]!["type"]!.GetValue<string>());
        Assert.Equal("A", calls[1]!["input"]!["template_code"]!.GetValue<string>());
        Assert.Empty(calls[2]!["input"]!.AsObject()); // ورودی خالی ⇒ {}
        var results = m[2]!["content"]!.AsArray();
        Assert.Equal(2, results.Count);
        Assert.Equal("t2", results[1]!["tool_use_id"]!.GetValue<string>());
    }

    [Fact]
    public void Claude_ParsesTextAndToolUse()
    {
        using var doc = JsonDocument.Parse("""
            {"content":[{"type":"text","text":"باشه"},{"type":"tool_use","id":"tu_1","name":"get_account_balance","input":{"query":"صندوق"}}]}
            """);
        var r = ClaudeAgentChatModel.ParseReply(doc.RootElement);
        Assert.Equal("باشه", r.Text);
        var call = r.ToolCalls.Single();
        Assert.Equal("tu_1", call.Id);
        Assert.Equal("get_account_balance", call.Name);
        Assert.Contains("صندوق", JsonDocument.Parse(call.ArgumentsJson).RootElement.GetProperty("query").GetString());
    }

    [Fact]
    public void Ollama_BuildsSystemAndToolMessages()
    {
        var m = OllamaAgentChatModel.BuildMessages("SYS", Turns);

        Assert.Equal(["system", "user", "assistant", "tool", "tool", "assistant", "user"], m.Select(x => x!["role"]!.GetValue<string>()));
        Assert.Equal("SYS", m[0]!["content"]!.GetValue<string>());
        Assert.Equal("preview_voucher", m[2]!["tool_calls"]![0]!["function"]!["name"]!.GetValue<string>());
        Assert.Equal("search_counterparty", m[4]!["tool_name"]!.GetValue<string>());
        Assert.Null(m[5]!["tool_calls"]);
    }

    [Theory]
    [InlineData("""{"message":{"content":"","tool_calls":[{"function":{"name":"preview_voucher","arguments":{"template_code":"A"}}}]}}""")]
    [InlineData("""{"message":{"content":"","tool_calls":[{"function":{"name":"preview_voucher","arguments":"{\"template_code\":\"A\"}"}}]}}""")]
    public void Ollama_ParsesToolCalls_ObjectOrStringArguments_AndMakesIds(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = OllamaAgentChatModel.ParseReply(doc.RootElement);
        Assert.Null(r.Text);
        var call = r.ToolCalls.Single();
        Assert.StartsWith("call_", call.Id);
        Assert.Equal("A", JsonDocument.Parse(call.ArgumentsJson).RootElement.GetProperty("template_code").GetString());
    }

    [Fact]
    public void Ollama_ParsesPlainReply_SkipsNamelessCalls()
    {
        using var doc = JsonDocument.Parse("""{"message":{"content":"سلام","tool_calls":[{"function":{"arguments":{}}}]}}""");
        var r = OllamaAgentChatModel.ParseReply(doc.RootElement);
        Assert.Equal("سلام", r.Text);
        Assert.Empty(r.ToolCalls);
    }
}

public sealed class AssistantLlmDiTests
{
    [Theory]
    [InlineData("None", null, typeof(DisabledAgentChatModel))]
    [InlineData("Claude", null, typeof(DisabledAgentChatModel))] // Claude بدون کلید = خاموش
    [InlineData("Claude", "k", typeof(ClaudeAgentChatModel))]
    [InlineData("Ollama", null, typeof(OllamaAgentChatModel))]
    public void Provider_SelectsAgentModel_AndBindsAgentOptions(string provider, string? key, Type expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Assistant:Llm:Provider"] = provider,
                ["Assistant:Llm:ApiKey"] = key,
                ["Assistant:Agent:MaxToolRounds"] = "3",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddAssistantLlm(config);
        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        var model = scope.ServiceProvider.GetRequiredService<IAgentChatModel>();
        Assert.IsType(expected, model);
        Assert.Equal(3, sp.GetRequiredService<AssistantAgentOptions>().MaxToolRounds);
    }
}
