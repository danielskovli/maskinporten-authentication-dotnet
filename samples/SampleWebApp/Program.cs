using System.Net.Http.Headers;
using MaskinportenAuthentication;
using MaskinportenAuthentication.Extensions;
using MaskinportenAuthentication.Services;

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

// Maskinporten configuration -- Option 1:
//     Named httpclient
// Refer to appsettings.json for `MaskinportenSettingsLoader` filepath config
builder.Services.AddMaskinportenHttpClient(
    clientName: "maskinportenClient1",
    scopes: ["skatteetaten:testnorge/testdata.read"]
);
builder.Services.AddMaskinportenHttpClient(
    clientName: "maskinportenClient2",
    scopes: ["idporten:dcr.read"],
    client =>
    {
        client.BaseAddress = new Uri("https://api.test.samarbeid.digdir.no");
    }
);

// Maskinporten configuration -- Option 2:
//     Dependency injection
// Refer to appsettings.json for `MaskinportenSettingsLoader` filepath config
builder.Services.AddHostedService<MaskinportenSettingsLoader>();
builder.Services.AddTransient<IMaskinportenClient, MaskinportenClient>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// Usage example 1.1:
//     Named http client, as configured above
app.MapGet(
    "/named-clients",
    async (IHttpClientFactory clientFactory) =>
    {
        // Retrieve the already configured http client named "maskinportenClient1"
        // This client has already been configured with a set of scopes in the section above,
        // and has a DelegatingHandler attached which will handle Authorization headers for all get/post/put/etc calls
        using var client1 = clientFactory.CreateClient("maskinportenClient1");
        using var result1 = await client1.GetAsync(
            "https://testdata.api.skatteetaten.no/api/testnorge/v2/soek/freg?kql=tenorRelasjoner.brreg-er-fr%3A%7BdagligLeder%3A*%7D&antall=3"
        );
        result1.EnsureSuccessStatusCode();
        var content1 = await result1.Content.ReadAsStringAsync();

        // Same as above, but retrieve "maskinportenClient2". This is a client with different scope claims.
        using var client2 = clientFactory.CreateClient("maskinportenClient2");
        using var result2 = await client2.GetAsync("/clients/ds_altinn_maskinporten");
        result2.EnsureSuccessStatusCode();
        var content2 = await result1.Content.ReadAsStringAsync();

        // This is just to demonstrate that each client is uniquely configured
        string? content3;
        try
        {
            using var theWrongClient = clientFactory.CreateClient("maskinportenClient2");
            using var errorResult = await theWrongClient.GetAsync(
                "https://testdata.api.skatteetaten.no/api/testnorge/v2/soek/freg?kql=tenorRelasjoner.brreg-er-fr%3A%7BdagligLeder%3A*%7D&antall=3"
            );
            errorResult.EnsureSuccessStatusCode();
            content3 = await errorResult.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            content3 = e.ToString();
        }

        return new
        {
            Result1 = content1,
            Result2 = content2,
            Result3 = content3
        };
    }
);

// Usage example 2.1:
//     Dependency injection with a configuration delegate
app.MapGet(
    "/config-delegate",
    async (IMaskinportenClient client) =>
    {
        // Generate a pre-authorized request
        using var request = await client.AuthorizedRequestAsync(
            scopes: ["idporten:dcr.read"],
            request =>
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri("https://api.test.samarbeid.digdir.no/clients/ds_altinn_maskinporten");
            }
        );

        // Send request and retrieve response
        using var result = await client.HttpClient.SendAsync(request);
        result.EnsureSuccessStatusCode();
        var content = await result.Content.ReadAsStringAsync();

        return new { Result = content };
    }
);

// Usage example 2.2:
//     Dependency injection with a factory method
app.MapGet(
    "/factory-method",
    async (IMaskinportenClient client) =>
    {
        // Generate a pre-authorized request
        using var request = await client.AuthorizedRequestAsync(
            scopes: ["idporten:dcr.read"],
            HttpMethod.Get,
            "https://api.test.samarbeid.digdir.no/clients/ds_altinn_maskinporten"
        );

        // Not required, just an example of further configuration
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Send request and retrieve response
        using var result = await client.HttpClient.SendAsync(request);
        result.EnsureSuccessStatusCode();
        var content = await result.Content.ReadAsStringAsync();

        return new { Result = content };
    }
);

// Usage example 2.3:
//     Dependency injection with a completely manual http request assembly
app.MapGet(
    "/manual-auth",
    async (IMaskinportenClient client) =>
    {
        // Fetch an authorization token from Maskinporten
        var authTokenResponse = await client.Authorize(scopes: ["idporten:dcr.read"]);

        // Create a http request and configure the Authorization header
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.test.samarbeid.digdir.no/clients/ds_altinn_maskinporten"
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authTokenResponse.AccessToken);

        // Send request and retrieve response
        using var result = await client.HttpClient.SendAsync(request);
        result.EnsureSuccessStatusCode();
        var content = await result.Content.ReadAsStringAsync();

        return new { Result = content };
    }
);

app.Run();
