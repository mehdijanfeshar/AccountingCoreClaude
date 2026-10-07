using System.Security.Claims;
using Accounting.Api.Security;

namespace Accounting.Api.Tests.Security;

/// <summary>توکن Keycloak باید به همان claimهایی برسد که HttpContextCurrentUser از توکن سامانهٔ ورود سازمان می‌خواند.</summary>
public sealed class KeycloakClaimsTransformationTests
{
    private static ClaimsPrincipal KeycloakToken(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "Bearer"));

    private static readonly Claim[] Typical =
    [
        new("sub", "6f1c2a7e-0d7b-4a52-9f1b-3c2d1e0f9a88"),
        new("preferred_username", "0000000002"),
        new("vahed_code", "1155"),
        new("resource_access", """{"accounting-api":{"roles":["FINANCIAL CORE MALI ADMIN"]},"account":{"roles":["manage-account"]}}""", "JSON"),
        new("realm_access", """{"roles":["offline_access"]}""", "JSON"),
    ];

    [Fact]
    public async Task MapsUserIdUnitAndClientRoles()
    {
        var result = await new KeycloakClaimsTransformation(new KeycloakOptions()).TransformAsync(KeycloakToken(Typical));

        Assert.Equal("0000000002", result.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("1155", result.FindFirstValue(KeycloakAuthentication.UnitClaimType));
        Assert.True(result.IsInRole("FINANCIAL CORE MALI ADMIN"));
        Assert.True(result.IsInRole("offline_access"));
        // نقش Client دیگر (account) نقش برنامه نیست.
        Assert.False(result.IsInRole("manage-account"));
    }

    [Fact]
    public async Task DoesNotUseSubAsUserId_BecauseAuditColumnsHoldTenChars()
    {
        var result = await new KeycloakClaimsTransformation(new KeycloakOptions())
            .TransformAsync(KeycloakToken(new Claim("sub", Guid.NewGuid().ToString())));

        Assert.Null(result.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    public async Task ClaimNamesAreConfigurable()
    {
        var options = new KeycloakOptions { UserIdClaim = "national_code", UnitClaim = "org", RolesClient = "fin" };
        var result = await new KeycloakClaimsTransformation(options).TransformAsync(KeycloakToken(
            new Claim("national_code", "1234567890"),
            new Claim("org", "0000"),
            new Claim("resource_access", """{"fin":{"roles":["FINANCIAL CORE SETAD ADMIN"]}}""", "JSON")));

        Assert.Equal("1234567890", result.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("0000", result.FindFirstValue(KeycloakAuthentication.UnitClaimType));
        Assert.True(result.IsInRole("FINANCIAL CORE SETAD ADMIN"));
    }

    [Fact]
    public async Task RunningTwice_DoesNotDuplicateClaims()
    {
        var transformation = new KeycloakClaimsTransformation(new KeycloakOptions());
        var once = await transformation.TransformAsync(KeycloakToken(Typical));
        var twice = await transformation.TransformAsync(once);

        Assert.Single(twice.FindAll(ClaimTypes.NameIdentifier));
        Assert.Single(twice.FindAll(c => c.Type == ClaimTypes.Role && c.Value == "FINANCIAL CORE MALI ADMIN"));
    }

    [Fact]
    public async Task AnonymousPrincipal_IsLeftAlone()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var result = await new KeycloakClaimsTransformation(new KeycloakOptions()).TransformAsync(anonymous);

        Assert.Same(anonymous, result);
    }
}
