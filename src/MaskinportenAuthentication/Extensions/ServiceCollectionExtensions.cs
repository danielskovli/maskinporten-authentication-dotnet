using MaskinportenAuthentication.Delegates;
using MaskinportenAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MaskinportenAuthentication.Extensions;

/// <summary>
/// Extension methods used to configure an <see cref="IServiceCollection"/> instance (asp.net).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a named http client to the services collection.<br/><br/>
    /// This client will be automatically load Maskinporten settings via <see cref="MaskinportenSettingsLoader"/> and
    /// provide authentication for every request via <see cref="MaskinportenDelegatingHandler"/>.<br/><br/>
    /// The key `MaskinportenSettingsFilepath` in appsettings.json defines where the
    /// `maskinporten-settings.json` file should be loaded from.
    /// </summary>
    /// <param name="services">The service collection to add the http client to.</param>
    /// <param name="clientName">The name of the client.</param>
    /// <param name="scopes">The scopes to associate with the authorization for this client.</param>
    public static IHttpClientBuilder AddMaskinportenHttpClient(
        this IServiceCollection services,
        string clientName,
        IEnumerable<string> scopes
    )
    {
        services.AddHostedService<MaskinportenSettingsLoader>();

        return services
            .AddHttpClient(clientName)
            .AddHttpMessageHandler(provider => new MaskinportenDelegatingHandler(
                scopes,
                loggerFactory: provider.GetService<ILoggerFactory>()
            ));
    }

    /// <summary>
    /// Adds a named http client to the services collection.<br/><br/>
    /// This client will be automatically load Maskinporten settings via <see cref="MaskinportenSettingsLoader"/> and
    /// provide authentication for every request via <see cref="MaskinportenDelegatingHandler"/>.<br/><br/>
    /// The key `MaskinportenSettingsFilepath` in appsettings.json defines where the
    /// `maskinporten-settings.json` file should be loaded from.
    /// </summary>
    /// <param name="services">The service collection to add the http client to.</param>
    /// <param name="clientName">The name of the client.</param>
    /// /// <param name="scopes">The scopes to associate with the authorization for this client.</param>
    /// <param name="configureClient">A delegate that is used to configure the created <see cref="HttpClient"/>.</param>
    public static IHttpClientBuilder AddMaskinportenHttpClient(
        this IServiceCollection services,
        string clientName,
        IEnumerable<string> scopes,
        Action<HttpClient> configureClient
    )
    {
        services.AddHostedService<MaskinportenSettingsLoader>();

        return services
            .AddHttpClient(clientName, configureClient)
            .AddHttpMessageHandler(provider => new MaskinportenDelegatingHandler(
                scopes,
                loggerFactory: provider.GetService<ILoggerFactory>()
            ));
    }
}
