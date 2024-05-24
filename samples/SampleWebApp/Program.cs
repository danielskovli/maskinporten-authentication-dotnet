using System.Net.Http.Headers;
using MaskinportenAuthentication;
using MaskinportenAuthentication.Extensions;
using SampleWebApp.HttpClients;

/*
    Running the examples in this test requires a Maskinporten client with the following scopes:
        idporten:dcr.read
        skatteetaten:testnorge/testdata.read

    This in order to access the protected API endpoints at:
        https://api.test.samarbeid.digdir.no/clients
        https://testdata.api.skatteetaten.no/api/testnorge/v2/soek

    For your own testing and/or implementation, substitute values as required.

    NOTE: The key `MaskinportenSettingsFilepath` in appsettings.Development.json defines where the
          maskinporten-settings.json file should be loaded from.
*/


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Required: Add a Maskinporten client to the dependency injection service
builder.AddMaskinportenClient();

// Optional: Set up global http clients
builder.Services.AddHttpClient("namedClient1").UseMaskinportenAuthorization(["skatteetaten:testnorge/testdata.read"]);
builder.Services.AddHttpClient<ITypedClient1, TypedClient1>().UseMaskinportenAuthorization(TypedClient1.RequiredScopes);
builder.Services.AddHttpClient<ITypedClient2, TypedClient2>().UseMaskinportenAuthorization(TypedClient2.RequiredScopes);

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// Usage example 1:
//     Named http client, as configured above
app.MapGet(
    "/named-client",
    async (IHttpClientFactory clientFactory) =>
    {
        using var httpClient = clientFactory.CreateClient("namedClient1");
        using var result = await httpClient.GetAsync(
            "https://testdata.api.skatteetaten.no/api/testnorge/v2/soek/freg?kql=tenorRelasjoner.brreg-er-fr%3A%7BdagligLeder%3A*%7D&antall=3"
        );
        result.EnsureSuccessStatusCode();
        var content = await result.Content.ReadAsStringAsync();

        return new { Data = content };
    }
);

// Usage example 2:
//     Typed client, as configured above
app.MapGet(
    "/typed-client",
    async (ITypedClient1 typedClient1, ITypedClient2 typedClient2) =>
    {
        var content1 = await typedClient1.GetApiData();
        var content2 = await typedClient2.GetApiData();
        return new { Data1 = content1, Data2 = content2 };
    }
);

// Usage example 3:
//     Manual authorization
app.MapGet(
    "/manual-auth",
    async (IMaskinportenClient maskinportenClient, IHttpClientFactory httpClientFactory) =>
    {
        using var httpclient = httpClientFactory.CreateClient();
        var token = await maskinportenClient.GetAccessToken(["idporten:dcr.read"]);
        httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        using var result = await httpclient.GetAsync(
            "https://api.test.samarbeid.digdir.no/clients/ds_altinn_maskinporten"
        );
        result.EnsureSuccessStatusCode();
        var content = await result.Content.ReadAsStringAsync();

        return new { Data = content };
    }
);

app.Run();
