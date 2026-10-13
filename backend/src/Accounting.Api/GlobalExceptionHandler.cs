using Accounting.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

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

            // Fixed generic messages: the exception text itself may carry Oracle/Legacy names.
            DuplicateKeyException => Problem(
                httpContext,
                StatusCodes.Status409Conflict,
                "A row with the same unique key already exists."),

            ForeignKeyViolationException => Problem(
                httpContext,
                StatusCodes.Status400BadRequest,
                "One or more referenced records do not exist."),

            NotFoundException => Problem(
                httpContext,
                StatusCodes.Status404NotFound,
                "The requested resource was not found."),

            MissingVahedScopeException => Problem(
                httpContext,
                StatusCodes.Status403Forbidden,
                "The authenticated caller has no usable organizational-unit scope for this operation."),

            // Deliberately a different detail from MissingVahedScopeException: that one means "we
            // could not work out which unit you are", this one "this record is not yours". Neither
            // body names the owning unit — that stays in the log.
            UnitAccessDeniedException => Problem(
                httpContext,
                StatusCodes.Status403Forbidden,
                "This record belongs to a different organizational unit."),

            // All-or-nothing bulk approvals: the per-id reasons ride along as a `failedIds`
            // extension so the caller can react per row without a second round-trip.
            PettyCashBulkApproveConflictException bulkException => (
                StatusCodes.Status409Conflict,
                BuildBulkApproveConflictProblemDetails(httpContext, bulkException)),

            PaymentRequestBulkApproveConflictException bulkException => (
                StatusCodes.Status409Conflict,
                BuildPaymentRequestBulkApproveConflictProblemDetails(httpContext, bulkException)),

            // Every other business exception declares its own status and caller-safe detail.
            IPublicProblemException publicProblem => Problem(
                httpContext,
                publicProblem.StatusCode,
                publicProblem.PublicDetail),

            _ => Problem(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred while processing the request."),
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

    private static (int StatusCode, ProblemDetails Problem) Problem(HttpContext httpContext, int statusCode, string detail)
    {
        return (statusCode, BuildProblemDetails(httpContext, statusCode, ReasonPhrases.GetReasonPhrase(statusCode), detail));
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

    /// <summary>
    /// Same shape as <see cref="BuildProblemDetails"/> plus a <c>failedIds</c> extension —
    /// <c>[{ id, reason }, ...]</c> — so a bulk-approve caller can react per row without a second
    /// round-trip. <c>reason</c> is one of the fixed machine-readable codes documented on
    /// <see cref="PettyCashBulkApproveConflictException.Failures"/>.
    /// </summary>
    private static ProblemDetails BuildBulkApproveConflictProblemDetails(
        HttpContext httpContext,
        PettyCashBulkApproveConflictException exception)
    {
        var problemDetails = BuildProblemDetails(
            httpContext,
            StatusCodes.Status409Conflict,
            "Conflict",
            exception.PublicDetail);

        problemDetails.Extensions["failedIds"] = exception.Failures
            .Select(pair => new { id = pair.Key, reason = pair.Value })
            .ToArray();

        return problemDetails;
    }

    /// <summary>Same shape as <see cref="BuildBulkApproveConflictProblemDetails"/>, for the
    /// خزانه‌داری bulk-approve endpoint's own exception type.</summary>
    private static ProblemDetails BuildPaymentRequestBulkApproveConflictProblemDetails(
        HttpContext httpContext,
        PaymentRequestBulkApproveConflictException exception)
    {
        var problemDetails = BuildProblemDetails(
            httpContext,
            StatusCodes.Status409Conflict,
            "Conflict",
            exception.PublicDetail);

        problemDetails.Extensions["failedIds"] = exception.Failures
            .Select(pair => new { id = pair.Key, reason = pair.Value })
            .ToArray();

        return problemDetails;
    }
}
