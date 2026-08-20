using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure.Idp;

/// <summary>
/// Ports the project owner's <c>AddTaminTokenManager(IServiceCollection, Action&lt;TokenManagerConfiguration&gt;)</c>
/// registration pattern from their other project. Registers <see cref="ITokenManager"/> as a
/// singleton plus a named <see cref="HttpClient"/> (<see cref="TokenManager.HttpClientName"/>)
/// resolved through <c>IHttpClientFactory</c> — never <c>new HttpClient()</c> per call.
/// </summary>
public static class TaminTokenManagerServiceCollectionExtensions
{
    public static IServiceCollection AddTaminTokenManager(
        this IServiceCollection services,
        Action<TokenManagerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new TokenManagerConfiguration();
        configure(configuration);

        services.AddSingleton(configuration);
        services.AddHttpClient(TokenManager.HttpClientName);
        services.AddSingleton<ITokenManager, TokenManager>();

        return services;
    }
}
