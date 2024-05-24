using SampleWebApp.HttpClients.Models;

namespace SampleWebApp.HttpClients;

public class TypedClient1 : ITypedClient1
{
    public static IEnumerable<string> RequiredScopes => ["skatteetaten:testnorge/testdata.read"];
    private readonly HttpClient _client;

    public TypedClient1(HttpClient client)
    {
        _client = client;
        _client.BaseAddress = new Uri("https://testdata.api.skatteetaten.no");
    }

    public async Task<SkattQueryDTO?> GetApiData()
    {
        return await _client.GetFromJsonAsync<SkattQueryDTO>(
            "/api/testnorge/v2/soek/freg?kql=tenorRelasjoner.brreg-er-fr%3A%7BdagligLeder%3A*%7D&antall=3"
        );
    }
}
