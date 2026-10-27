using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Accounting.Application.OperationTemplates.Agent;
using Accounting.Infrastructure.OperationTemplates.Agent;
using Microsoft.Extensions.Options;

namespace Accounting.Infrastructure.Tests.OperationTemplates;

/// <summary>
/// <c>Assistant:Llm:Think</c> — qwen3 بی‌فکر (false) سریع است؛ خالی ⇒ اصلاً فرستاده نمی‌شود چون مدل‌های بدون
/// قابلیت فکر (qwen2.5) نباید درگیرش شوند.
/// </summary>
public sealed class OllamaThinkOptionTests
{
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public JsonNode? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"message":{"content":"سلام"}}""", Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static async Task<JsonNode> SentBody(bool? think)
    {
        var handler = new CaptureHandler();
        var model = new OllamaAgentChatModel(
            new Factory(handler),
            Options.Create(new AssistantLlmOptions { Provider = "Ollama", Model = "qwen3:8b", Think = think }));

        await model.CompleteAsync(new AgentModelRequest("SYS", [], []), CancellationToken.None);
        return handler.Body!;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Think_WhenSet_IsSent(bool think)
    {
        var body = await SentBody(think);
        Assert.Equal(think, body["think"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Think_WhenUnset_IsOmitted()
    {
        var body = await SentBody(null);
        Assert.Null(body["think"]);
    }
}
