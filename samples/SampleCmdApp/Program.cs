using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using MaskinportenAuthentication;
using MaskinportenAuthentication.Models;
using MaskinportenAuthentication.Utils;
using Microsoft.Extensions.Logging;

/*
    Running the examples in this test requires a Maskinporten client with the following scopes:
        idporten:dcr.read
        skatteetaten:testnorge/testdata.read
        
    This in order to access the protected API endpoints at:
        https://api.test.samarbeid.digdir.no/clients
        https://testdata.api.skatteetaten.no/api/testnorge/v2/soek
        
    For your own testing and/or implementation, substitute values as required.
*/


using var loggerFactory = LoggerFactory.Create(static builder =>
{
    builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddFilter("MaskinportenAuthentication", LogLevel.Debug)
        .AddConsole();
});
var logger = loggerFactory.CreateLogger<Program>();

// Maskinporten client configuration
var settings = await JsonLoader<MaskinportenSettings>.LoadFileAsync(
    Path.Combine(
        Environment.CurrentDirectory,
        "../../../../",
        "secrets",
        "maskinporten-settings.json"
    )
);
var client = new MaskinportenClient(
    settings: settings,
    logger: loggerFactory.CreateLogger<MaskinportenClient>()
);

// Usage example 1: configuration delegate
var request = await client.AuthorizedRequestAsync(
    scopes: ["idporten:dcr.read"],
    request =>
    {
        request.Method = HttpMethod.Get;
        request.RequestUri = new Uri(
            "https://api.test.samarbeid.digdir.no/clients/ds_altinn_maskinporten"
        );
    }
);
var result = await client.HttpClient.SendAsync(request);
result.EnsureSuccessStatusCode();
var content = await result.Content.ReadAsStringAsync();
logger.LogInformation("Configuration delegate result: {Result}", content);

// Usage example 2: factory method
request = await client.AuthorizedRequestAsync(
    scopes: ["skatteetaten:testnorge/testdata.read"],
    HttpMethod.Get,
    "https://testdata.api.skatteetaten.no/api/testnorge/v2/soek/freg?kql=tenorRelasjoner.brreg-er-fr%3A%7BdagligLeder%3A*%7D&antall=3"
);

result = await client.HttpClient.SendAsync(request);
result.EnsureSuccessStatusCode();
content = await result.Content.ReadAsStringAsync();
logger.LogInformation("Factory method result: {Result}", content);

// Usage example 3: manual authorization
var authTokenResponse = await client.Authorize(scopes: ["skatteetaten:testnorge/testdata.read"]);
request = new HttpRequestMessage(
    HttpMethod.Get,
    "https://testdata.api.skatteetaten.no/api/testnorge/v2/soek/freg?kql=tenorRelasjoner.brreg-er-fr%3A%7BdagligLeder%3A*%7D&antall=3"
);
request.Headers.Authorization = new AuthenticationHeaderValue(
    "Bearer",
    authTokenResponse.AccessToken
);

result = await client.HttpClient.SendAsync(request);
result.EnsureSuccessStatusCode();
content = await result.Content.ReadAsStringAsync();
logger.LogInformation("Manual auth result: {Result}", content);
