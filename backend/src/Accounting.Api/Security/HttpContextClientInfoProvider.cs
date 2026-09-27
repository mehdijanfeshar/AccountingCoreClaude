using Accounting.Application.Common.Interfaces;

namespace Accounting.Api.Security;

/// <summary>
/// <see cref="IClientInfoProvider"/> implementation backed by <see cref="IHttpContextAccessor"/>.
/// Registered scoped (one instance per request), mirroring <see cref="HttpContextCurrentUser"/>.
/// </summary>
public sealed class HttpContextClientInfoProvider : IClientInfoProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextClientInfoProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? ClientIp => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}
