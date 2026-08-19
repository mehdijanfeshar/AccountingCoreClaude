using System.Security.Claims;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Api.Security;

/// <summary>
/// <see cref="ICurrentUser"/> implementation backed by <see cref="IHttpContextAccessor"/>.
/// Reads identity information from claims populated by the JWT bearer authentication
/// middleware configured in <c>Program.cs</c>. Registered scoped (one instance per request).
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    /// <summary>
    /// <c>TB_ACCOUNTCODE.ADDUSERID</c> / <c>TB_VOUCHERSHEAD.ADDUSERID</c> are <c>varchar(10)</c>
    /// in Legacy. Silently truncating an authenticated identity into an audit column is worse
    /// than failing loudly.
    /// </summary>
    private const int MaxUserIdLength = 10;

    private const string VahedCodeClaimType = "vahed_code";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string UserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is null || !(httpContext.User?.Identity?.IsAuthenticated ?? false))
            {
                throw new InvalidOperationException(
                    "Cannot read ICurrentUser.UserId: there is no authenticated user on the current request.");
            }

            var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException(
                    "Cannot read ICurrentUser.UserId: the authenticated principal has no NameIdentifier claim.");
            }

            if (value.Length > MaxUserIdLength)
            {
                throw new InvalidOperationException(
                    $"ICurrentUser.UserId value '{value}' is {value.Length} characters long, which exceeds the " +
                    $"{MaxUserIdLength}-character limit of the Legacy ADDUSERID/CHANGEUSERID audit columns. " +
                    "Refusing to truncate an identity silently.");
            }

            return value;
        }
    }

    public string? VahedCode => _httpContextAccessor.HttpContext?.User?.FindFirstValue(VahedCodeClaimType);

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}
