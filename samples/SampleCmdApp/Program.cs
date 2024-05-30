using MaskinportenAuthentication.Extensions;
using MaskinportenAuthentication.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleCmdApp;

/*
    Running the example in this test requires a Maskinporten client with the following scope:
        skatteetaten:testnorge/testdata.read

    This in order to access the protected API endpoint at:
        https://testdata.api.skatteetaten.no/api/testnorge/v2/soek

    For your own testing and/or implementation, substitute the values as required.

    NOTE: The maskinporten-settings.json file is optimized for a `WebApplication` consumer,
          which means the `MaskinportenSettings` data is wrapped under a JSON property called "MaskinportenSettings".
          For this reason the deserializing is slightly more tedious than normal.
*/

// Instantiate a host application to handle DI and other goodies
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Configre logging
builder
    .Logging.AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("System", LogLevel.Warning)
    .AddFilter("MaskinportenAuthentication", LogLevel.Debug)
    .AddFilter("SampleCmdApp", LogLevel.Debug)
    .AddConsole();

// Load Maskinporten settings and configure clients
var maskinportenSettings = SettingsLoader<MaskinportenSettings>.Load(
    Path.GetFullPath("../secrets/maskinporten-settings.json"),
    "MaskinportenSettings"
);
builder.Services.AddMaskinportenClient(config =>
{
    config.Key = maskinportenSettings.Key;
    config.Authority = maskinportenSettings.Authority;
    config.ClientId = maskinportenSettings.ClientId;
});
builder.Services.AddHttpClient<ExampleHttpClient>().UseMaskinportenAuthorization(ExampleHttpClient.RequiredScopes);
builder.Services.AddSingleton<ExampleRunner>();

// Build host and execute runner
using IHost host = builder.Build();
var runner = host.Services.GetRequiredService<ExampleRunner>();
await runner.Run();
