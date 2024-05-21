using MaskinportenAuthentication.Models;
using MaskinportenAuthentication.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MaskinportenAuthentication.Services;

/// <summary>
/// A hosted service which will immediately load the `maskinporten-settings.json`,
/// and thereafter at <see cref="MaskinportenSettingsLoader.ReloadInterval"/> intervals.
/// </summary>
public class MaskinportenSettingsLoader : IHostedService, IAsyncDisposable
{
    // TODO: These are public because I wanted visibility for callers. Necessary?
    public static string AppSettingsKeyName => "MaskinportenSettingsFilepath";
    public static string DefaultSettingsFilepath => "/mnt/app-secrets/maskinporten-settings.json";
    public static TimeSpan ReloadInterval => TimeSpan.FromMinutes(5);

    private readonly string? _settingsFilepath;
    private readonly ILogger<MaskinportenSettingsLoader>? _logger;
    private Timer? _timer;

    /// <summary>
    /// Instantiates a new <see cref="MaskinportenSettingsLoader"/> service.
    /// </summary>
    /// <param name="config">Configuration object to get the value associated with <see cref="AppSettingsKeyName"/> from</param>
    /// <param name="logger">Optional logger interface</param>
    public MaskinportenSettingsLoader(IConfiguration config, ILogger<MaskinportenSettingsLoader>? logger = default)
    {
        _settingsFilepath = config.GetValue<string>(AppSettingsKeyName);
        _logger = logger;
    }

    /// <summary>
    /// Instantiates a new <see cref="MaskinportenSettingsLoader"/> service.
    /// </summary>
    /// <param name="settingsFilepath">Path to a `maskinporten-settings.json` file</param>
    /// <param name="logger">Optional logger interface</param>
    public MaskinportenSettingsLoader(string settingsFilepath, ILogger<MaskinportenSettingsLoader>? logger = default)
    {
        _settingsFilepath = settingsFilepath;
        _logger = logger;
    }

    /// <summary>
    /// Loads Maskinporten settings from the file defined <see cref="_settingsFilepath"/>,
    /// alternatively falling back to <see cref="DefaultSettingsFilepath"/> if nothing has been provided.
    /// </summary>
    /// <param name="state">Unused: Timer implementation requirement</param>
    private void LoadSettings(object? state)
    {
        var settingsFilepath = _settingsFilepath;
        if (settingsFilepath is null)
        {
            _logger?.LogWarning(
                "No filepath to settings object has been provided. Assuming {DefaultFilepath}",
                DefaultSettingsFilepath
            );
            settingsFilepath = DefaultSettingsFilepath;
        }

        var settings = JsonLoader<MaskinportenSettings>.LoadFile(settingsFilepath);
        MaskinportenClient.Configure(settings);
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting service {Service}", nameof(MaskinportenSettingsLoader));
        _timer = new Timer(LoadSettings, null, TimeSpan.Zero, ReloadInterval);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Shutting down service {Service}", nameof(MaskinportenSettingsLoader));
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_timer is IAsyncDisposable timer)
        {
            await timer.DisposeAsync();
        }

        _timer = null;
    }
}
