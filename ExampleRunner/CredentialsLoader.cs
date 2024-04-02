using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace ExampleRunner;

public static class CredentialsLoader
{
    public static async Task<Credentials> Load(string filepath)
    {
        var raw = await File.ReadAllTextAsync(filepath);
        return JsonSerializer.Deserialize<Credentials>(raw);
    }
}

public class Credentials
{
    [JsonPropertyName("appId")]
    public string AppId { get; set; }

    [JsonPropertyName("keys")]
    public List<JsonWebKey> Keys { get; set; }
}