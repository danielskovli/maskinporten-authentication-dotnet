using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace MaskinportenAuthentication.Models;

public record MaskinportenSettings
{
    [JsonPropertyName("authority")]
    public required string Authority { get; set; }

    [JsonPropertyName("clientId")]
    public required string ClientId { get; set; }

    [JsonPropertyName("key")]
    public required JsonWebKey Key { get; set; }
}
