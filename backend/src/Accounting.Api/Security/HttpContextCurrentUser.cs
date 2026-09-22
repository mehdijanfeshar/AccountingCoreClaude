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

    /// <summary>
    /// Confirmed against a real Tamin IDP token and live Oracle data (2026-09-12): the org
    /// claim's value ("0000") matches <c>TB_VAHED_INFO.VAHEDCODE</c> ("ستاد مرکزی") exactly, so
    /// this is a plain claim-name correction, not a value transform. The previous "vahed_code"
    /// name was never validated against a real IDP token (see open-decisions.md risk #10) and
    /// does not exist on real tokens, so every VahedCode read silently came back null.
    /// </summary>
    private const string VahedCodeClaimType = "urn:tamin:jwt:claim:org";

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

    /// <summary>
    /// Header name the «تغییر واحد» picker sets. A header rather than a body/query field on
    /// purpose: it applies uniformly to all ~60 existing commands and queries without changing a
    /// single contract, and it keeps the untrusted value out of the request models entirely —
    /// phase 33 deliberately removed <c>VahedCode</c> from API contracts and that stays true.
    /// </summary>
    private const string RequestedVahedCodeHeader = "X-Vahed-Code";

    public string? RequestedVahedCode
    {
        get
        {
            var headers = _httpContextAccessor.HttpContext?.Request?.Headers;

            if (headers is null || !headers.TryGetValue(RequestedVahedCodeHeader, out var values))
            {
                return null;
            }

            // A repeated header is a malformed request, not a choice to guess at. Taking the first
            // value would let a caller smuggle a second one past anything that inspects only one.
            if (values.Count != 1)
            {
                return null;
            }

            var value = values[0];

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}
