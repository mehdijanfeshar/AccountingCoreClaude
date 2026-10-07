using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Accounting.Api.Security;

/// <summary>
/// تنظیمات Keycloak — بخش <c>Keycloak</c> در appsettings؛ فقط وقتی <c>Auth:Provider = Keycloak</c>.
/// </summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>آدرس Realm، مثل <c>http://localhost:8080/realms/accounting</c> (همان <c>iss</c> توکن).</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary><c>aud</c> مورد انتظار — Client بک‌اند (نقش‌ها هم Client Roleهای همین Client‌اند).</summary>
    public string Audience { get; set; } = "accounting-api";

    /// <summary>Client که نقش‌های <c>FINANCIAL CORE …</c> روی آن تعریف شده‌اند (<c>resource_access.{client}.roles</c>).</summary>
    public string RolesClient { get; set; } = "accounting-api";

    /// <summary>
    /// Claim شناسهٔ کاربر — ستون‌های <c>ADDUSERID</c>/<c>CHANGEUSERID</c> حداکثر ۱۰ نویسه‌اند (کد ملی)، پس
    /// <c>sub</c> (GUID ۳۶ نویسه‌ای Keycloak) به کار نمی‌آید. پیش‌فرض: نام کاربری = کد ملی.
    /// </summary>
    public string UserIdClaim { get; set; } = "preferred_username";

    /// <summary>Claim کد واحد کاربر (ویژگی کاربر در Keycloak با Mapper «User Attribute»).</summary>
    public string UnitClaim { get; set; } = "vahed_code";

    /// <summary>فقط توسعه: Keycloak محلی روی http.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}

public static class KeycloakAuthentication
{
    /// <summary>Claim داخلی کد واحد — همان که <see cref="HttpContextCurrentUser"/> از توکن سامانهٔ ورود سازمان می‌خواند.</summary>
    public const string UnitClaimType = "urn:tamin:jwt:claim:org";

    /// <summary>
    /// JWT Bearer با discovery/JWKS خود Keycloak، روی همان طرح «Bearer» که پکیج سازمان ثبت می‌کرد — پس
    /// FallbackPolicy، رویدادهای ProblemDetails و بقیهٔ Program.cs بی‌تغییر می‌مانند.
    /// </summary>
    public static IServiceCollection AddKeycloakJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>() ?? new KeycloakOptions();
        if (string.IsNullOrWhiteSpace(options.Authority))
            throw new InvalidOperationException("Auth:Provider = Keycloak ولی Keycloak:Authority خالی است.");

        services.AddSingleton(options);
        services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwt =>
            {
                jwt.Authority = options.Authority.TrimEnd('/');
                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                // نام claimها همان‌طور که Keycloak می‌فرستد (sub نه NameIdentifier)؛ نگاشت در KeycloakClaimsTransformation.
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters.ValidateIssuer = true;
                jwt.TokenValidationParameters.ValidateAudience = true;
                jwt.TokenValidationParameters.NameClaimType = "name";
                jwt.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
            });

        return services;
    }
}

/// <summary>
/// توکن Keycloak را به همان شکلی درمی‌آورد که بقیهٔ برنامه از توکن سامانهٔ ورود سازمان انتظار دارد:
/// شناسهٔ کاربر ⇒ <see cref="ClaimTypes.NameIdentifier"/>، کد واحد ⇒ <c>urn:tamin:jwt:claim:org</c>، و Client Roleها
/// (<c>resource_access.{client}.roles</c>) به‌علاوهٔ Realm Roleها ⇒ <see cref="ClaimTypes.Role"/>.
/// </summary>
public sealed class KeycloakClaimsTransformation : IClaimsTransformation
{
    private const string MarkerType = "accounting:keycloak-mapped";
    private readonly KeycloakOptions _options;

    public KeycloakClaimsTransformation(KeycloakOptions options)
    {
        _options = options;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } source || principal.HasClaim(c => c.Type == MarkerType))
            return Task.FromResult(principal);

        var identity = new ClaimsIdentity(source.Claims, source.AuthenticationType, "name", ClaimTypes.Role);
        identity.AddClaim(new Claim(MarkerType, "1"));

        if (principal.FindFirst(_options.UserIdClaim)?.Value is { Length: > 0 } userId)
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

        if (principal.FindFirst(_options.UnitClaim)?.Value is { Length: > 0 } unit)
            identity.AddClaim(new Claim(KeycloakAuthentication.UnitClaimType, unit));

        foreach (var role in ReadRoles(principal))
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return Task.FromResult(new ClaimsPrincipal(identity));
    }

    private IEnumerable<string> ReadRoles(ClaimsPrincipal principal)
    {
        var roles = new HashSet<string>(StringComparer.Ordinal);

        if (principal.FindFirst("resource_access")?.Value is { Length: > 0 } resourceAccess)
        {
            using var doc = JsonDocument.Parse(resourceAccess);
            if (doc.RootElement.TryGetProperty(_options.RolesClient, out var client)
                && client.TryGetProperty("roles", out var clientRoles)
                && clientRoles.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in clientRoles.EnumerateArray())
                    if (r.GetString() is { Length: > 0 } s) roles.Add(s);
            }
        }

        if (principal.FindFirst("realm_access")?.Value is { Length: > 0 } realmAccess)
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var realmRoles) && realmRoles.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in realmRoles.EnumerateArray())
                    if (r.GetString() is { Length: > 0 } s) roles.Add(s);
            }
        }

        return roles;
    }
}
