using SampleWebApp.HttpClients.Models;

namespace SampleWebApp.HttpClients;

public class TypedClient2 : ITypedClient2
{
    public static IEnumerable<string> RequiredScopes => ["idporten:dcr.read"];
    private readonly HttpClient _client;

    public TypedClient2(HttpClient client)
    {
        _client = client;
        _client.BaseAddress = new Uri("https://api.test.samarbeid.digdir.no");
    }

    public async Task<MaskinportenQueryDTO?> GetApiData()
    {
        return await _client.GetFromJsonAsync<MaskinportenQueryDTO>("/clients/ds_altinn_maskinporten");
    }
}
