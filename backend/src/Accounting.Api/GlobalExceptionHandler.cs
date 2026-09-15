using Accounting.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api;

/// <summary>
/// Centralized exception-to-HTTP-response translation for every controller in the API.
/// Registered via <c>AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c> in <c>Program.cs</c>; no controller catches exceptions
/// itself. Mapping:
/// <list type="bullet">
/// <item><description><see cref="ValidationException"/> (FluentValidation, raised by
/// <c>ValidationBehavior</c>) → 400 <see cref="HttpValidationProblemDetails"/> with a
/// field→messages dictionary built from <see cref="ValidationException.Errors"/>.</description></item>
/// <item><description><see cref="DuplicateKeyException"/> (raised by
/// <c>UnitOfWork.SaveChangesAsync</c> after translating an Oracle ORA-00001) → 409
/// <see cref="ProblemDetails"/> with a safe, generic message — never the raw Oracle/SQL
/// text.</description></item>
/// <item><description><see cref="ForeignKeyViolationException"/> (raised by
/// <c>UnitOfWork.SaveChangesAsync</c> after translating an Oracle ORA-02291) → 400
/// <see cref="ProblemDetails"/> with a safe, generic message — never the raw Oracle/SQL text,
/// which would name the violated constraint, table and column. 400 rather than 409 because a
/// missing parent key means the caller sent an identifier that references nothing (an invalid
/// payload), not a conflict with existing state — see <see cref="ForeignKeyViolationException"/>
/// XML doc for the full rationale, including why 404 was rejected. Note this is the one 400
/// response on the API that carries a plain <see cref="ProblemDetails"/> rather than an
/// <see cref="HttpValidationProblemDetails"/> with an <c>errors</c> dictionary, since there is no
/// request field to attribute the failure to without leaking the Oracle constraint
/// name.</description></item>
/// <item><description><see cref="NotFoundException"/> (raised by Update/Delete command
/// handlers when the target row does not exist or is already soft-deleted) → 404
/// <see cref="ProblemDetails"/> with a safe, generic message — no table/column name
/// leaked.</description></item>
/// <item><description><see cref="MissingVahedScopeException"/> (raised by
/// <c>VahedScopeBehavior</c> when the authenticated caller has no usable
/// <c>VahedCode</c>/unit-scope claim) → 403 <see cref="ProblemDetails"/>. Deliberately 403, not
/// 401: the caller is authenticated (401 already applies to unauthenticated requests via the
/// fallback authorization policy), it simply lacks a claim usable for this
/// operation.</description></item>
/// <item><description>Anything else → 500 generic <see cref="ProblemDetails"/> with no stack
/// trace in the body. The original exception is still logged via <see cref="ILogger"/>.</description></item>
/// </list>
/// Writes the response through <see cref="IProblemDetailsService"/> (registered by
/// <c>AddProblemDetails()</c> in <c>Program.cs</c>) rather than hand-serializing JSON, so the
/// response gets the correct <c>application/problem+json</c> content type and honours any
/// <c>CustomizeProblemDetails</c> hook registered elsewhere.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                (ProblemDetails)BuildValidationProblemDetails(validationException, httpContext)),

            DuplicateKeyException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    "A row with the same unique key already exists.")),

            ForeignKeyViolationException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    "One or more referenced records do not exist.")),

            NotFoundException => (
                StatusCodes.Status404NotFound,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    "Not Found",
                    "The requested resource was not found.")),

            MissingVahedScopeException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    "The authenticated caller has no usable organizational-unit scope for this operation.")),

            _ => (
                StatusCodes.Status500InternalServerError,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred while processing the request.")),
        };

        // Correlation: lets an operator tie a client-reported error back to the matching log
        // line (both carry the same ASP.NET Core TraceIdentifier for this request).
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        LogException(exception, statusCode, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    private void LogException(Exception exception, int statusCode, string traceId)
    {
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing request {TraceId}.", traceId);
        }
        else
        {
            _logger.LogWarning(exception, "Request {TraceId} failed with status code {StatusCode}.", traceId, statusCode);
        }
    }

    private static HttpValidationProblemDetails BuildValidationProblemDetails(
        ValidationException validationException,
        HttpContext httpContext)
    {
        var errors = validationException.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        return new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Instance = httpContext.Request.Path,
        };
    }

    private static ProblemDetails BuildProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };
    }
}
