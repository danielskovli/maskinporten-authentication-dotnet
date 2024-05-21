using MaskinportenAuthentication.Models;
using MaskinportenAuthentication.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MaskinportenAuthentication.Services;

public class MaskinportenSettingsLoader : IHostedService, IAsyncDisposable
{
    private static readonly string _appsettingsKeyName = "MaskinportenSettingsFilepath";
    private static readonly string _defaultSettingsFilepath = "/mnt/app-secrets/maskinporten-settings.json";
    private readonly string? _settingsFilepath;
    private readonly ILogger<MaskinportenSettingsLoader>? _logger;
    private Timer? _timer;

    public MaskinportenSettingsLoader(IConfiguration config, ILogger<MaskinportenSettingsLoader>? logger = default)
    {
        _settingsFilepath = config.GetValue<string>(_appsettingsKeyName);
        _logger = logger;
    }

    public MaskinportenSettingsLoader(string settingsFilepath, ILogger<MaskinportenSettingsLoader>? logger = default)
    {
        _settingsFilepath = settingsFilepath;
        _logger = logger;
    }

    private void LoadSettings(object? state)
    {
        var settingsFilepath = _settingsFilepath;
        if (settingsFilepath is null)
        {
            _logger?.LogWarning(
                "No filepath to settings object has been provided. Assuming {DefaultFilepath}",
                _defaultSettingsFilepath
            );
            settingsFilepath = _defaultSettingsFilepath;
        }

        var settings = JsonLoader<MaskinportenSettings>.LoadFile(settingsFilepath);
        MaskinportenClient.Configure(settings);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting service {Service}", nameof(MaskinportenSettingsLoader));
        _timer = new Timer(LoadSettings, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Shutting down service {Service}", nameof(MaskinportenSettingsLoader));
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer is IAsyncDisposable timer)
        {
            await timer.DisposeAsync();
        }

        _timer = null;
    }
}
