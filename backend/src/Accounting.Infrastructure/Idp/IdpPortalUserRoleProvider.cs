using System.Net.Http.Headers;
using System.Text.Json;
using Accounting.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Idp;

/// <summary>
/// نقش‌های کاربر از پورتال سامانهٔ ورود — عین <c>CurrentUserRepository</c> سیستم قدیم: توکن سرویس
/// (<c>client_credentials</c>، کلید <c>"tamin"</c> در بخش <c>Idp</c> کانفیگ) و
/// <c>GET {CurrentUserServiceConfig:BaseAddress}{GetCurrentUserUrl}/{userId}/info</c>؛ نقش‌ها از
/// <c>data.roles[].roleName</c>. نتیجه ۱۰ دقیقه برای هر کاربر نگه داشته می‌شود (خطا ۱ دقیقه).
/// </summary>
public sealed class IdpPortalUserRoleProvider : IUserRoleProvider
{
    private const string TokenKey = "tamin";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan FailureTtl = TimeSpan.FromMinutes(1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenManager _tokenManager;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdpPortalUserRoleProvider> _logger;

    public IdpPortalUserRoleProvider(
        IHttpClientFactory httpClientFactory,
        ITokenManager tokenManager,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<IdpPortalUserRoleProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenManager = tokenManager;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<string>();

        var key = "idp-roles:" + userId;
        if (_cache.TryGetValue(key, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        var baseAddress = _configuration["CurrentUserServiceConfig:BaseAddress"];
        var path = _configuration["CurrentUserServiceConfig:GetCurrentUserUrl"] ?? "/api/v2.0/users";
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            _logger.LogWarning("CurrentUserServiceConfig:BaseAddress is not configured; user roles cannot be loaded.");
            return Array.Empty<string>();
        }

        try
        {
            var token = await _tokenManager.GetAccessTokenAsync(TokenKey, cancellationToken);
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"{baseAddress.TrimEnd('/')}{path}/{Uri.EscapeDataString(userId)}/info");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClientFactory.CreateClient(nameof(IdpPortalUserRoleProvider))
                .SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var roles = new List<string>();
            if (doc.RootElement.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("roles", out var rolesElement)
                && rolesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in rolesElement.EnumerateArray())
                {
                    if (role.ValueKind == JsonValueKind.Object
                        && role.TryGetProperty("roleName", out var name)
                        && name.GetString() is { Length: > 0 } value)
                    {
                        roles.Add(value.Trim());
                    }
                }
            }

            IReadOnlyList<string> result = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            _cache.Set(key, result, Ttl);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Loading roles for user {UserId} from the IDP portal failed.", userId);
            IReadOnlyList<string> empty = Array.Empty<string>();
            _cache.Set(key, empty, FailureTtl);
            return empty;
        }
    }
}
