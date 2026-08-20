using System.Text;
using System.Text.Json;
using Accounting.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Accounting.Api.Tests;

/// <summary>
/// Exercises <see cref="GlobalExceptionHandler"/> end to end against a real
/// <see cref="IProblemDetailsService"/> (resolved from a minimal DI container built with the
/// same <c>AddProblemDetails()</c> call <c>Program.cs</c> uses) so the assertions cover the
/// actual serialized HTTP response body, not just the in-memory <c>ProblemDetails</c> object.
/// </summary>
public sealed class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, DefaultHttpContext HttpContext, MemoryStream Body) CreateHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();

        var problemDetailsService = provider.GetRequiredService<IProblemDetailsService>();
        var handler = new GlobalExceptionHandler(problemDetailsService, NullLogger<GlobalExceptionHandler>.Instance);

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Request.Path = "/api/account-codes";
        var body = new MemoryStream();
        httpContext.Response.Body = body;

        return (handler, httpContext, body);
    }

    private static async Task<JsonDocument> ReadBodyAsJsonAsync(MemoryStream body)
    {
        body.Position = 0;
        using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
        var json = await reader.ReadToEndAsync();
        return JsonDocument.Parse(json);
    }

    private static async Task<string> ReadBodyAsTextAsync(MemoryStream body)
    {
        body.Position = 0;
        using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_Returns400_WithFieldToMessagesDictionaryGroupedCorrectly()
    {
        var (handler, httpContext, body) = CreateHandler();
        var failures = new[]
        {
            new ValidationFailure("AccCode", "AccCode is required."),
            new ValidationFailure("AccCode", "AccCode must not exceed 6 characters."),
            new ValidationFailure("AccCodeName", "AccCodeName is required."),
        };

        var handled = await handler.TryHandleAsync(httpContext, new ValidationException(failures), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

        using var doc = await ReadBodyAsJsonAsync(body);
        var errors = doc.RootElement.GetProperty("errors");

        var accCodeMessages = errors.GetProperty("AccCode").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(2, accCodeMessages.Length);
        Assert.Contains("AccCode is required.", accCodeMessages);
        Assert.Contains("AccCode must not exceed 6 characters.", accCodeMessages);

        var accCodeNameMessages = errors.GetProperty("AccCodeName").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(accCodeNameMessages);
        Assert.Equal("AccCodeName is required.", accCodeNameMessages[0]);

        Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task TryHandleAsync_DuplicateKeyException_Returns409_WithGenericSafeMessage()
    {
        var (handler, httpContext, body) = CreateHandler();
        var exception = new DuplicateKeyException(
            "A row with the same unique key already exists.",
            new InvalidOperationException("ORA-00001: unique constraint (X.UK_ACCOUNTCODE) violated"));

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);

        using var doc = await ReadBodyAsJsonAsync(body);
        Assert.Equal(409, doc.RootElement.GetProperty("status").GetInt32());

        var text = await ReadBodyAsTextAsync(body);
        // The 409 body must carry the generic message baked into GlobalExceptionHandler, never
        // the inner exception's raw Oracle text — even though DuplicateKeyException.InnerException
        // does contain it.
        Assert.DoesNotContain("ORA-00001", text);
        Assert.DoesNotContain("UK_ACCOUNTCODE", text);
    }

    [Fact]
    public async Task TryHandleAsync_NotFoundException_Returns404_WithGenericSafeMessage_AndDoesNotLeakResourceNameOrId()
    {
        var (handler, httpContext, body) = CreateHandler();
        var id = Guid.NewGuid();
        var exception = new NotFoundException("AccountCode", id);

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);

        using var doc = await ReadBodyAsJsonAsync(body);
        Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            "The requested resource was not found.",
            doc.RootElement.GetProperty("detail").GetString());

        var text = await ReadBodyAsTextAsync(body);
        // NotFoundException.Message embeds the resource name and id (e.g. "AccountCode with id
        // '...' was not found.") — GlobalExceptionHandler must write its own generic message
        // instead of exception.Message, so neither the table/resource name nor the raw guid
        // string may leak into the response body. ("was not found" is deliberately NOT asserted
        // against here — it is a substring of the intentional generic detail text itself
        // ("The requested resource was not found."), not a sign of a leak.)
        Assert.DoesNotContain("AccountCode", text);
        Assert.DoesNotContain(id.ToString(), text);
        Assert.DoesNotContain("with id", text);
    }

    [Fact]
    public async Task TryHandleAsync_ArbitraryException_Returns500_AndBodyContainsNoStackTraceAndNoOracleSqlText()
    {
        var (handler, httpContext, body) = CreateHandler();
        Exception exception;
        try
        {
            // Deliberately give the exception a realistic Oracle-flavoured message AND a real
            // populated stack trace (by actually throwing/catching it), so the assertions prove
            // GlobalExceptionHandler builds its own generic ProblemDetails rather than ever
            // echoing exception.Message or exception.StackTrace into the response.
            throw new InvalidOperationException(
                "ORA-00001: unique constraint (CENTRALACCOUNT.UK_ACCOUNTCODE) violated -- " +
                "SELECT * FROM TB_ACCOUNTCODE WHERE ACCCODE = '100100'");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        var text = await ReadBodyAsTextAsync(body);

        Assert.DoesNotContain("ORA-00001", text);
        Assert.DoesNotContain("UK_ACCOUNTCODE", text);
        Assert.DoesNotContain("SELECT", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TB_ACCOUNTCODE", text);
        Assert.DoesNotContain(nameof(InvalidOperationException), text);
        Assert.DoesNotContain(".cs:line", text);
        Assert.DoesNotContain("   at ", text); // classic .NET stack-frame line prefix

        using var doc = JsonDocument.Parse(text);
        Assert.Equal(500, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            "An unexpected error occurred while processing the request.",
            doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_SetsTraceIdExtension_FromHttpContextTraceIdentifier()
    {
        var (handler, httpContext, body) = CreateHandler();
        httpContext.TraceIdentifier = "trace-abc-123";

        await handler.TryHandleAsync(httpContext, new InvalidOperationException("boom"), CancellationToken.None);

        using var doc = await ReadBodyAsJsonAsync(body);
        Assert.Equal("trace-abc-123", doc.RootElement.GetProperty("traceId").GetString());
    }
}
