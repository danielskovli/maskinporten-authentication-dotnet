using MaskinportenAuthentication.Delegates;
using MaskinportenAuthentication.Exceptions;
using MaskinportenAuthentication.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace MaskinportenAuthentication.Extensions;

public static class MaskinportenClientIntegration
{
    private const string _appSettingsKeyName = "MaskinportenSettingsFilepath";
    private const string _defaultSettingsFilepath = "/mnt/app-secrets/maskinporten-settings.json";

    public static IHostApplicationBuilder AddMaskinportenClient(this IHostApplicationBuilder builder)
    {
        if (builder.Services.Any(x => x.ServiceType == typeof(IMaskinportenClient)))
        {
            // IMaskinportenClient is already registered
            // TODO: Can we log here?
            return builder;
        }

        var jsonProvidedPath = builder.Configuration.GetValue<string>(_appSettingsKeyName) ?? _defaultSettingsFilepath;
        var jsonAbsolutePath = Path.GetFullPath(jsonProvidedPath);
        var jsonDir = Path.GetDirectoryName(jsonAbsolutePath) ?? string.Empty;
        var jsonFile = Path.GetFileName(jsonAbsolutePath);

        if (!File.Exists(jsonAbsolutePath))
        {
            throw new MaskinportenConfigurationException(
                $"Maskinporten settings not found at specified location: {jsonAbsolutePath}"
            );
        }

        builder.Configuration.AddJsonFile(
            provider: new PhysicalFileProvider(jsonDir),
            path: jsonFile,
            optional: false,
            reloadOnChange: true
        );
        builder
            .Services.AddOptions<MaskinportenSettings>()
            .BindConfiguration("MaskinportenSettings")
            .ValidateDataAnnotations();
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = 256;
        });
        builder.Services.AddSingleton<IMaskinportenClient, MaskinportenClient>();

        return builder;
    }

    public static IServiceCollection AddMaskinportenClient(
        this IServiceCollection services,
        Action<MaskinportenSettings> configure
    )
    {
        if (services.Any(x => x.ServiceType == typeof(IMaskinportenClient)))
        {
            // TODO: Can we log here?
            return services;
        }

        services.AddOptions<MaskinportenSettings>().Configure(configure).ValidateDataAnnotations();
        services.AddMemoryCache();
        services.AddSingleton<IMaskinportenClient, MaskinportenClient>();

        return services;
    }

    public static IHttpClientBuilder UseMaskinportenAuthorization(
        this IHttpClientBuilder builder,
        IEnumerable<string> scopes
    )
    {
        return builder.AddHttpMessageHandler(provider => new MaskinportenDelegatingHandler(scopes, provider));
    }
}
