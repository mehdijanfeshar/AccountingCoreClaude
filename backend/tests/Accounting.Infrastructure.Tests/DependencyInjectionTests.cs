// NOTE: AddInfrastructure is an extension method declared in the Accounting.Infrastructure
// namespace itself (DependencyInjection.cs); this using is required even though the test
// namespace below is a child of it.
using Accounting.Infrastructure;
using Accounting.Infrastructure.Idp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure.Tests;

/// <summary>
/// Covers <c>DependencyInjection.AddInfrastructure</c>'s <c>Idp:*</c> → <see cref="TokenManagerConfiguration"/>
/// binding — the mapping from the ported config-key naming (<c>External_ClientId</c>,
/// <c>External_ClientSecret</c>, <c>External_Resources</c>) onto <see cref="TokenManagerDetail"/>.
/// This is otherwise untested private logic; a key-name typo here would silently break token
/// acquisition in production while every <see cref="TokenManager"/> unit test (which builds
/// <see cref="TokenManagerDetail"/> directly) would keep passing.
///
/// Only <see cref="IServiceCollection.BuildServiceProvider()"/> + resolving
/// <see cref="TokenManagerConfiguration"/> happens here — <c>LegacyDbContext</c> is registered by
/// the same call but is never resolved, so this never touches Oracle (build-time registration of
/// EF Core options performs no I/O).
/// </summary>
public sealed class DependencyInjectionTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void AddInfrastructure_BindsIdpTaminSection_IntoTokenManagerConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "",
            ["Idp:tamin:Server"] = "https://idp.example.test/connect/token",
            ["Idp:tamin:External_ClientId"] = "client-abc",
            ["Idp:tamin:External_ClientSecret"] = "secret-value",
            ["Idp:tamin:Audience"] = "aud-value",
            ["Idp:tamin:GrantType"] = "client_credentials",
            ["Idp:tamin:External_Resources"] = "res-a, res-b,res-c",
        });

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var tokenManagerConfiguration = provider.GetRequiredService<TokenManagerConfiguration>();

        Assert.True(tokenManagerConfiguration.TokenManagers.TryGetValue("tamin", out var detail));
        Assert.Equal("https://idp.example.test/connect/token", detail!.Server);
        Assert.Equal("client-abc", detail.ClientId);
        Assert.Equal("secret-value", detail.ClientSecret);
        Assert.Equal("aud-value", detail.Audience);
        Assert.Equal("client_credentials", detail.GrantType);
        Assert.Equal(new[] { "res-a", "res-b", "res-c" }, detail.Resources);
    }

    [Fact]
    public void AddInfrastructure_DefaultsGrantTypeToClientCredentials_WhenNotConfigured()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "",
            ["Idp:tamin:Server"] = "https://idp.example.test/connect/token",
            ["Idp:tamin:External_ClientId"] = "client-abc",
            ["Idp:tamin:External_ClientSecret"] = "secret-value",
        });

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var detail = provider.GetRequiredService<TokenManagerConfiguration>().TokenManagers["tamin"];

        Assert.Equal("client_credentials", detail.GrantType);
        Assert.Empty(detail.Resources);
        Assert.Null(detail.Audience);
    }

    [Fact]
    public void AddInfrastructure_WithNoIdpSectionAtAll_RegistersEmptyTokenManagerConfiguration_AndDoesNotThrow()
    {
        // Matches appsettings.json's placeholder-only shipped state for local/dev environments
        // where nothing calls a downstream tamin service yet — startup must not fail.
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "",
        });

        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
        {
            services.AddInfrastructure(configuration);
            using var provider = services.BuildServiceProvider();
            var tokenManagerConfiguration = provider.GetRequiredService<TokenManagerConfiguration>();
            Assert.Empty(tokenManagerConfiguration.TokenManagers);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void AddInfrastructure_RegistersITokenManager()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "",
        });

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ITokenManager>());
    }
}
