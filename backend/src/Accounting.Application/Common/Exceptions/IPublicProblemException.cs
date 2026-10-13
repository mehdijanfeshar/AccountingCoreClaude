namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// An exception whose message is safe to show the caller. <c>GlobalExceptionHandler</c> turns any
/// implementation into an RFC 7807 response with <see cref="StatusCode"/> and
/// <see cref="PublicDetail"/> — a new business exception needs no new branch in the handler.
/// </summary>
/// <remarks>
/// <see cref="PublicDetail"/> must never name a Legacy table, column or constraint.
/// </remarks>
public interface IPublicProblemException
{
    /// <summary>The HTTP status the exception maps to (400, 403, 404, 409, 422).</summary>
    int StatusCode { get; }

    /// <summary>The text that is safe to return in the HTTP response body.</summary>
    string PublicDetail { get; }
}
