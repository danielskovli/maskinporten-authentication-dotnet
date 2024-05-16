using MaskinportenAuthentication.Delegates;
using MaskinportenAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MaskinportenAuthentication.Extensions;

public static class ServiceCollectionExtensions
{
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
