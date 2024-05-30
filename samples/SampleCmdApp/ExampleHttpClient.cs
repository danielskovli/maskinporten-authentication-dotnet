using Microsoft.Extensions.Logging;

namespace SampleCmdApp;

public class ExampleHttpClient
{
    public static IEnumerable<string> RequiredScopes => ["skatteetaten:testnorge/testdata.read"];
    private readonly HttpClient _client;
    private readonly ILogger<ExampleHttpClient> _logger;

    public ExampleHttpClient(HttpClient client, ILogger<ExampleHttpClient> logger)
    {
        _logger = logger;
        _client = client;
        _client.BaseAddress = new Uri("https://testdata.api.skatteetaten.no");
    }

    public async Task<string> GetApiData()
    {
        _logger.LogDebug("Executing API call via client {Instance}->{HttpClient}", this, _client);

        using var result = await _client.GetAsync(
            "/api/testnorge/v2/soek/freg?kql=tenorRelasjoner.brreg-er-fr%3A%7BdagligLeder%3A*%7D&antall=3"
        );
        result.EnsureSuccessStatusCode();

        _logger.LogDebug("Server responded with success");

        return await result.Content.ReadAsStringAsync();
    }
}
