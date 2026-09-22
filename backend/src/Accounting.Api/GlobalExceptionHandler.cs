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

            // The only mapping here that returns a per-exception detail rather than a fixed
            // string. That is deliberate and safe: what it carries is a تفصیلی level's business
            // name, which the caller already sees on the form — not a Legacy table, column or
            // constraint name. See TafsiliLevelRuleException for the reasoning.
            TafsiliLevelRuleException tafsiliLevelRuleException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    tafsiliLevelRuleException.PublicDetail)),

            // State-based refusal, not a permissions one: the caller MAY edit this voucher, just
            // not while it is reviewed/accepted — and they can fix that themselves via
            // change-state. See VoucherNotEditableException for why 409 rather than 403.
            // Terminal-state refusal: تأیید دائم can never be moved. Separate from
            // VoucherNotEditableException because that one offers a way out and this one cannot.
            VoucherStateChangeDeniedException voucherStateChangeDeniedException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    voucherStateChangeDeniedException.PublicDetail)),

            VoucherNotEditableException voucherNotEditableException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    voucherNotEditableException.PublicDetail)),

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

            // Scope-level 403: the request asked to BE a unit the caller may not be, decided
            // before any row is touched — as opposed to the record-level
            // UnitAccessDeniedException below. See UnitActAsDeniedException for why they are not
            // collapsed. Carries a per-exception detail for the same reason
            // TafsiliLevelRuleException does: the value echoed back is the unit code the caller
            // themselves just sent.
            UnitActAsDeniedException unitActAsDeniedException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    unitActAsDeniedException.PublicDetail)),

            // A fourth distinct 403: the token names a unit, but no TB_VAHED_INFO row carries that
            // code — an IDP/Legacy data mismatch rather than a permissions decision. The unit code
            // echoed back is the caller's own, which their token already carries.
            UnknownCallerUnitException unknownCallerUnitException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    unknownCallerUnitException.PublicDetail)),

            // Deliberately a different detail string from MissingVahedScopeException above, even
            // though both are 403: that one means "we could not work out which unit you are", this
            // one means "we know which unit you are, and this record is not yours". Collapsing them
            // would make a misconfigured token and a cross-unit access attempt indistinguishable in
            // support. Neither body names the owning unit — that stays in the log.
            UnitAccessDeniedException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    "This record belongs to a different organizational unit.")),

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
