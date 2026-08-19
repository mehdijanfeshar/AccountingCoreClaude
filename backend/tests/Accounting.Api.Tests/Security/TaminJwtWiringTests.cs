using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tamin.Framework.Common.Security;

namespace Accounting.Api.Tests.Security;

/// <summary>
/// Regression guards over the third-party <c>Tamin.Framework.Common.Security</c> package's
/// registration behaviour, which <c>Program.cs</c> depends on but does not control.
///
/// <para>
/// These are pure <see cref="ServiceCollection"/> unit tests — no network call is made to the
/// org IDP (the package embeds a static signing key rather than fetching JWKS/discovery), no
/// database is touched, and no host is built.
/// </para>
///
/// <para>
/// Why these exist: the API's entire authorization model rests on
/// <c>SetFallbackPolicy(RequireAuthenticatedUser)</c>, which silently authenticates nothing if
/// the package ever stops registering a resolvable default scheme. A package upgrade could
/// change that without any compile error — these tests turn that into a red test instead.
/// </para>
/// </summary>
public sealed class TaminJwtWiringTests
{
    private const string TestAudience = "test-audience-value";

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTaminJWTToken(TestAudience, Environments.Test);
        return services.BuildServiceProvider();
    }

    private static JwtBearerOptions GetBearerOptions(ServiceProvider provider) =>
        provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

    [Fact]
    public void AddTaminJWTToken_registers_the_standard_Bearer_scheme()
    {
        using var provider = BuildProvider();

        var options = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        var scheme = Assert.Single(options.Schemes);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, scheme.Name);
        Assert.Equal(typeof(JwtBearerHandler), scheme.HandlerType);
    }

    [Fact]
    public async Task Default_authenticate_and_challenge_schemes_resolve_to_Bearer()
    {
        // This is what makes SetFallbackPolicy(RequireAuthenticatedUser) in Program.cs actually
        // authenticate/challenge. If this regresses, every endpoint silently stops being
        // protected (or starts failing to challenge) with no compile-time signal.
        using var provider = BuildProvider();

        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        var authenticateScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        var challengeScheme = await schemeProvider.GetDefaultChallengeSchemeAsync();

        Assert.NotNull(authenticateScheme);
        Assert.NotNull(challengeScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authenticateScheme!.Name);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, challengeScheme!.Name);
    }

    [Fact]
    public void The_configured_audience_is_the_one_actually_validated()
    {
        using var provider = BuildProvider();

        var parameters = GetBearerOptions(provider).TokenValidationParameters;

        Assert.True(parameters.ValidateAudience);
        Assert.NotNull(parameters.ValidAudiences);
        Assert.Contains(TestAudience, parameters.ValidAudiences!);
    }

    [Fact]
    public void Token_validation_is_not_silently_relaxed()
    {
        using var provider = BuildProvider();

        var parameters = GetBearerOptions(provider).TokenValidationParameters;

        Assert.True(parameters.ValidateIssuer);
        Assert.True(parameters.ValidateLifetime);
        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.False(string.IsNullOrWhiteSpace(parameters.ValidIssuer));
        Assert.NotNull(parameters.IssuerSigningKey);
    }

    [Fact]
    public void Package_supplies_an_events_object_for_Program_to_chain_onto()
    {
        // Program.cs deliberately CHAINS onto OnChallenge/OnForbidden rather than assigning a
        // fresh JwtBearerEvents, so that the package's own OnMessageReceived /
        // OnAuthenticationFailed diagnostics survive. That chaining is only meaningful while the
        // package keeps supplying an events object here.
        using var provider = BuildProvider();

        var events = GetBearerOptions(provider).Events;

        Assert.NotNull(events);
        Assert.IsType<JwtBearerEvents>(events);
    }
}
